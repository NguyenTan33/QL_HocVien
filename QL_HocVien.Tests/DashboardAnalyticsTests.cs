using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Models.Filters;
using QL_HocVien.Services;
using Xunit;

namespace QL_HocVien.Tests
{
    public class DashboardAnalyticsTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IDashboardAnalyticsService _dashboardService;
        private readonly ITrainingRecommendationService _recommendationService;
        private readonly IExcelService _excelService;
        private readonly ICadetService _cadetService;
        private readonly string _dbName;

        public DashboardAnalyticsTests()
        {
            _dbName = $"TestDb_Dashboard_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbName}.db")
                .Options;

            _context = new AppDbContext(options);
            DbInitializer.Initialize(_context);

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

            _cadetService = new CadetService(cadetRepo);
            var examService = new PhysicalExamService(examRepo, subjectRepo, evalService);
            var subjectService = new SubjectService(subjectRepo);
            var classService = new ClassService(classRepo);
            var catalogService = new CatalogService(rankRepo, posRepo, unitRepo, majorRepo);

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

            _dashboardService = new DashboardAnalyticsService(
                _cadetService,
                examService,
                subjectService,
                classService,
                catalogService);

            _recommendationService = new TrainingRecommendationService();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            if (File.Exists($"{_dbName}.db"))
            {
                try { File.Delete($"{_dbName}.db"); } catch { }
            }
        }

        [Fact]
        public async Task Test_DashboardAnalytics_SummaryAndCalculations()
        {
            var criteria = new DashboardFilterCriteria();
            var summary = await _dashboardService.GetSummaryAsync(criteria);

            Assert.NotNull(summary);
            Assert.True(summary.TotalCadets > 0);
            Assert.True(summary.TotalExamRecords > 0);
            Assert.True(summary.OverallPassRate >= 0 && summary.OverallPassRate <= 100);
            Assert.True(summary.EliteRate >= 0 && summary.EliteRate <= 100);
            Assert.False(string.IsNullOrWhiteSpace(summary.OverallRatingLabel));
        }

        [Fact]
        public async Task Test_DashboardAnalytics_FilterByUnit()
        {
            var units = await _dashboardService.GetAvailableUnitsAsync();
            var testUnit = units.FirstOrDefault(u => u != "Tất cả");
            Assert.NotNull(testUnit);

            var criteria = new DashboardFilterCriteria { Unit = testUnit };
            var filteredRecords = await _dashboardService.GetFilteredRecordsAsync(criteria);
            var summary = await _dashboardService.GetSummaryAsync(criteria);

            Assert.NotNull(filteredRecords);
            foreach (var r in filteredRecords)
            {
                Assert.Equal(testUnit, r.Cadet?.Unit);
            }
        }

        [Fact]
        public async Task Test_DashboardAnalytics_UnitLeaderboard()
        {
            var criteria = new DashboardFilterCriteria();
            var leaderboard = await _dashboardService.GetUnitLeaderboardAsync(criteria);

            Assert.NotNull(leaderboard);
            Assert.NotEmpty(leaderboard);

            // Xếp hạng bắt đầu từ 1
            Assert.Equal(1, leaderboard[0].Rank);
            Assert.False(string.IsNullOrWhiteSpace(leaderboard[0].RankMedal));

            // Đơn vị đứng đầu phải có PassRate >= đơn vị sau
            for (int i = 0; i < leaderboard.Count - 1; i++)
            {
                Assert.True(leaderboard[i].PassRate >= leaderboard[i + 1].PassRate ||
                           (leaderboard[i].PassRate == leaderboard[i + 1].PassRate && leaderboard[i].EliteRate >= leaderboard[i + 1].EliteRate));
            }
        }

        [Fact]
        public async Task Test_DashboardAnalytics_SubjectPerformance()
        {
            var criteria = new DashboardFilterCriteria();
            var subPerfs = await _dashboardService.GetSubjectPerformancesAsync(criteria);

            Assert.NotNull(subPerfs);
            Assert.NotEmpty(subPerfs);

            // Môn có tỷ lệ trượt cao nhất phải đứng đầu danh sách
            for (int i = 0; i < subPerfs.Count - 1; i++)
            {
                Assert.True(subPerfs[i].FailRate >= subPerfs[i + 1].FailRate);
            }
        }

        [Fact]
        public async Task Test_AIRecommendation_GeneratesDirectives_And_Prescriptions()
        {
            var criteria = new DashboardFilterCriteria();
            var records = await _dashboardService.GetFilteredRecordsAsync(criteria);
            var cadets = await _cadetService.GetAllCadetsAsync();

            var aiResult = await _recommendationService.GenerateRecommendationsAsync(records, cadets);

            Assert.NotNull(aiResult);
            Assert.NotNull(aiResult.StrategicDirective);
            Assert.False(string.IsNullOrWhiteSpace(aiResult.StrategicDirective.Title));
            Assert.False(string.IsNullOrWhiteSpace(aiResult.StrategicDirective.ExecutiveSummary));
            Assert.False(string.IsNullOrWhiteSpace(aiResult.StrategicDirective.TimeAllocationDirective));

            // Có 4 nhóm tố chất rèn luyện thể lực
            Assert.Equal(4, aiResult.ComponentPrescriptions.Count);

            foreach (var pres in aiResult.ComponentPrescriptions)
            {
                Assert.False(string.IsNullOrWhiteSpace(pres.ComponentName));
                Assert.False(string.IsNullOrWhiteSpace(pres.ScientificTrainingProtocol));
                Assert.False(string.IsNullOrWhiteSpace(pres.WeeklyScheduleRecommendation));
                Assert.False(string.IsNullOrWhiteSpace(pres.UrgencyLevel));
            }
        }

        [Fact]
        public async Task Test_ExportDashboardExecutiveReport_Generates4Sheets()
        {
            var criteria = new DashboardFilterCriteria();
            var summary = await _dashboardService.GetSummaryAsync(criteria);
            var units = await _dashboardService.GetUnitLeaderboardAsync(criteria);
            var subjects = await _dashboardService.GetSubjectPerformancesAsync(criteria);
            var failed = await _dashboardService.GetFailedRecordsAsync(criteria);
            var honors = await _dashboardService.GetHonoredCadetsAsync(criteria);

            var records = await _dashboardService.GetFilteredRecordsAsync(criteria);
            var cadets = await _cadetService.GetAllCadetsAsync();
            var aiRecs = await _recommendationService.GenerateRecommendationsAsync(records, cadets);

            var filePath = Path.Combine(Path.GetTempPath(), $"Test_Dashboard_Report_{Guid.NewGuid():N}.xlsx");

            try
            {
                var result = await _excelService.ExportDashboardExecutiveReportAsync(
                    filePath, summary, units, subjects, aiRecs, failed, honors);

                Assert.True(result.Success, result.Message);
                Assert.True(File.Exists(filePath));

                // Kiểm tra 4 sheets bằng ClosedXML
                using var wb = new XLWorkbook(filePath);
                Assert.Equal(4, wb.Worksheets.Count);
                Assert.NotNull(wb.Worksheet("Tổng Quan & Thi Đua"));
                Assert.NotNull(wb.Worksheet("Đề Xuất Huấn Luyện AI"));
                Assert.NotNull(wb.Worksheet("DS Cần Bồi Dưỡng Thể Lực"));
                Assert.NotNull(wb.Worksheet("DS Biểu Dương Khen Thưởng"));
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }
            }
        }
    }
}
