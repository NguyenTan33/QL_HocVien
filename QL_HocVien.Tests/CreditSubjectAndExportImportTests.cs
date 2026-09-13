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
    }
}
