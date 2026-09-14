using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Models.Entity;
using QL_HocVien.Services.Calculators;
using QL_HocVien.Services.Implementations;
using QL_HocVien.ViewModels;
using Xunit;

namespace QL_HocVien.Tests
{
    public class CreditSubjectAndExportImportTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly string _dbName;
        private readonly CreditSubjectService _creditService;
        private readonly CreditGradeCalculator _calculator;

        public CreditSubjectAndExportImportTests()
        {
            _dbName = $"TestDb_Credit_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbName}.db")
                .Options;

            _context = new AppDbContext(options);
            DbInitializer.Initialize(_context);

            _calculator = new CreditGradeCalculator();
            _creditService = new CreditSubjectService(_context, _calculator);
        }

        public void Dispose()
        {
            try
            {
                _context.Database.EnsureDeleted();
                _context.Dispose();
                if (File.Exists($"{_dbName}.db"))
                {
                    File.Delete($"{_dbName}.db");
                }
            }
            catch { }
        }


        [Fact]
        public void Test_IsAllSubjectsSelected_SelectsAllItems()
        {
            var vm = new CreditSubjectManagementViewModel(_creditService, null!, null!, null!, null!, null!, null, null);
            var s1 = new CreditSubject { SubjectCode = "S1", IsSelected = false };
            var s2 = new CreditSubject { SubjectCode = "S2", IsSelected = false };
            s1.PropertyChanged += (sender, e) => { };
            s2.PropertyChanged += (sender, e) => { };
            vm.Subjects.Add(s1);
            vm.Subjects.Add(s2);

            vm.IsAllSubjectsSelected = true;

            Assert.True(s1.IsSelected, "s1 should be selected");
            Assert.True(s2.IsSelected, "s2 should be selected");

            vm.ToggleSelectAllSubjects();
            Assert.False(s1.IsSelected, "s1 should be deselected");
            Assert.False(s2.IsSelected, "s2 should be deselected");
        }

        [Fact]
        public async Task Test_ConsolidateMajorSubjects_IsIdempotent_And_ScoresDoNotDecreaseOnMultipleRuns()
        {
            // 1. Tạo dữ liệu mẫu: 1 học viên và môn học có nhóm (CNTT gồm CNTT 1 và CNTT 2)
            var cadet = new Cadet
            {
                CadetCode = "HV-TEST-001",
                FullName = "Nguyễn Văn Test",
                Unit = "Đại đội 1",
                ClassName = "Lớp Test"
            };
            _context.Cadets.Add(cadet);

            var s1 = new CreditSubject
            {
                SubjectCode = "CNTT1",
                SubjectName = "CNTT 1",
                Credits = 1.5,
                SubjectGroup = "CNTT",
                IsComponent = false
            };
            var s2 = new CreditSubject
            {
                SubjectCode = "CNTT2",
                SubjectName = "CNTT 2",
                Credits = 1.5,
                SubjectGroup = "CNTT",
                IsComponent = false
            };
            _context.CreditSubjects.AddRange(s1, s2);
            await _context.SaveChangesAsync();

            // Nhập điểm cho 2 môn thành phần
            _context.CreditScoreRecords.Add(new CreditScoreRecord
            {
                CadetId = cadet.Id,
                CreditSubjectId = s1.Id,
                FinalScore = 8.0,
                RegularScore = 8.0,
                CreatedAt = DateTime.Now
            });
            _context.CreditScoreRecords.Add(new CreditScoreRecord
            {
                CadetId = cadet.Id,
                CreditSubjectId = s2.Id,
                FinalScore = 9.0,
                RegularScore = 9.0,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            // 2. Chạy lần đầu tiên để gom nhóm
            await _creditService.ConsolidateMajorSubjectsAsync();

            var summaries1 = await _creditService.GetCadetAcademicSummariesAsync();
            var cadetSummary1 = summaries1.First(c => c.CadetId == cadet.Id);
            double initialGpa = cadetSummary1.Gpa;
            int initialCompleted = cadetSummary1.TotalSubjectsCompleted;
            double initialCredits = cadetSummary1.TotalCreditsEarned;

            Assert.True(initialGpa > 0, "GPA ban đầu phải lớn hơn 0");
            Assert.True(initialCredits > 0, "Tổng tín chỉ đạt được phải lớn hơn 0");

            var primarySubject = await _context.CreditSubjects
                .Include(s => s.Components)
                .FirstAsync(s => s.SubjectGroup == "CNTT" && !s.IsComponent);

            double expectedCredits = 3.0; // 1.5 + 1.5
            Assert.Equal(expectedCredits, primarySubject.Credits);
            Assert.Equal(2, primarySubject.Components.Count);

            // 3. Giả lập người dùng click vào danh mục Môn Học 10 lần liên tục
            for (int i = 0; i < 10; i++)
            {
                await _creditService.ConsolidateMajorSubjectsAsync();
            }

            // 4. Kiểm tra lại: Tín chỉ môn chính KHÔNG ĐƯỢC tăng dồn (vẫn là 3.0)
            var refreshedSubject = await _context.CreditSubjects
                .Include(s => s.Components)
                .FirstAsync(s => s.SubjectGroup == "CNTT" && !s.IsComponent);

            Assert.Equal(expectedCredits, refreshedSubject.Credits);
            Assert.Equal(2, refreshedSubject.Components.Count);

            // 5. Kiểm tra lại: Điểm số, GPA và số môn hoàn thành KHÔNG ĐƯỢC giảm dần
            var summariesAfter = await _creditService.GetCadetAcademicSummariesAsync();
            var cadetSummaryAfter = summariesAfter.First(c => c.CadetId == cadet.Id);

            Assert.Equal(initialGpa, cadetSummaryAfter.Gpa);
            Assert.Equal(initialCompleted, cadetSummaryAfter.TotalSubjectsCompleted);
            Assert.Equal(initialCredits, cadetSummaryAfter.TotalCreditsEarned);

            // Điểm trong CSDL không bị xóa do CASCADE
            var scoreCount = await _context.CreditScoreRecords.CountAsync(s => s.CadetId == cadet.Id);
            Assert.Equal(2, scoreCount);
        }

        [Fact]
        public async Task Test_ExportAcademicReport_PlacesCreditAtEnd_And_ReimportDoesNotDuplicate()
        {
            // 1. Tạo môn học và học viên có điểm
            var cadet = new Cadet
            {
                CadetCode = "HV-EXPORT-01",
                FullName = "Trần Văn Xuất",
                Unit = "Đại đội 2",
                ClassName = "Lớp Xuất"
            };
            _context.Cadets.Add(cadet);

            var s1 = new CreditSubject
            {
                SubjectCode = "TOAN",
                SubjectName = "Toán Cao Cấp",
                Credits = 3.0,
                IsComponent = false
            };
            _context.CreditSubjects.Add(s1);
            await _context.SaveChangesAsync();

            var comp = new SubjectAssessmentComponent
            {
                CreditSubjectId = s1.Id,
                ComponentName = "Toán Cao Cấp",
                Credits = 3.0,
                OrderIndex = 1
            };
            _context.SubjectAssessmentComponents.Add(comp);
            await _context.SaveChangesAsync();

            _context.CreditScoreRecords.Add(new CreditScoreRecord
            {
                CadetId = cadet.Id,
                CreditSubjectId = s1.Id,
                ComponentId = comp.Id,
                FinalScore = 8.5,
                RegularScore = 8.5
            });
            await _context.SaveChangesAsync();

            // 2. Xuất báo cáo Excel
            string tempFile = Path.Combine(Path.GetTempPath(), $"AcademicReport_{Guid.NewGuid():N}.xlsx");
            try
            {
                var summaries = await _creditService.GetCadetAcademicSummariesAsync();
                var subjects = await _creditService.GetAllSubjectsAsync();
                var (success, msg) = await _creditService.ExportAcademicReportAsync(tempFile, summaries, subjects);
                Assert.True(success, msg);
                Assert.True(File.Exists(tempFile));

                // 3. Kiểm tra cấu trúc file Excel xuất ra
                using (var wb = new XLWorkbook(tempFile))
                {
                    var ws = wb.Worksheets.First();
                    Assert.NotNull(ws);

                    // Cột 2 là Mã học viên
                    Assert.Equal("Mã học viên", ws.Cell(4, 2).GetString());
                    Assert.Equal("HV-EXPORT-01", ws.Cell(6, 2).GetString());

                    // Cột tín chỉ môn học bắt đầu ở cột 7 (sau 6 cột thông tin học viên)
                    Assert.Equal("Toán Cao Cấp", ws.Cell(5, 7).GetString());

                    // Cột Tổng Tín Chỉ ở cuối bảng ngay trước TBM
                    Assert.Equal("Tổng Tín Chỉ", ws.Cell(5, 8).GetString());
                    Assert.Equal("TBM", ws.Cell(5, 9).GetString());

                    // Không có footer row ghi "TÍN CHỈ ĐỢT THI"
                    var cellA7 = ws.Cell(7, 1).GetString();
                    Assert.False(cellA7.Contains("TÍN CHỈ"), "Không được có footer row tín chỉ gây lặp khi import lại");
                }

                // 4. Nhập lại file vừa xuất và kiểm tra không tạo thêm môn học trùng lặp và bảo lưu mã học viên
                var (importSuccess, importMsg, importedCadets, importedScores) = await _creditService.ImportStandardTbmExcelAsync(tempFile);
                Assert.True(importSuccess, importMsg);

                var reimportedCadet = await _context.Cadets.FirstOrDefaultAsync(c => c.FullName == "Trần Văn Xuất");
                Assert.NotNull(reimportedCadet);
                Assert.Equal("HV-EXPORT-01", reimportedCadet.CadetCode);

                int subjectCountAfterFirstImport = await _context.CreditSubjects.CountAsync();
                Assert.True(subjectCountAfterFirstImport > 0);

                // Nhập lại lần 2 từ cùng một file: Số lượng môn học phải giữ nguyên tuyệt đối (không bị nhân đôi)
                var (reimportSuccess, _, _, _) = await _creditService.ImportStandardTbmExcelAsync(tempFile);
                Assert.True(reimportSuccess);

                int subjectCountAfterSecondImport = await _context.CreditSubjects.CountAsync();
                Assert.Equal(subjectCountAfterFirstImport, subjectCountAfterSecondImport);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }

        [Fact]
        public async Task Test_ImportScores_PreservesExistingCadetCodes_AndDoesNotOverwriteWithAutoIncrement()
        {
            // 1. Giả lập người dùng nhập danh sách học viên trước với mã sinh viên tùy chỉnh (không phải mặc định)
            var c1 = new Cadet
            {
                CadetCode = "K26-CNTT-007",
                FullName = "Lê Văn Tám",
                Unit = "Đại đội 1"
            };
            var c2 = new Cadet
            {
                CadetCode = "2024-HVQS-999",
                FullName = "Phạm Thị Mai",
                Unit = "Đại đội 2"
            };
            _context.Cadets.AddRange(c1, c2);
            await _context.SaveChangesAsync();

            // 2. Tạo file Excel điểm có cột Mã học viên (như khi xuất từ hệ thống ra)
            string tempFileWithCode = Path.Combine(Path.GetTempPath(), $"ScoreWithCode_{Guid.NewGuid():N}.xlsx");
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Diem");
                    // Row 1: Credits
                    ws.Cell(1, 7).Value = 2.0;

                    // Row 4 & 5: Headers
                    ws.Cell(4, 1).Value = "TT";
                    ws.Cell(4, 2).Value = "Mã học viên";
                    ws.Cell(4, 3).Value = "Đơn vị";
                    ws.Cell(4, 4).Value = "Họ và tên đệm";
                    ws.Cell(4, 5).Value = "Tên";
                    ws.Cell(4, 6).Value = "Họ và tên ghép";
                    ws.Cell(5, 7).Value = "Triết học";

                    // Row 6: c1
                    ws.Cell(6, 1).Value = 1;
                    ws.Cell(6, 2).Value = "K26-CNTT-007";
                    ws.Cell(6, 3).Value = "Đại đội 1";
                    ws.Cell(6, 4).Value = "Lê Văn";
                    ws.Cell(6, 5).Value = "Tám";
                    ws.Cell(6, 6).Value = "Lê Văn Tám";
                    ws.Cell(6, 7).Value = 8.5;

                    // Row 7: c2
                    ws.Cell(7, 1).Value = 2;
                    ws.Cell(7, 2).Value = "2024-HVQS-999";
                    ws.Cell(7, 3).Value = "Đại đội 2";
                    ws.Cell(7, 4).Value = "Phạm Thị";
                    ws.Cell(7, 5).Value = "Mai";
                    ws.Cell(7, 6).Value = "Phạm Thị Mai";
                    ws.Cell(7, 7).Value = 9.0;

                    wb.SaveAs(tempFileWithCode);
                }

                // 3. Nhập điểm từ file Excel này
                var (success1, msg1, newCadets1, scores1) = await _creditService.ImportStandardTbmExcelAsync(tempFileWithCode);
                Assert.True(success1, msg1);
                Assert.Equal(0, newCadets1); // Không tạo học viên mới vì đã có sẵn trong DB
                Assert.Equal(2, scores1);

                // 4. Kiểm tra CSDL: Mã học viên PHẢI GIỮ NGUYÊN TUYỆT ĐỐI, không bị đổi thành HV26001, HV26002
                var dbC1 = await _context.Cadets.FindAsync(c1.Id);
                var dbC2 = await _context.Cadets.FindAsync(c2.Id);

                Assert.NotNull(dbC1);
                Assert.NotNull(dbC2);
                Assert.Equal("K26-CNTT-007", dbC1.CadetCode);
                Assert.Equal("2024-HVQS-999", dbC2.CadetCode);

                // Điểm số được gắn chuẩn xác vào CadetId
                var score1 = await _context.CreditScoreRecords.FirstOrDefaultAsync(s => s.CadetId == c1.Id);
                var score2 = await _context.CreditScoreRecords.FirstOrDefaultAsync(s => s.CadetId == c2.Id);
                Assert.NotNull(score1);
                Assert.Equal(8.5, score1.FinalScore);
                Assert.NotNull(score2);
                Assert.Equal(9.0, score2.FinalScore);
            }
            finally
            {
                if (File.Exists(tempFileWithCode))
                {
                    try { File.Delete(tempFileWithCode); } catch { }
                }
            }

            // 5. Kiểm tra trường hợp file Excel cũ KHÔNG CÓ cột Mã học viên (chỉ có Tên + Đơn vị)
            string tempFileLegacy = Path.Combine(Path.GetTempPath(), $"ScoreLegacy_{Guid.NewGuid():N}.xlsx");
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("DiemLegacy");
                    // Row 1: Credits
                    ws.Cell(1, 6).Value = 3.0;

                    // Row 4 & 5: Headers (không có cột mã học viên, Col 2 là Đơn vị)
                    ws.Cell(4, 1).Value = "TT";
                    ws.Cell(4, 2).Value = "Đơn vị";
                    ws.Cell(4, 3).Value = "Họ đệm";
                    ws.Cell(4, 4).Value = "Tên";
                    ws.Cell(4, 5).Value = "Họ và tên";
                    ws.Cell(5, 6).Value = "Chính trị";

                    // Row 6: c1
                    ws.Cell(6, 1).Value = 1;
                    ws.Cell(6, 2).Value = "Đại đội 1";
                    ws.Cell(6, 3).Value = "Lê Văn";
                    ws.Cell(6, 4).Value = "Tám";
                    ws.Cell(6, 5).Value = "Lê Văn Tám";
                    ws.Cell(6, 6).Value = 7.5;

                    // Row 7: c2
                    ws.Cell(7, 1).Value = 2;
                    ws.Cell(7, 2).Value = "Đại đội 2";
                    ws.Cell(7, 3).Value = "Phạm Thị";
                    ws.Cell(7, 4).Value = "Mai";
                    ws.Cell(7, 5).Value = "Phạm Thị Mai";
                    ws.Cell(7, 6).Value = 8.0;

                    wb.SaveAs(tempFileLegacy);
                }

                // 6. Nhập điểm từ file Legacy
                var (success2, msg2, newCadets2, scores2) = await _creditService.ImportStandardTbmExcelAsync(tempFileLegacy);
                Assert.True(success2, msg2);
                Assert.Equal(0, newCadets2); // Vẫn khớp đúng c1, c2 qua Tên + Đơn vị

                // 7. Mã học viên VẪN PHẢI ĐƯỢC BẢO LƯU NGUYÊN VẸN!
                var dbC1Legacy = await _context.Cadets.FindAsync(c1.Id);
                var dbC2Legacy = await _context.Cadets.FindAsync(c2.Id);

                Assert.NotNull(dbC1Legacy);
                Assert.NotNull(dbC2Legacy);
                Assert.Equal("K26-CNTT-007", dbC1Legacy.CadetCode);
                Assert.Equal("2024-HVQS-999", dbC2Legacy.CadetCode);
            }
            finally
            {
                if (File.Exists(tempFileLegacy))
                {
                    try { File.Delete(tempFileLegacy); } catch { }
                }
            }
        }

        [Fact]
        public async Task Test_ImportRealStandardTbmExcel_ExactGrouping_And_TbmMatching()
        {
            var realFilePath = @"C:\Users\minht\Downloads\Điểm TBM chuẩn .xlsx";
            if (!File.Exists(realFilePath))
            {
                // Bỏ qua nếu môi trường test không có file trên máy người dùng
                return;
            }

            // 1. Thực hiện import từ file Excel chuẩn của người dùng
            var (success, message, newCadets, importedScores) = await _creditService.ImportStandardTbmExcelAsync(realFilePath);
            Assert.True(success, message);
            Assert.Equal(65, newCadets); // File Excel có 65 học viên (STT 33 bị khuyết trong file gốc, từ HD123 đến HD187)
            Assert.True(importedScores > 0);

            // 2. Kiểm tra việc gom nhóm môn học
            // Có đúng 28 môn lớn (IsComponent = false)
            var majorSubjects = await _context.CreditSubjects
                .Where(s => !s.IsComponent)
                .Include(s => s.Components)
                .ToListAsync();
            Assert.Equal(28, majorSubjects.Count);

            // Có đúng 57 bài kiểm tra / môn thành phần (SubjectAssessmentComponent)
            var components = await _context.SubjectAssessmentComponents.ToListAsync();
            Assert.Equal(57, components.Count);

            // Tổng số tín chỉ của các môn/thành phần = 62.90
            var totalCredits = Math.Round(components.Sum(c => c.Credits), 2);
            Assert.Equal(62.90, totalCredits);

            // 3. Kiểm tra chi tiết một số môn lớn quan trọng
            var toan = majorSubjects.FirstOrDefault(m => m.SubjectName == "Toán");
            Assert.NotNull(toan);
            Assert.Equal(1.0, toan.Credits);

            var banSung = majorSubjects.FirstOrDefault(m => m.SubjectName == "Bắn súng");
            Assert.NotNull(banSung);
            Assert.Equal(6, banSung.Components.Count);
            Assert.Equal(5.2, Math.Round(banSung.Credits, 2));

            var cntt = majorSubjects.FirstOrDefault(m => m.SubjectName == "CNTT");
            Assert.NotNull(cntt);
            Assert.Equal(4, cntt.Components.Count);
            Assert.Equal(4.1, Math.Round(cntt.Credits, 2));

            var theLuc = majorSubjects.FirstOrDefault(m => m.SubjectName == "Thể lực");
            Assert.NotNull(theLuc);
            Assert.Equal(4, theLuc.Components.Count);
            Assert.Equal(3.2, Math.Round(theLuc.Credits, 2));

            // 4. Kiểm tra điểm TBM và MSSV của các học viên mẫu khớp chuẩn 100% với Excel
            var summaries = await _creditService.GetCadetAcademicSummariesAsync();
            var hdSummaries = summaries.Where(c => c.CadetCode.StartsWith("HD")).ToList();
            Assert.Equal(65, hdSummaries.Count);

            // HD123 - Đặng Thắng An -> 7.74
            var s123 = summaries.FirstOrDefault(c => c.CadetCode == "HD123");
            Assert.NotNull(s123);
            Assert.Equal("Đặng Thắng An", s123.FullName);
            Assert.Equal("b2", s123.Unit);
            Assert.Equal(7.74, s123.Gpa);

            // HD124 - Nguyễn Lê Quốc An -> 8.01
            var s124 = summaries.FirstOrDefault(c => c.CadetCode == "HD124");
            Assert.NotNull(s124);
            Assert.Equal(8.01, s124.Gpa);

            // HD187 - Huỳnh Chí Vỹ -> 7.61
            var s187 = summaries.FirstOrDefault(c => c.CadetCode == "HD187");
            Assert.NotNull(s187);
            Assert.Equal(7.61, s187.Gpa);

            // 5. Kiểm tra toàn bộ 66 học viên so với cột TBM (Cột 64) trong file Excel gốc
            using var testStream = new FileStream(realFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(testStream);
            var ws = workbook.Worksheets.First();
            for (int r = 5; r <= 69; r++)
            {
                var code = ws.Cell(r, 2).GetString().Trim();
                var expectedTbm = Math.Round(ws.Cell(r, 64).GetDouble(), 2);

                var s = summaries.FirstOrDefault(c => c.CadetCode == code);
                Assert.NotNull(s);
                Assert.True(Math.Abs(expectedTbm - s.Gpa) < 0.001,
                    $"Học viên {code} ({s.FullName}) tính ra {s.Gpa} nhưng Excel là {expectedTbm}");
            }
        }

        [Fact]
        public async Task Test_ImportStandardTbmExcel_With_GroupCodeRow_Merges_Components_Correctly()
        {
            string tempExcel = Path.Combine(Path.GetTempPath(), $"Test_GroupCode_{Guid.NewGuid():N}.xlsx");
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Sheet1");
                    // Row 1: Tín chỉ
                    ws.Cell(1, 7).Value = 0.8;
                    ws.Cell(1, 8).Value = 0.2;
                    ws.Cell(1, 9).Value = 1.5;

                    // Row 3: Mã Môn (Mã gộp)
                    ws.Cell(3, 6).Value = "Mã Môn";
                    ws.Cell(3, 7).Value = "ad.vc.1";
                    ws.Cell(3, 8).Value = "ad.vc.1";
                    ws.Cell(3, 9).Value = "ntrk.1";

                    // Row 4: Header tên môn
                    ws.Cell(4, 1).Value = "STT";
                    ws.Cell(4, 2).Value = "Mã HV";
                    ws.Cell(4, 3).Value = "Đơn vị";
                    ws.Cell(4, 6).Value = "Họ và tên ghép";
                    ws.Cell(4, 7).Value = "2 AK";
                    ws.Cell(4, 8).Value = "AK 1";
                    ws.Cell(4, 9).Value = "THI TH M-L";

                    // Row 5: Học viên 1
                    ws.Cell(5, 1).Value = 1;
                    ws.Cell(5, 2).Value = "HV-TEST-999";
                    ws.Cell(5, 3).Value = "b1";
                    ws.Cell(5, 6).Value = "Trần Văn TestGroupCode";
                    ws.Cell(5, 7).Value = 8.0;
                    ws.Cell(5, 8).Value = 9.0;
                    ws.Cell(5, 9).Value = 7.0;

                    wb.SaveAs(tempExcel);
                }

                // Thực hiện Import
                var (success, msg, cadetsCount, scoresCount) = await _creditService.ImportStandardTbmExcelAsync(tempExcel);
                Assert.True(success, msg);
                Assert.Equal(1, cadetsCount);
                Assert.Equal(3, scoresCount);

                // Kiểm tra Môn lớn có mã "ad.vc.1" được gom từ cả 2 đợt "2 AK" và "AK 1"
                var mergedSubj = await _context.CreditSubjects
                    .Include(s => s.Components)
                    .FirstOrDefaultAsync(s => s.SubjectCode == "ad.vc.1");

                Assert.NotNull(mergedSubj);
                Assert.Equal(1.0, mergedSubj.Credits); // 0.8 + 0.2 = 1.0
                Assert.Equal(2, mergedSubj.Components.Count);
                Assert.Contains(mergedSubj.Components, c => c.ComponentName == "2 AK" && Math.Abs(c.Credits - 0.8) < 0.001);
                Assert.Contains(mergedSubj.Components, c => c.ComponentName == "AK 1" && Math.Abs(c.Credits - 0.2) < 0.001);

                // Kiểm tra môn độc lập "ntrk.1"
                var indepSubj = await _context.CreditSubjects
                    .Include(s => s.Components)
                    .FirstOrDefaultAsync(s => s.SubjectCode == "ntrk.1");

                Assert.NotNull(indepSubj);
                Assert.Equal(1.5, indepSubj.Credits);
                Assert.Single(indepSubj.Components);
                Assert.Equal("THI TH M-L", indepSubj.Components.First().ComponentName);

                // Kiểm tra tính điểm
                var summaries = await _creditService.GetCadetAcademicSummariesAsync();
                var cadetSummary = summaries.FirstOrDefault(c => c.FullName == "Trần Văn TestGroupCode");
                Assert.NotNull(cadetSummary);

                // Điểm môn gộp ad.vc.1: (8.0 * 0.8 + 9.0 * 0.2) / 1.0 = 8.2
                Assert.True(cadetSummary.SubjectScores.ContainsKey(mergedSubj.Id));
                Assert.Equal(8.2, cadetSummary.SubjectScores[mergedSubj.Id]);

                // Export ra file Excel và kiểm tra lại dòng mã môn
                string exportExcel = Path.Combine(Path.GetTempPath(), $"Test_Export_{Guid.NewGuid():N}.xlsx");
                var allSubjects = await _context.CreditSubjects.ToListAsync();
                var (expSuccess, expMsg) = await _creditService.ExportAcademicReportAsync(exportExcel, summaries, allSubjects);
                Assert.True(expSuccess, expMsg);

                using (var expWb = new XLWorkbook(exportExcel))
                {
                    var expWs = expWb.Worksheets.First();
                    // Kiểm tra dòng 4 cột 6 ghi "Mã Môn"
                    Assert.Equal("Mã Môn", expWs.Cell(4, 6).GetString().Trim());
                    // Cột 7 và 8 của đợt thi mang mã môn "ad.vc.1"
                    Assert.Equal("ad.vc.1", expWs.Cell(4, 7).GetString().Trim());
                    Assert.Equal("ad.vc.1", expWs.Cell(4, 8).GetString().Trim());
                }

                if (File.Exists(exportExcel)) File.Delete(exportExcel);
            }
            finally
            {
                if (File.Exists(tempExcel)) File.Delete(tempExcel);
            }
        }

        [Fact]
        public async Task Test_SchoolYear_And_Cohort_Filtering_And_Scoping()
        {
            // 1. Tạo 2 học viên thuộc 2 khóa khác nhau
            var hv1 = new Cadet
            {
                CadetCode = "HV-K29-01",
                FullName = "Nguyễn Văn K29",
                Cohort = "K29",
                EnrollmentYear = 2023,
                AcademicYear = "2023 - 2027",
                Unit = "Đại đội 1",
                ClassName = "Lớp K29"
            };

            var hv2 = new Cadet
            {
                CadetCode = "HV-K30-01",
                FullName = "Trần Văn K30",
                Cohort = "K30",
                EnrollmentYear = 2024,
                AcademicYear = "2024 - 2028",
                Unit = "Đại đội 2",
                ClassName = "Lớp K30"
            };

            _context.Cadets.AddRange(hv1, hv2);

            // 2. Tạo môn học
            var subj = new CreditSubject
            {
                SubjectCode = "TOANCC",
                SubjectName = "Toán Cao Cấp",
                Credits = 2.0,
                IsComponent = false
            };
            _context.CreditSubjects.Add(subj);
            await _context.SaveChangesAsync();

            var comp = new SubjectAssessmentComponent
            {
                CreditSubjectId = subj.Id,
                ComponentName = "Thi Kết Thúc",
                Credits = 2.0,
                OrderIndex = 1
            };
            _context.SubjectAssessmentComponents.Add(comp);
            await _context.SaveChangesAsync();

            // 3. HV1 có điểm năm 2023 - 2024 (8.0) và năm 2024 - 2025 (9.0)
            _context.CreditScoreRecords.Add(new CreditScoreRecord
            {
                CadetId = hv1.Id,
                ComponentId = comp.Id,
                CreditSubjectId = subj.Id,
                FinalScore = 8.0,
                SchoolYear = "2023 - 2024",
                CreatedAt = DateTime.UtcNow
            });
            _context.CreditScoreRecords.Add(new CreditScoreRecord
            {
                CadetId = hv1.Id,
                ComponentId = comp.Id,
                CreditSubjectId = subj.Id,
                FinalScore = 9.0,
                SchoolYear = "2024 - 2025",
                CreatedAt = DateTime.UtcNow
            });

            // HV2 có điểm năm 2024 - 2025 (7.0)
            _context.CreditScoreRecords.Add(new CreditScoreRecord
            {
                CadetId = hv2.Id,
                ComponentId = comp.Id,
                CreditSubjectId = subj.Id,
                FinalScore = 7.0,
                SchoolYear = "2024 - 2025",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // 4. Test GetDistinctSchoolYearsAsync
            var schoolYears = await _creditService.GetDistinctSchoolYearsAsync();
            Assert.Contains("2023 - 2024", schoolYears);
            Assert.Contains("2024 - 2025", schoolYears);

            // 5. Test lọc theo SchoolYear "2023 - 2024"
            var summaries2023 = await _creditService.GetCadetAcademicSummariesAsync(schoolYear: "2023 - 2024");
            var s1 = summaries2023.FirstOrDefault(c => c.CadetId == hv1.Id);
            var s2 = summaries2023.FirstOrDefault(c => c.CadetId == hv2.Id);
            Assert.NotNull(s1);
            Assert.Equal(8.0, s1.SubjectScores[subj.Id]);
            Assert.NotNull(s2);
            Assert.Null(s2.SubjectScores[subj.Id]); // HV2 chưa có điểm năm 2023 - 2024

            // 6. Test lọc theo Cohort "K29"
            var summariesK29 = await _creditService.GetCadetAcademicSummariesAsync(cohort: "K29");
            Assert.All(summariesK29, c => Assert.Equal("K29", c.Cohort));
            Assert.Contains(summariesK29, c => c.CadetId == hv1.Id);
            Assert.DoesNotContain(summariesK29, c => c.CadetId == hv2.Id);

            // 7. Test lọc theo Cohort "K30"
            var summariesK30 = await _creditService.GetCadetAcademicSummariesAsync(cohort: "K30");
            Assert.All(summariesK30, c => Assert.Equal("K30", c.Cohort));
            Assert.Contains(summariesK30, c => c.CadetId == hv2.Id);
            Assert.DoesNotContain(summariesK30, c => c.CadetId == hv1.Id);
        }

        [Fact]
        public async Task Test_ImportStandardTbmExcel_Detects_Cohort_And_SchoolYear_Automatically()
        {
            string tempExcel = Path.Combine(Path.GetTempPath(), $"Test_DetectCohortSchoolYear_{Guid.NewGuid():N}.xlsx");
            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("DiemTB");

                    // Header có chứa Khóa K29 và Năm học 2023-2024
                    ws.Cell(1, 1).Value = "HỌC VIỆN HẢI QUÂN";
                    ws.Cell(2, 1).Value = "BẢNG TỔNG HỢP ĐIỂM HỌC VIÊN KHÓA K29 - NĂM HỌC 2023 - 2024";

                    // Row 3: Tên đợt
                    ws.Cell(3, 7).Value = "Thi Cuối Kỳ";
                    // Row 4: Tín chỉ
                    ws.Cell(4, 1).Value = "STT";
                    ws.Cell(4, 2).Value = "Mã học viên";
                    ws.Cell(4, 6).Value = "Môn Tin";
                    ws.Cell(4, 7).Value = 2.0;

                    // Row 5: Học viên
                    ws.Cell(5, 1).Value = 1;
                    ws.Cell(5, 2).Value = "HV-DETECT-01";
                    ws.Cell(5, 6).Value = "Lê Văn Tự Động";
                    ws.Cell(5, 7).Value = 8.5;

                    wb.SaveAs(tempExcel);
                }

                // Thực hiện import không truyền schoolYear/cohort -> Service sẽ tự động nhận diện từ header
                var (success, msg, cadetsCount, scoresCount) = await _creditService.ImportStandardTbmExcelAsync(tempExcel);
                Assert.True(success, msg);
                Assert.Equal(1, cadetsCount);
                Assert.Equal(1, scoresCount);

                // Kiểm tra học viên được gán Cohort = K29, EnrollmentYear = 2023
                var cadet = await _context.Cadets.FirstOrDefaultAsync(c => c.CadetCode == "HV-DETECT-01");
                Assert.NotNull(cadet);
                Assert.Equal("K29", cadet.Cohort);
                Assert.Equal(2023, cadet.EnrollmentYear);

                // Kiểm tra điểm được gán SchoolYear = "2023 - 2024"
                var score = await _context.CreditScoreRecords.FirstOrDefaultAsync(s => s.CadetId == cadet.Id);
                Assert.NotNull(score);
                Assert.Equal("2023 - 2024", score.SchoolYear);
                Assert.Equal(8.5, score.FinalScore);
            }
            finally
            {
                if (File.Exists(tempExcel)) File.Delete(tempExcel);
            }
        }

        [Fact]
        public async Task Test_UnitHierarchy_ExcelImport_AndSegmentFiltering()
        {
            // 1. Tạo Khóa 75
            var cohort75 = new AcademicCohort
            {
                CohortCode = "K75",
                CohortName = "Khóa 75",
                CohortNumber = 75,
                AcademicYear = "2020 - 2024"
            };
            _context.AcademicCohorts.Add(cohort75);
            await _context.SaveChangesAsync();

            // 2. Tạo học viên với mã và đơn vị phân cấp
            var c1 = new Cadet { CadetCode = "ĐH.075.001", FullName = "Nguyễn Văn A", Unit = "dBB1/cBB1/bBB1", Cohort = "K75", CohortId = cohort75.Id };
            var c2 = new Cadet { CadetCode = "ĐH.075.002", FullName = "Trần Văn B", Unit = "dBB1/cBB1/bBB2", Cohort = "K75", CohortId = cohort75.Id };
            var c3 = new Cadet { CadetCode = "ĐH.075.003", FullName = "Lê Văn C", Unit = "dBB2/cBB1/bBB1", Cohort = "K75", CohortId = cohort75.Id };
            _context.Cadets.AddRange(c1, c2, c3);
            await _context.SaveChangesAsync();

            // 3. Kiểm tra lọc cấp Tiểu đoàn (mã & tên)
            var d1 = await _creditService.GetCadetAcademicSummariesAsync(unit: "dBB1", cohort: "K75");
            Assert.Equal(2, d1.Count);

            var d1Name = await _creditService.GetCadetAcademicSummariesAsync(unit: "Tiểu đoàn 1", cohort: "K75");
            Assert.Equal(2, d1Name.Count);

            var d2 = await _creditService.GetCadetAcademicSummariesAsync(unit: "dBB2", cohort: "K75");
            Assert.Equal(1, d2.Count);

            // 4. Kiểm tra lọc cấp Đại đội (mã & tên)
            var c1UnderD1 = await _creditService.GetCadetAcademicSummariesAsync(unit: "dBB1/cBB1", cohort: "K75");
            Assert.Equal(2, c1UnderD1.Count);

            var c1Name = await _creditService.GetCadetAcademicSummariesAsync(unit: "Đại đội 1", cohort: "K75");
            Assert.Equal(3, c1Name.Count); // cBB1 thuộc cả dBB1 và dBB2

            // 5. Kiểm tra lọc cấp Tiểu đội
            var b1 = await _creditService.GetCadetAcademicSummariesAsync(unit: "dBB1/cBB1/bBB1", cohort: "K75");
            Assert.Equal(1, b1.Count);
            Assert.Equal("ĐH.075.001", b1[0].CadetCode);

            var b2 = await _creditService.GetCadetAcademicSummariesAsync(unit: "dBB1/cBB1/bBB2", cohort: "K75");
            Assert.Equal(1, b2.Count);
            Assert.Equal("ĐH.075.002", b2[0].CadetCode);

            // 6. Kiểm tra lọc kết hợp với từ khóa tìm kiếm
            var search = await _creditService.GetCadetAcademicSummariesAsync(unit: "dBB1", keyword: "Nguyễn", cohort: "K75");
            Assert.Equal(1, search.Count);
            Assert.Equal("Nguyễn Văn A", search[0].FullName);
        }

        [Fact]
        public async Task Test_AutoHealUnitHierarchyAndCohorts()
        {
            // 1. Tạo Khóa 75
            var cohort = new AcademicCohort { CohortCode = "K75", CohortName = "Khóa 75", CohortNumber = 75 };
            _context.AcademicCohorts.Add(cohort);

            // Đơn vị cha
            var uD = new MilitaryUnit { UnitCode = "dBB1", UnitName = "Tiểu đoàn 1", ParentUnit = "K75" };
            _context.MilitaryUnits.Add(uD);
            await _context.SaveChangesAsync();

            var uC = new MilitaryUnit { UnitCode = "cBB1", UnitName = "Đại đội 1", ParentUnit = "Tiểu đoàn 1", ParentUnitId = uD.Id };
            _context.MilitaryUnits.Add(uC);
            await _context.SaveChangesAsync();

            // Đơn vị con bị thiếu ParentUnitId và có ParentUnit dạng "dBB1/cBB1"
            var uB = new MilitaryUnit { UnitCode = "bBB1", UnitName = "Tiểu đội 1", ParentUnit = "dBB1/cBB1", ParentUnitId = null };
            _context.MilitaryUnits.Add(uB);

            // Học viên có Cohort = "K75" nhưng CohortId = null
            var cadet = new Cadet { CadetCode = "HV-HEAL-01", FullName = "Test Heal", Cohort = "K75", CohortId = null, Unit = "dBB1/cBB1/bBB1" };
            _context.Cadets.Add(cadet);
            await _context.SaveChangesAsync();

            // Chạy auto-heal
            DbInitializer.AutoHealUnitHierarchyAndCohorts(_context);

            // Kiểm tra kết quả
            var healedUnit = await _context.MilitaryUnits.FirstOrDefaultAsync(u => u.UnitCode == "bBB1");
            Assert.NotNull(healedUnit);
            Assert.Equal(uC.Id, healedUnit.ParentUnitId);

            var healedCadet = await _context.Cadets.FirstOrDefaultAsync(c => c.CadetCode == "HV-HEAL-01");
            Assert.NotNull(healedCadet);
            Assert.Equal(cohort.Id, healedCadet.CohortId);
        }

        [Fact]
        public async Task Test_GetSubjectBreakdownForCadetAsync_ReturnsAllComponents_WithCorrectWeightsAndFinalScores()
        {
            // 1. Tạo học viên
            var cadet = new Cadet
            {
                CadetCode = "HV-BD-01",
                FullName = "Nguyễn Văn Phân Rã",
                Unit = "dBB1/cBB1/bBB1",
                Cohort = "K75"
            };
            _context.Cadets.Add(cadet);
            await _context.SaveChangesAsync();

            // 2. Tạo môn học lớn có 2 thành phần (VKHD: Thi VKHDL 0.5 TC, VKHD 4.8 TC, Tổng 5.3 TC)
            var subjVkhd = new CreditSubject
            {
                SubjectCode = "TEST.VKHD",
                SubjectName = "VKHD",
                Credits = 5.3,
                IsComponent = false
            };
            _context.CreditSubjects.Add(subjVkhd);
            await _context.SaveChangesAsync();

            var comp1 = new SubjectAssessmentComponent
            {
                CreditSubjectId = subjVkhd.Id,
                ComponentName = "Thi VKHDL",
                Credits = 0.5,
                OrderIndex = 1
            };
            var comp2 = new SubjectAssessmentComponent
            {
                CreditSubjectId = subjVkhd.Id,
                ComponentName = "VKHD",
                Credits = 4.8,
                OrderIndex = 2
            };
            _context.SubjectAssessmentComponents.AddRange(comp1, comp2);

            // 3. Tạo môn độc lập không có thành phần con (Hóa: 0.15 TC)
            var subjHoa = new CreditSubject
            {
                SubjectCode = "TEST.HOA",
                SubjectName = "Hóa",
                Credits = 0.15,
                IsComponent = false
            };
            _context.CreditSubjects.Add(subjHoa);
            await _context.SaveChangesAsync();

            // 4. Nhập điểm
            var s1 = new CreditScoreRecord
            {
                CadetId = cadet.Id,
                CreditSubjectId = subjVkhd.Id,
                ComponentId = comp1.Id,
                FinalScore = 8.0,
                ExamDate = DateTime.Today
            };
            var s2 = new CreditScoreRecord
            {
                CadetId = cadet.Id,
                CreditSubjectId = subjVkhd.Id,
                ComponentId = comp2.Id,
                FinalScore = 7.1,
                ExamDate = DateTime.Today
            };
            var s3 = new CreditScoreRecord
            {
                CadetId = cadet.Id,
                CreditSubjectId = subjHoa.Id,
                FinalScore = 7.5,
                ExamDate = DateTime.Today
            };
            _context.CreditScoreRecords.AddRange(s1, s2, s3);
            await _context.SaveChangesAsync();

            // 5. Gọi GetSubjectBreakdownForCadetAsync
            var breakdowns = await _creditService.GetSubjectBreakdownForCadetAsync(cadet.Id);
            Assert.NotNull(breakdowns);

            // 6. Kiểm tra môn VKHD
            var vkhdDto = breakdowns.FirstOrDefault(b => b.MajorSubjectName == "VKHD");
            Assert.NotNull(vkhdDto);
            Assert.Equal(5.3, vkhdDto.TotalCredits);
            Assert.Equal(2, vkhdDto.Components.Count);
            Assert.True(vkhdDto.IsComplete);
            Assert.Equal(7.18, vkhdDto.FinalScore); // (8.0 * 0.5 + 7.1 * 4.8) / 5.3 = 7.1849 -> 7.18

            var c1 = vkhdDto.Components.FirstOrDefault(c => c.ComponentName == "Thi VKHDL");
            Assert.NotNull(c1);
            Assert.Equal(0.5, c1.Credits);
            Assert.Equal(8.0, c1.RecordedScore);
            Assert.Equal(0.75, c1.ContributionScore); // 8.0 * 0.5 / 5.3 = 0.7547 -> 0.75

            var c2 = vkhdDto.Components.FirstOrDefault(c => c.ComponentName == "VKHD");
            Assert.NotNull(c2);
            Assert.Equal(4.8, c2.Credits);
            Assert.Equal(7.1, c2.RecordedScore);
            Assert.Equal(6.43, c2.ContributionScore); // 7.1 * 4.8 / 5.3 = 6.4301 -> 6.43

            // 7. Kiểm tra môn Hóa (môn độc lập)
            var hoaDto = breakdowns.FirstOrDefault(b => b.MajorSubjectName == "Hóa");
            Assert.NotNull(hoaDto);
            Assert.Equal(0.15, hoaDto.TotalCredits);
            Assert.Single(hoaDto.Components);
            Assert.True(hoaDto.IsComplete);
            Assert.Equal(7.5, hoaDto.FinalScore);
        }

        [Fact]
        public void Test_Cadet_DynamicAgeCalculation()
        {
            var today = DateTime.Today;
            // Trường hợp 1: Đã qua sinh nhật trong năm
            var pastBirthdayDob = new DateTime(today.Year - 20, 1, 1);
            int expectedAge1 = 20;
            Assert.Equal(expectedAge1, Cadet.CalculateAge(pastBirthdayDob));

            // Trường hợp 2: Chưa tới sinh nhật trong năm (ví dụ tháng 12)
            var futureBirthdayDob = new DateTime(today.Year - 20, 12, 31);
            int expectedAge2 = (today.Month == 12 && today.Day == 31) ? 20 : 19;
            Assert.Equal(expectedAge2, Cadet.CalculateAge(futureBirthdayDob));

            // Kiểm tra thuộc tính dynamic Age trên Cadet entity
            var cadet = new Cadet
            {
                CadetCode = "HV-AGE-01",
                FullName = "Nguyễn Văn Tuổi Động",
                DateOfBirth = pastBirthdayDob
            };

            Assert.Equal(expectedAge1, cadet.Age);

            // Cập nhật ngày sinh khác -> Tuổi tự động tính lại
            cadet.DateOfBirth = new DateTime(today.Year - 25, 1, 1);
            Assert.Equal(25, cadet.Age);
        }

        [Theory]
        [InlineData("Công nghệ thông tin (Kiểm tra lần 1)", "Công nghệ thông tin")]
        [InlineData("Công nghệ thông tin (Thi)", "Công nghệ thông tin")]
        [InlineData("Điều lệnh quản lí bộ đội (Kiểm tra)", "Điều lệnh quản lí bộ đội")]
        [InlineData("Điều lệnh quản lí bộ đội (Thi)", "Điều lệnh quản lí bộ đội")]
        [InlineData("BĐT bài 1 AK", "BĐT bài 1 AK")]
        [InlineData("BĐT bài 2 AK", "BĐT bài 2 AK")]
        [InlineData("Bơi ếch", "Bơi ếch")]
        [InlineData("Co tay xà đơn", "Co tay xà đơn")]
        public void Test_CleanSubjectBaseName(string input, string expected)
        {
            var result = CreditSubjectService.CleanSubjectBaseName(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Test_Import_A1_And_T1_Excel_ExactCodeGrouping_NoOverwriting()
        {
            string a1Path = @"C:\Users\minht\Downloads\A1.xlsx";
            string t1Path = @"C:\Users\minht\Downloads\T1.xlsx";

            if (File.Exists(a1Path))
            {
                var (success, message, newCadets, importedScores) = await _creditService.ImportStandardTbmExcelAsync(a1Path);
                Assert.True(success, message);

                var majorSubjects = await _context.CreditSubjects
                    .Where(s => !s.IsComponent)
                    .Include(s => s.Components)
                    .ToListAsync();
                Assert.Equal(39, majorSubjects.Count);

                var components = await _context.SubjectAssessmentComponents.ToListAsync();
                Assert.Equal(57, components.Count);

                var totalCredits = Math.Round(components.Sum(c => c.Credits), 2);
                Assert.Equal(62.90, totalCredits);

                // Đảm bảo các môn có mã khác nhau nhưng cùng nhóm từ khóa không bị ghi đè hay gộp nhầm
                var ktA1 = majorSubjects.FirstOrDefault(s => s.SubjectCode == "KT.A1");
                var ktA2 = majorSubjects.FirstOrDefault(s => s.SubjectCode == "KT.A2");
                var ktR2 = majorSubjects.FirstOrDefault(s => s.SubjectCode == "KT.R2");
                var ttBe = majorSubjects.FirstOrDefault(s => s.SubjectCode == "TT.BE");
                var ttXd = majorSubjects.FirstOrDefault(s => s.SubjectCode == "TT.XD");

                Assert.NotNull(ktA1);
                Assert.NotNull(ktA2);
                Assert.NotNull(ktR2);
                Assert.NotNull(ttBe);
                Assert.NotNull(ttXd);

                Assert.NotEqual(ktA1.Id, ktA2.Id);
                Assert.NotEqual(ktA1.Id, ktR2.Id);
                Assert.NotEqual(ttBe.Id, ttXd.Id);
            }

            if (File.Exists(t1Path))
            {
                var (success, message, newCadets, importedScores) = await _creditService.ImportStandardTbmExcelAsync(t1Path);
                Assert.True(success, message);

                var majorSubjects = await _context.CreditSubjects
                    .Where(s => !s.IsComponent)
                    .Include(s => s.Components)
                    .ToListAsync();
                Assert.Equal(28, majorSubjects.Count);

                var components = await _context.SubjectAssessmentComponents.ToListAsync();
                Assert.Equal(57, components.Count);

                var totalCredits = Math.Round(components.Sum(c => c.Credits), 2);
                Assert.Equal(62.90, totalCredits);
            }
        }
    }
}
