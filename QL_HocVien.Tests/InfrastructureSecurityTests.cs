using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Infrastructure.DTOs;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;
using QL_HocVien.Services;
using QL_HocVien.ViewModels;
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

        #region 6. KIỂM THỬ BẢO MẬT HỆ THỐNG NÂNG CAO (DPAPI, OTP RATE LIMIT, BRUTE FORCE PROTECTION)
        [Fact]
        public void Test_Dpapi_Secret_Encryption_And_Decryption()
        {
            string secret = "SuperSecretAdminPassword@2026";
            string encrypted = AuthSecurityHelper.EncryptSecret(secret);

            // Xác minh đã được mã hóa không còn dạng plaintext
            Assert.StartsWith("enc:", encrypted);
            Assert.DoesNotContain(secret, encrypted);

            // Xác minh giải mã thành công về chuỗi ban đầu
            string decrypted = AuthSecurityHelper.DecryptSecret(encrypted);
            Assert.Equal(secret, decrypted);

            // Xác minh xử lý chuỗi rỗng và chuỗi không mã hóa
            Assert.Equal(string.Empty, AuthSecurityHelper.EncryptSecret(""));
            Assert.Equal("plain_text_without_prefix", AuthSecurityHelper.DecryptSecret("plain_text_without_prefix"));
        }

        [Fact]
        public void Test_AuthSecurityHelper_Hash_And_Verify_SecurityAnswer()
        {
            string answer = "Thủ Đô Hà Nội";
            string hash = AuthSecurityHelper.HashSecurityAnswer(answer);

            Assert.False(string.IsNullOrWhiteSpace(hash));
            // Không phân biệt HOA/thường và khoảng trắng thừa
            Assert.True(AuthSecurityHelper.VerifySecurityAnswer("thủ đô hà nội", hash));
            Assert.True(AuthSecurityHelper.VerifySecurityAnswer("  THỦ ĐÔ HÀ NỘI   ", hash));

            // Trả lời sai
            Assert.False(AuthSecurityHelper.VerifySecurityAnswer("TP Hồ Chí Minh", hash));
            Assert.False(AuthSecurityHelper.VerifySecurityAnswer("", hash));
            Assert.False(AuthSecurityHelper.VerifySecurityAnswer("thủ đô hà nội", null));
        }

        [Fact]
        public async Task Test_AccountRecovery_Info_Lookup_And_Hint()
        {
            string dbName = $"TestSecDb_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbName}.db")
                .Options;

            using var context = new AppDbContext(options);
            DbInitializer.Initialize(context);

            var userRepo = new UserRepository(context);
            var cadetRepo = new CadetRepository(context);
            var authService = new AuthService(userRepo, cadetRepo, context);

            try
            {
                // 1. Tra cứu theo Username ("admin")
                var adminLookup = await authService.GetAccountRecoveryInfoAsync("admin");
                Assert.True(adminLookup.Success);
                Assert.False(string.IsNullOrWhiteSpace(adminLookup.SecurityQuestion));
                Assert.Contains("bàn giao", adminLookup.PasswordHint!);

                // 2. Tra cứu theo Số điện thoại ("0988888888")
                var phoneLookup = await authService.GetAccountRecoveryInfoAsync("0988888888");
                Assert.True(phoneLookup.Success);
                Assert.Equal(adminLookup.SecurityQuestion, phoneLookup.SecurityQuestion);

                // 3. Tra cứu tài khoản không tồn tại
                var nonExist = await authService.GetAccountRecoveryInfoAsync("nguoidung_khong_co_that");
                Assert.False(nonExist.Success);
                Assert.Contains("Không tìm thấy", nonExist.Message);
            }
            finally
            {
                context.Database.EnsureDeleted();
            }
        }

        [Fact]
        public async Task Test_SecurityAnswer_Wrong_Answer_Fails()
        {
            string dbName = $"TestSecDb_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbName}.db")
                .Options;

            using var context = new AppDbContext(options);
            DbInitializer.Initialize(context);

            var userRepo = new UserRepository(context);
            var cadetRepo = new CadetRepository(context);
            var authService = new AuthService(userRepo, cadetRepo, context);

            try
            {
                // Thử đổi mật khẩu với câu trả lời sai
                var failRes = await authService.ResetPasswordWithSecurityAnswerAsync("admin", "cautraloisai", "NewPass@123");
                Assert.False(failRes.Success);
                Assert.Contains("không chính xác", failRes.Message, StringComparison.OrdinalIgnoreCase);

                // Mật khẩu cũ vẫn phải hoạt động bình thường
                var loginOld = await authService.LoginAsync("admin", "Admin@123");
                Assert.True(loginOld.Success);
            }
            finally
            {
                context.Database.EnsureDeleted();
            }
        }

        [Fact]
        public async Task Test_SecurityAnswer_ResetPassword_Success_And_Login()
        {
            string dbName = $"TestSecDb_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbName}.db")
                .Options;

            using var context = new AppDbContext(options);
            DbInitializer.Initialize(context);

            var userRepo = new UserRepository(context);
            var cadetRepo = new CadetRepository(context);
            var authService = new AuthService(userRepo, cadetRepo, context);

            try
            {
                // Đổi mật khẩu với câu trả lời bảo mật đúng của admin mặc định ("quanlyhocvien")
                var resetRes = await authService.ResetPasswordWithSecurityAnswerAsync("admin", "quanlyhocvien", "NewSecureAdminPass@123");
                Assert.True(resetRes.Success);

                // Xác minh đăng nhập thành công bằng mật khẩu mới
                var loginRes = await authService.LoginAsync("admin", "NewSecureAdminPass@123");
                Assert.True(loginRes.Success);
                Assert.NotNull(loginRes.User);

                // Mật khẩu cũ không còn hợp lệ
                var loginOld = await authService.LoginAsync("admin", "Admin@123");
                Assert.False(loginOld.Success);
            }
            finally
            {
                context.Database.EnsureDeleted();
            }
        }

        [Fact]
        public async Task Test_ForgotPasswordViewModel_Offline_Recovery_Flow()
        {
            string dbName = $"TestSecDb_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbName}.db")
                .Options;

            using var context = new AppDbContext(options);
            DbInitializer.Initialize(context);

            var userRepo = new UserRepository(context);
            var cadetRepo = new CadetRepository(context);
            var authService = new AuthService(userRepo, cadetRepo, context);

            try
            {
                var vm = new ForgotPasswordViewModel(authService);
                vm.Identifier = "admin";

                // Bước 1: Tra cứu tài khoản offline
                await vm.LookupAccountCommand.ExecuteAsync(null);
                Assert.True(vm.IsAccountFound);
                Assert.NotNull(vm.SecurityQuestion);
                Assert.NotNull(vm.PasswordHint);

                // Bước 2: Nhập câu trả lời bảo mật và mật khẩu mới
                vm.SecurityAnswer = "quanlyhocvien";
                vm.NewPassword = "VmResetPass@123";
                vm.ConfirmNewPassword = "VmResetPass@123";

                await vm.ResetPasswordCommand.ExecuteAsync(null);

                // Xác minh thành công không báo lỗi
                Assert.True(string.IsNullOrEmpty(vm.ErrorMessage));
                Assert.Contains("thành công", vm.InfoMessage, StringComparison.OrdinalIgnoreCase);

                // Xác minh đăng nhập thành công với mật khẩu vừa đặt lại
                var loginRes = await authService.LoginAsync("admin", "VmResetPass@123");
                Assert.True(loginRes.Success);
            }
            finally
            {
                context.Database.EnsureDeleted();
            }
        }
        #endregion
    }
}
