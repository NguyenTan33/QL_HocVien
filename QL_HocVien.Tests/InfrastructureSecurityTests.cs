using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.Infrastructure.DTOs;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;
using Xunit;

namespace QL_HocVien.Tests
{
    public class InfrastructureSecurityTests
    {
        private readonly ISecuritySanitizer _sanitizer;
        private readonly IExcelSecurityValidator _excelValidator;

        public InfrastructureSecurityTests()
        {
            _sanitizer = new SecuritySanitizer();
            _excelValidator = new ExcelSecurityValidator();
        }

        #region 1. KIỂM THỬ PHÒNG CHỐNG SQL INJECTION
        [Theory]
        [InlineData("admin' OR '1'='1")]
        [InlineData("1; DROP TABLE Cadets--")]
        [InlineData("' UNION SELECT null, null, username, password FROM Users--")]
        [InlineData("test'; EXEC xp_cmdshell('dir')--")]
        [InlineData("'; TRUNCATE TABLE Officers;--")]
        public void Test_SqlInjection_Detection_Blocks_Attacks(string maliciousInput)
        {
            // Xác minh nhận diện SQLi
            bool detected = _sanitizer.ContainsSqlInjection(maliciousInput);
            Assert.True(detected, $"Failed to detect SQLi in: {maliciousInput}");

            // Xác minh ném SecurityThreatException
            Assert.Throws<SecurityThreatException>(() =>
            {
                _sanitizer.EnsureSafeInput(maliciousInput, "TestField");
            });
        }

        [Theory]
        [InlineData("Nguyễn Văn Chiến Thắng")]
        [InlineData("Đại đội 1 - Tiểu đoàn 2")]
        [InlineData("Lớp Chỉ huy Tham mưu Khóa 42")]
        [InlineData("Sĩ quan cấp Úy (Thượng úy)")]
        [InlineData("0988888888")]
        [InlineData("hocvien.quandoi@academy.mil.vn")]
        public void Test_SqlInjection_Allows_Valid_Vietnamese_Strings(string safeInput)
        {
            bool detected = _sanitizer.ContainsSqlInjection(safeInput);
            Assert.False(detected, $"False positive SQLi on safe text: {safeInput}");

            // Không ném Exception
            _sanitizer.EnsureSafeInput(safeInput, "SafeField");
        }
        #endregion

        #region 2. KIỂM THỬ PHÒNG CHỐNG SCRIPT / XSS & COMMAND INJECTION
        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("<script src='http://evil.com/hack.js'></script>")]
        [InlineData("javascript:alert(document.cookie)")]
        [InlineData("<img src='x' onerror='alert(1)'>")]
        [InlineData("<iframe src='http://malicious.org'></iframe>")]
        public void Test_ScriptInjection_Detection_Blocks_Attacks(string xssInput)
        {
            bool detected = _sanitizer.ContainsScriptInjection(xssInput);
            Assert.True(detected, $"Failed to detect Script/XSS in: {xssInput}");

            Assert.Throws<SecurityThreatException>(() =>
            {
                _sanitizer.EnsureSafeInput(xssInput, "XssField");
            });
        }

        [Theory]
        [InlineData("| powershell.exe -Command calc.exe")]
        [InlineData("& cmd.exe /c whoami")]
        [InlineData("file.txt; rm -rf /")]
        [InlineData("../../etc/shadow")]
        [InlineData(@"..\..\Windows\System32\cmd.exe")]
        public void Test_CommandInjection_Detection_Blocks_Attacks(string cmdInput)
        {
            bool detected = _sanitizer.ContainsCommandInjection(cmdInput);
            Assert.True(detected, $"Failed to detect Command Injection in: {cmdInput}");

            Assert.Throws<SecurityThreatException>(() =>
            {
                _sanitizer.EnsureSafeInput(cmdInput, "CmdField");
            });
        }
        #endregion

        #region 3. KIỂM THỬ EXCEL FORMULA INJECTION (DDE)
        [Theory]
        [InlineData("=cmd|'/C calc'!A0")]
        [InlineData("@SUM(1+1)*cmd|' /C calc'!A0")]
        [InlineData("+cmd|'/C calc'!A0")]
        [InlineData("-2+3+cmd|'/C calc'!A0")]
        public void Test_FormulaInjection_Detection(string formulaInput)
        {
            bool detected = _sanitizer.ContainsFormulaInjection(formulaInput);
            Assert.True(detected, $"Failed to detect Formula Injection in: {formulaInput}");

            Assert.Throws<SecurityThreatException>(() =>
            {
                _sanitizer.EnsureSafeInput(formulaInput, "FormulaField");
            });
        }

        [Fact]
        public void Test_FormulaInjection_Sanitization()
        {
            var dangerous = "=cmd|'/c calc'!A0";
            var sanitized = _sanitizer.SanitizeInput(dangerous);
            Assert.StartsWith("'", sanitized);
        }
        #endregion

        #region 4. KIỂM THỬ BẢO MẬT TẬP TIN EXCEL TẢI LÊN
        [Theory]
        [InlineData("virus.exe")]
        [InlineData("payload.bat")]
        [InlineData("script.ps1")]
        [InlineData("malware.vbs")]
        [InlineData("installer.msi")]
        [InlineData("screen.scr")]
        [InlineData("exploit.jar")]
        [InlineData("macro.xlsm")]
        public async Task Test_ExcelFile_Rejection_Of_Executable_And_Macro_Extensions(string fileName)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), fileName);
            try
            {
                await File.WriteAllTextAsync(tempFile, "fake content");
                var result = await _excelValidator.ValidateExcelFileAsync(tempFile);

                Assert.False(result.IsValid, $"Should reject executable extension: {fileName}");
                Assert.True(result.Message.Contains("bị cấm", StringComparison.OrdinalIgnoreCase) || 
                            result.Message.Contains("Macro", StringComparison.OrdinalIgnoreCase) || 
                            result.Message.Contains("không hợp lệ", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task Test_ExcelFile_Rejection_Of_Double_Extension()
        {
            var doubleExtFile = Path.Combine(Path.GetTempPath(), $"danh_sach.xlsx.exe");
            try
            {
                await File.WriteAllTextAsync(doubleExtFile, "fake payload");
                var result = await _excelValidator.ValidateExcelFileAsync(doubleExtFile);

                Assert.False(result.IsValid);
                Assert.Contains("bị cấm", result.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(doubleExtFile)) File.Delete(doubleExtFile);
            }
        }

        [Fact]
        public async Task Test_ExcelFile_Rejection_Of_Disguised_Exe_File_Via_MagicBytes()
        {
            // Giả lập kẻ tấn công đổi tên tệp malware.exe thành danh_sach.xlsx
            // File .exe bắt đầu bằng 2 byte Magic 'M' 'Z' (0x4D, 0x5A)
            var disguisedFile = Path.Combine(Path.GetTempPath(), $"malware_disguised_{Guid.NewGuid():N}.xlsx");
            try
            {
                byte[] peBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
                await File.WriteAllBytesAsync(disguisedFile, peBytes);

                var result = await _excelValidator.ValidateExcelFileAsync(disguisedFile);

                Assert.False(result.IsValid, "Phải phát hiện file PE .exe giả mạo đuôi .xlsx!");
                Assert.Contains("MZ", result.Message);
            }
            finally
            {
                if (File.Exists(disguisedFile)) File.Delete(disguisedFile);
            }
        }

        [Fact]
        public async Task Test_ExcelFile_Rejection_Of_Embedded_VbaProject_Macro()
        {
            // Giả lập tệp .xlsx chứa mã kịch bản Macro VBA nhúng bên trong
            var macroZipFile = Path.Combine(Path.GetTempPath(), $"macro_test_{Guid.NewGuid():N}.xlsx");
            try
            {
                using (var zipStream = new FileStream(macroZipFile, FileMode.Create))
                {
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        var entry = archive.CreateEntry("xl/vbaProject.bin");
                        using var writer = new StreamWriter(entry.Open());
                        writer.Write("VBA malicious code payload");
                    }
                }

                var result = await _excelValidator.ValidateExcelFileAsync(macroZipFile, disallowMacros: true);

                Assert.False(result.IsValid, "Phải từ chối file Excel chứa vbaProject.bin!");
                Assert.Contains("vbaProject.bin", result.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(macroZipFile)) File.Delete(macroZipFile);
            }
        }

        [Fact]
        public async Task Test_ExcelFile_Acceptance_Of_Real_Valid_Excel()
        {
            // Tạo 1 file Excel .xlsx chuẩn bằng ClosedXML
            var validFile = Path.Combine(Path.GetTempPath(), $"valid_excel_{Guid.NewGuid():N}.xlsx");
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Sheet1");
                    ws.Cell("A1").Value = "Mã học viên";
                    ws.Cell("B1").Value = "Họ và tên";
                    ws.Cell("A2").Value = "HV-001";
                    ws.Cell("B2").Value = "Nguyễn Văn A";
                    wb.SaveAs(validFile);
                }

                var result = await _excelValidator.ValidateExcelFileAsync(validFile);

                Assert.True(result.IsValid, $"Tập tin Excel hợp lệ bị từ chối: {result.Message}");
                Assert.Empty(result.SecurityWarnings);
            }
            finally
            {
                if (File.Exists(validFile)) File.Delete(validFile);
            }
        }
        #endregion

        #region 5. KIỂM THỬ VALIDATION FACTORY & RULES PATTERN (OOP SOLID)
        [Fact]
        public async Task Test_ValidationFactory_Execution_With_Rules()
        {
            // Thiết lập DI và tự động đăng ký quy tắc thông qua AddAppInfrastructureValidation
            var services = new ServiceCollection();
            services.AddAppInfrastructureValidation();
            var serviceProvider = services.BuildServiceProvider();

            var factory = serviceProvider.GetRequiredService<IValidationFactory>();
            Assert.NotNull(factory);

            // 1. Kiểm tra Login hợp lệ
            var validLogin = new LoginValidationRequest("admin", "Admin@123");
            await factory.ValidateAsync(validLogin); // Phải chạy mượt không ném exception

            // 2. Kiểm tra Login chứa SQL Injection -> Ném SecurityThreatException
            var maliciousLogin = new LoginValidationRequest("admin' OR '1'='1", "Password123");
            await Assert.ThrowsAsync<SecurityThreatException>(async () =>
            {
                await factory.ValidateAsync(maliciousLogin);
            });

            // 3. Kiểm tra Cadet chứa XSS Script Injection -> Ném SecurityThreatException
            var maliciousCadet = new Cadet
            {
                CadetCode = "HV-999",
                FullName = "<script>alert('hacked')</script>",
                Rank = "Binh nhì",
                Unit = "Đại đội 1",
                ClassName = "Lớp 1"
            };
            await Assert.ThrowsAsync<SecurityThreatException>(async () =>
            {
                await factory.ValidateAsync(maliciousCadet);
            });

            // 4. Kiểm tra ExcelFileValidationRequest chứa file .exe độc hại
            var tempExe = Path.Combine(Path.GetTempPath(), $"trojan_{Guid.NewGuid():N}.exe");
            try
            {
                await File.WriteAllTextAsync(tempExe, "MZ executable payload");
                var fileReq = new ExcelFileValidationRequest(tempExe);

                await Assert.ThrowsAsync<SecurityThreatException>(async () =>
                {
                    await factory.ValidateAsync(fileReq);
                });
            }
            finally
            {
                if (File.Exists(tempExe)) File.Delete(tempExe);
            }
        }
        #endregion
    }
}
