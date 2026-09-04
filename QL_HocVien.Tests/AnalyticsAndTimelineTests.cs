using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Services;
using Xunit;

namespace QL_HocVien.Tests
{
    public class AnalyticsAndTimelineTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ITrainingEventRepository _eventRepository;
        private readonly ITrainingEventService _eventService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IExcelService _excelService;
        private readonly string _dbName;

        public AnalyticsAndTimelineTests()
        {
            _dbName = $"TestDb_Analytics_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbName}.db")
                .Options;

            _context = new AppDbContext(options);
            DbInitializer.Initialize(_context);

            _eventRepository = new TrainingEventRepository(_context);
            _eventService = new TrainingEventService(_eventRepository);
            _analyticsService = new AnalyticsService(_context);

            var cadetRepo = new CadetRepository(_context);
            var classRepo = new ClassRepository(_context);
            var subjectRepo = new SubjectRepository(_context);
            var examRepo = new PhysicalExamRepository(_context);
            var evalService = new EvaluationService();
            var offRepo = new OfficerRepository(_context);
            var rankRepo = new RankRepository(_context);
            var posRepo = new PositionRepository(_context);
            var unitRepo = new UnitRepository(_context);
            var majorRepo = new MajorRepository(_context);
            var secValidator = new QL_HocVien.Infrastructure.Security.ExcelSecurityValidator();

            _excelService = new ExcelService(
                _context,
                cadetRepo,
                classRepo,
                subjectRepo,
                examRepo,
                evalService,
                offRepo,
                rankRepo,
                posRepo,
                unitRepo,
                majorRepo,
                secValidator);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            try
            {
                if (File.Exists($"{_dbName}.db"))
                {
                    File.Delete($"{_dbName}.db");
                }
            }
            catch
            {
                // Bỏ qua nếu đang bị giữ khóa
            }
        }

        #region 1. KIỂM THỬ THUẬT TOÁN ĐÁNH GIÁ BIẾN ĐỘNG (GROWTH, UNCHANGED, REGRESSION)
        [Fact]
        public async Task Test_CompareSessions_HigherBetterSubject_Growth()
        {
            // Môn Xà đơn (XD - IsHigherBetter = true): Từ 24 lần lên 26 lần
            var sessions = await _analyticsService.GetAvailableSessionsAsync();
            Assert.Contains("Kiểm tra Quý 3/2026", sessions);
            Assert.Contains("Kiểm tra Quý 4/2026", sessions);

            var result = await _analyticsService.CompareSessionsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026");
            Assert.NotNull(result);

            // HV 1 (Nguyễn Văn An): Cả 3 môn đều tiến bộ
            var hv1 = result.CadetTrends.FirstOrDefault(c => c.CadetCode == "HV-2026-001");
            Assert.NotNull(hv1);
            Assert.Equal(TrendDirection.Growth, hv1.OverallTrend);
            Assert.Equal("▲", hv1.OverallTrendSymbol);
            Assert.Equal("Tăng trưởng", hv1.OverallTrendText);

            var xdTrend = hv1.SubjectTrends.FirstOrDefault(s => s.SubjectCode == "XD");
            Assert.NotNull(xdTrend);
            Assert.True(xdTrend.ScoreDelta > 0);
            Assert.Equal(TrendDirection.Growth, xdTrend.Trend);
        }

        [Fact]
        public async Task Test_CompareSessions_LowerBetterSubject_Growth()
        {
            // Môn Chạy 100m (C100 - IsHigherBetter = false): Thời gian giảm từ 13.1s xuống 12.8s
            var result = await _analyticsService.CompareSessionsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026");
            var hv1 = result.CadetTrends.FirstOrDefault(c => c.CadetCode == "HV-2026-001");
            Assert.NotNull(hv1);

            var c100Trend = hv1.SubjectTrends.FirstOrDefault(s => s.SubjectCode == "C100");
            Assert.NotNull(c100Trend);
            Assert.False(c100Trend.IsHigherBetter);
            Assert.True(c100Trend.ScoreDelta < 0, "Thời gian chạy giảm phải có delta âm");
            Assert.Equal(TrendDirection.Growth, c100Trend.Trend);
        }

        [Fact]
        public async Task Test_CompareSessions_Regression_Detection()
        {
            // HV 4 (Trần Minh Quang): Môn XD giảm từ 18 xuống 14 lần, chạy 100m tăng thời gian từ 13.8 lên 14.5s -> Thụt lùi
            var result = await _analyticsService.CompareSessionsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026");
            var hv4 = result.CadetTrends.FirstOrDefault(c => c.CadetCode == "HV-2026-004");
            Assert.NotNull(hv4);

            Assert.Equal(TrendDirection.Regression, hv4.OverallTrend);
            Assert.Equal("▼", hv4.OverallTrendSymbol);
            Assert.Equal("Thụt lùi", hv4.OverallTrendText);

            var xdTrend = hv4.SubjectTrends.FirstOrDefault(s => s.SubjectCode == "XD");
            Assert.NotNull(xdTrend);
            Assert.Equal(TrendDirection.Regression, xdTrend.Trend);
        }

        [Fact]
        public async Task Test_CompareSessions_Unchanged_Detection()
        {
            // HV 3 (Phạm Hoàng Dũng): Thành tích 3 môn đều giữ nguyên
            var result = await _analyticsService.CompareSessionsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026");
            var hv3 = result.CadetTrends.FirstOrDefault(c => c.CadetCode == "HV-2026-003");
            Assert.NotNull(hv3);

            Assert.Equal(TrendDirection.Unchanged, hv3.OverallTrend);
            Assert.Equal("—", hv3.OverallTrendSymbol);
            Assert.Equal("Giữ nguyên", hv3.OverallTrendText);
        }
        #endregion

        #region 2. KIỂM THỬ PHÂN CẤP ĐẠI ĐỘI & LỚP / TIỂU ĐỘI
        [Fact]
        public async Task Test_UnitComparison_HierarchicalStats()
        {
            var unitComparisons = await _analyticsService.CompareUnitsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026");
            Assert.NotNull(unitComparisons);
            Assert.True(unitComparisons.Count >= 2);

            // Đại đội 1 có 2 học viên tăng trưởng 100%
            var c1Unit = unitComparisons.FirstOrDefault(u => u.UnitName == "Đại đội 1");
            Assert.NotNull(c1Unit);
            Assert.Equal(2, c1Unit.TotalCadets);
            Assert.Equal(2, c1Unit.GrowthCadetsCount);
            Assert.Equal(0, c1Unit.RegressionCadetsCount);

            // Đại đội 2 có 1 giữ nguyên và 1 thụt lùi
            var c2Unit = unitComparisons.FirstOrDefault(u => u.UnitName == "Đại đội 2");
            Assert.NotNull(c2Unit);
            Assert.Equal(2, c2Unit.TotalCadets);
            Assert.Equal(1, c2Unit.UnchangedCadetsCount);
            Assert.Equal(1, c2Unit.RegressionCadetsCount);
        }

        [Fact]
        public async Task Test_ClassComparison_RankingInUnit()
        {
            var classComparisons = await _analyticsService.CompareClassesAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026");
            Assert.NotNull(classComparisons);
            Assert.True(classComparisons.Count >= 2);

            // Xác minh xếp hạng trong đơn vị
            foreach (var group in classComparisons.GroupBy(c => c.Unit))
            {
                var ranked = group.OrderBy(c => c.RankInUnit).ToList();
                Assert.Equal(1, ranked[0].RankInUnit);
            }
        }

        [Fact]
        public async Task Test_CadetFilter_ByKeyword_And_Unit()
        {
            // Lọc theo tên "Văn An"
            var filteredByKw = await _analyticsService.CompareCadetsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026", keyword: "Văn An");
            Assert.Single(filteredByKw);
            Assert.Equal("HV-2026-001", filteredByKw[0].CadetCode);

            // Lọc theo mã "HV-2026-003"
            var filteredByCode = await _analyticsService.CompareCadetsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026", keyword: "HV-2026-003");
            Assert.Single(filteredByCode);
            Assert.Equal("Phạm Hoàng Dũng", filteredByCode[0].FullName);

            // Lọc theo Đơn vị "Đại đội 1"
            var filteredByUnit = await _analyticsService.CompareCadetsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026", unit: "Đại đội 1");
            Assert.Equal(2, filteredByUnit.Count);
            Assert.All(filteredByUnit, c => Assert.Equal("Đại đội 1", c.Unit));
        }
        #endregion

        #region 3. KIỂM THỬ QUẢN LÝ LỊCH SỰ KIỆN TIMELINE (TRAININGEVENT)
        [Fact]
        public async Task Test_TrainingEvent_CRUD_And_Validation()
        {
            // 1. Thêm mới sự kiện hợp lệ
            var newEvt = new TrainingEvent
            {
                Title = "Kiểm tra bắn súng K54 bài 1",
                Category = "Thi cử quân sự",
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(6),
                TargetUnit = "Đại đội 1",
                Location = "Trường bắn TB2",
                Priority = "Cao",
                Status = "Đang chuẩn bị",
                Description = "Bắn bia số 4 ngực cự ly 25m"
            };

            var (success, msg, created) = await _eventService.CreateEventAsync(newEvt);
            Assert.True(success);
            Assert.NotNull(created);
            Assert.True(created.Id > 0);

            // 2. Kiểm tra validation: Ngày kết thúc < Ngày bắt đầu
            var invalidEvt = new TrainingEvent
            {
                Title = "Sự kiện lỗi ngày",
                Category = "Kiểm tra thể lực",
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(5) // Lỗi
            };
            var (invSuccess, invMsg, _) = await _eventService.CreateEventAsync(invalidEvt);
            Assert.False(invSuccess);
            Assert.Contains("Ngày kết thúc", invMsg);

            // 3. Đánh dấu hoàn thành (Toggle Complete)
            var toggleRes = await _eventService.ToggleCompleteAsync(created.Id);
            Assert.True(toggleRes.Success);
            var updated = await _eventService.GetByIdAsync(created.Id);
            Assert.NotNull(updated);
            Assert.Equal("Đã hoàn thành", updated.Status);

            // 4. Xóa sự kiện
            var deleteRes = await _eventService.DeleteEventAsync(created.Id);
            Assert.True(deleteRes.Success);
            var deleted = await _eventService.GetByIdAsync(created.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Test_TrainingEvent_Filter_ByCategoryAndStatus()
        {
            var events = await _eventService.GetFilteredEventsAsync("Kiểm tra thể lực", null, null, null);
            Assert.NotEmpty(events);
            Assert.All(events, e => Assert.Equal("Kiểm tra thể lực", e.Category));
        }
        #endregion

        #region 4. KIỂM THỬ XUẤT BÁO CÁO SO SÁNH EXCEL
        [Fact]
        public async Task Test_ExcelExport_ComparisonReport()
        {
            var result = await _analyticsService.CompareSessionsAsync("Kiểm tra Quý 3/2026", "Kiểm tra Quý 4/2026");
            Assert.NotNull(result);

            var tempFile = Path.Combine(Path.GetTempPath(), $"TestComparisonReport_{Guid.NewGuid():N}.xlsx");
            try
            {
                var (success, msg) = await _excelService.ExportComparisonToExcelAsync(result, tempFile);
                Assert.True(success, $"Export failed: {msg}");
                Assert.True(File.Exists(tempFile));

                var fileInfo = new FileInfo(tempFile);
                Assert.True(fileInfo.Length > 0, "File Excel xuất ra phải có nội dung");
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
        #endregion
    }
}
