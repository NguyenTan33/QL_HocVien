using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Models.Filters;
using QL_HocVien.Services.Calculators;
using QL_HocVien.Services.Interfaces;

namespace QL_HocVien.Services.Implementations
{
    public class DashboardAnalyticsService : IDashboardAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly ICadetService _cadetService;
        private readonly IPhysicalExamService? _examService;
        private readonly ISubjectService? _subjectService;
        private readonly IClassService? _classService;
        private readonly ICatalogService? _catalogService;
        private readonly ICreditSubjectService _creditSubjectService;
        private readonly IAcademicAnalyticsService _academicAnalyticsService;

        public DashboardAnalyticsService(
            AppDbContext context,
            ICadetService cadetService,
            IPhysicalExamService? examService = null,
            ISubjectService? subjectService = null,
            IClassService? classService = null,
            ICatalogService? catalogService = null,
            ICreditSubjectService? creditSubjectService = null,
            IAcademicAnalyticsService? academicAnalyticsService = null)
        {
            _context = context;
            _cadetService = cadetService;
            _examService = examService;
            _subjectService = subjectService;
            _classService = classService;
            _catalogService = catalogService;
            _creditSubjectService = creditSubjectService ?? new CreditSubjectService(context);
            _academicAnalyticsService = academicAnalyticsService ?? new AcademicAnalyticsService(context, _creditSubjectService, new CreditGradeCalculator());
        }

        public async Task<AcademicAnalyticsResultDto> GetRawAcademicAnalyticsAsync(DashboardFilterCriteria criteria)
        {
            string? unit = (!string.IsNullOrWhiteSpace(criteria.Unit) && !criteria.Unit.Contains("Tất cả")) ? criteria.Unit : null;
            string? className = (!string.IsNullOrWhiteSpace(criteria.ClassName) && !criteria.ClassName.Contains("Tất cả")) ? criteria.ClassName : null;
            string? rating = (!string.IsNullOrWhiteSpace(criteria.AcademicRating) && !criteria.AcademicRating.Contains("Tất cả")) ? criteria.AcademicRating : null;
            string? status = (!string.IsNullOrWhiteSpace(criteria.Status) && !criteria.Status.Contains("Tất cả")) ? criteria.Status : null;

            return await _academicAnalyticsService.GetAcademicAnalyticsAsync(
                unit: unit,
                className: className,
                rating: rating,
                status: status,
                keyword: criteria.SearchKeyword);
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(DashboardFilterCriteria criteria)
        {
            var academicData = await GetRawAcademicAnalyticsAsync(criteria);
            var cadets = academicData.CadetAnalytics;

            int totalCadets = cadets.Count;
            int totalUnits = cadets.Select(c => c.Unit).Where(u => !string.IsNullOrEmpty(u)).Distinct().Count();
            int totalClasses = cadets.Select(c => c.ClassName).Where(cl => !string.IsNullOrEmpty(cl)).Distinct().Count();

            var allSubjects = await _context.CreditSubjects.AsNoTracking().Where(s => !s.IsComponent).ToListAsync();
            int totalCreditSubjects = allSubjects.Count;

            var scoresQuery = _context.CreditScoreRecords.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(criteria.ExamSession) && criteria.ExamSession != "Tất cả")
            {
                scoresQuery = scoresQuery.Where(s => s.ExamSession == criteria.ExamSession);
            }
            if (criteria.SubjectId.HasValue && criteria.SubjectId.Value > 0)
            {
                scoresQuery = scoresQuery.Where(s => s.CreditSubjectId == criteria.SubjectId.Value);
            }
            int totalScoresCount = await scoresQuery.CountAsync();

            int excellentCount = cadets.Count(c => c.AcademicRating == "Giỏi");
            int goodCount = cadets.Count(c => c.AcademicRating == "Khá");
            int fairCount = cadets.Count(c => c.AcademicRating == "Trung bình");
            int weakCount = cadets.Count(c => c.AcademicRating == "Yếu");
            int completedCount = cadets.Count(c => !c.HasMissingSubjects);
            int warningCount = cadets.Count(c => c.HasMissingSubjects || c.Gpa < 5.0);

            double avgGpa = totalCadets > 0 ? Math.Round(cadets.Average(c => c.Gpa), 2) : 0.0;

            int totalRecordsBasis = totalCadets > 0 ? totalCadets : totalScoresCount;

            var summary = new DashboardSummaryDto
            {
                TotalCadets = totalCadets,
                TotalUnitsCount = totalUnits,
                TotalClassesCount = totalClasses,
                TotalCreditSubjects = totalCreditSubjects,
                TotalCreditScores = totalScoresCount,
                TotalTestedSubjects = totalCreditSubjects,
                TotalExamRecords = totalRecordsBasis,
                UniqueTestedCadets = cadets.Count(c => c.Gpa > 0),
                AverageGpa = avgGpa,
                ExcellentCount = excellentCount,
                GoodCount = goodCount,
                FairCount = fairCount,
                PassCount = excellentCount + goodCount + fairCount,
                FailCount = weakCount,
                WarningCount = warningCount,
                CompletedCadetsCount = completedCount
            };

            return summary;
        }

        public async Task<List<UnitLeaderboardDto>> GetUnitLeaderboardAsync(DashboardFilterCriteria criteria)
        {
            var academicData = await GetRawAcademicAnalyticsAsync(criteria);
            var list = new List<UnitLeaderboardDto>();

            foreach (var u in academicData.UnitComparisons)
            {
                int totalCadets = u.TotalCadets;
                int passed = u.ExcellentCount + u.GoodCount + u.AverageCount;
                int elite = u.ExcellentCount + u.GoodCount;

                list.Add(new UnitLeaderboardDto
                {
                    UnitName = u.UnitName,
                    TotalCadets = totalCadets,
                    TotalExamRecords = totalCadets,
                    PassedCount = passed,
                    EliteCount = elite,
                    FailedCount = u.WeakCount,
                    ExcellentCount = u.ExcellentCount,
                    GoodCount = u.GoodCount,
                    FairCount = u.AverageCount,
                    AverageGpa = u.AverageGpa
                });
            }

            var sorted = list.OrderByDescending(u => u.PassRate)
                             .ThenByDescending(u => u.EliteRate)
                             .ThenByDescending(u => u.AverageGpa)
                             .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].Rank = i + 1;
            }

            return sorted;
        }

        public async Task<List<AcademicClassComparisonDto>> GetClassLeaderboardAsync(DashboardFilterCriteria criteria)
        {
            var academicData = await GetRawAcademicAnalyticsAsync(criteria);
            return academicData.ClassComparisons;
        }

        public async Task<List<SubjectPerformanceDto>> GetSubjectPerformancesAsync(DashboardFilterCriteria criteria)
        {
            var subjects = await _context.CreditSubjects
                .Include(s => s.Components)
                .Where(s => !s.IsComponent)
                .AsNoTracking()
                .ToListAsync();

            if (criteria.SubjectId.HasValue && criteria.SubjectId.Value > 0)
            {
                subjects = subjects.Where(s => s.Id == criteria.SubjectId.Value).ToList();
            }

            var scoresQuery = _context.CreditScoreRecords
                .Include(s => s.Cadet)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
            {
                scoresQuery = scoresQuery.Where(s => s.Cadet != null && s.Cadet.Unit == criteria.Unit);
            }
            if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Tất cả")
            {
                scoresQuery = scoresQuery.Where(s => s.Cadet != null && (s.Cadet.ClassName == criteria.ClassName || (s.Cadet.MilitaryClass != null && s.Cadet.MilitaryClass.ClassName == criteria.ClassName)));
            }
            if (!string.IsNullOrWhiteSpace(criteria.ExamSession) && criteria.ExamSession != "Tất cả")
            {
                scoresQuery = scoresQuery.Where(s => s.ExamSession == criteria.ExamSession);
            }

            var scores = await scoresQuery.ToListAsync();
            var scoresBySub = scores.GroupBy(s => s.CreditSubjectId).ToDictionary(g => g.Key, g => g.ToList());

            var list = new List<SubjectPerformanceDto>();
            foreach (var sub in subjects)
            {
                scoresBySub.TryGetValue(sub.Id, out var subScores);
                subScores ??= new List<CreditScoreRecord>();

                int total = subScores.Count;
                int passed = subScores.Count(s => s.FinalScore >= 5.0);
                int elite = subScores.Count(s => s.FinalScore >= 8.0);
                int failed = subScores.Count(s => s.FinalScore < 5.0);
                double avgScore = total > 0 ? Math.Round(subScores.Average(s => s.FinalScore), 2) : 0.0;

                list.Add(new SubjectPerformanceDto
                {
                    SubjectId = sub.Id,
                    SubjectCode = sub.SubjectCode,
                    SubjectName = sub.SubjectName,
                    Credits = sub.CalculatedTotalCredits,
                    AssessmentType = sub.AssessmentType,
                    AverageScore = avgScore,
                    TotalTested = total,
                    PassedCount = passed,
                    EliteCount = elite,
                    FailedCount = failed
                });
            }

            return list.OrderByDescending(s => s.FailRate)
                       .ThenBy(s => s.PassRate)
                       .ThenByDescending(s => s.TotalTested)
                       .ToList();
        }

        public async Task<List<CadetHonorDto>> GetHonoredCadetsAsync(DashboardFilterCriteria criteria, int topCount = 15)
        {
            var academicData = await GetRawAcademicAnalyticsAsync(criteria);
            var cadets = academicData.CadetAnalytics
                .Where(c => c.Gpa >= 7.5)
                .OrderByDescending(c => c.Gpa)
                .ThenByDescending(c => c.TotalCreditsEarned)
                .Take(topCount)
                .ToList();

            var list = new List<CadetHonorDto>();
            int rank = 1;
            foreach (var c in cadets)
            {
                string title;
                if (rank == 1 || c.Gpa >= 9.0) title = "🥇 Thủ Khoa - Học Viên Xuất Sắc";
                else if (c.Gpa >= 8.5) title = "🥈 Học Viên Giỏi Toàn Diện";
                else if (c.Gpa >= 8.0) title = "🥉 Học Viên Tiêu Biểu";
                else title = "⭐ Học Viên Khá Xuất Sắc";

                var bestDetail = c.SubjectDetails.Where(d => d.Score.HasValue).OrderByDescending(d => d.Score!.Value).FirstOrDefault();

                list.Add(new CadetHonorDto
                {
                    CadetId = c.CadetId,
                    CadetCode = c.CadetCode,
                    FullName = c.FullName,
                    Rank = c.Rank,
                    Unit = c.Unit,
                    ClassName = c.ClassName,
                    TotalExams = c.SubjectDetails.Count(d => d.Score.HasValue),
                    ExcellentExams = c.SubjectDetails.Count(d => d.Score.HasValue && d.Score.Value >= 8.5),
                    GoodExams = c.SubjectDetails.Count(d => d.Score.HasValue && d.Score.Value >= 7.0 && d.Score.Value < 8.5),
                    Gpa = c.Gpa,
                    TotalCreditsEarned = c.TotalCreditsEarned,
                    HonorTitle = title,
                    BestSubject = bestDetail?.SubjectName ?? (string.IsNullOrEmpty(bestDetail?.ComponentName) ? "Toàn diện" : bestDetail.ComponentName),
                    BestScore = bestDetail?.ScoreDisplay ?? $"{c.Gpa:F2}"
                });
                rank++;
            }

            return list;
        }

        public async Task<List<AcademicWarningCadetDto>> GetAcademicWarningCadetsAsync(DashboardFilterCriteria criteria)
        {
            var academicData = await GetRawAcademicAnalyticsAsync(criteria);
            var cadets = academicData.CadetAnalytics;

            var list = new List<AcademicWarningCadetDto>();
            foreach (var c in cadets)
            {
                var failedSubjects = c.SubjectDetails.Where(d => d.Score.HasValue && d.Score.Value < 5.0).ToList();
                int failedCount = failedSubjects.Count;
                string failedDisplay = failedCount > 0 ? string.Join(", ", failedSubjects.Select(f => f.ComponentName)) : string.Empty;

                bool isWarning = c.Gpa < 5.0 || c.HasMissingSubjects || failedCount > 0;
                if (isWarning)
                {
                    string reason;
                    if (c.Gpa > 0 && c.Gpa < 5.0 && (c.HasMissingSubjects || failedCount > 0))
                    {
                        reason = $"GPA yếu ({c.Gpa:F2}) & nợ {(c.MissingSubjectsCount + failedCount)} nội dung";
                    }
                    else if (c.Gpa > 0 && c.Gpa < 5.0)
                    {
                        reason = $"Điểm TBM yếu ({c.Gpa:F2} < 5.0)";
                    }
                    else if (failedCount > 0)
                    {
                        reason = $"Chưa đạt {failedCount} học phần: {failedDisplay}";
                    }
                    else
                    {
                        reason = $"Chưa hoàn thành {c.MissingSubjectsCount} đợt kiểm tra / thi";
                    }

                    list.Add(new AcademicWarningCadetDto
                    {
                        CadetId = c.CadetId,
                        CadetCode = c.CadetCode,
                        FullName = c.FullName,
                        Rank = c.Rank,
                        Unit = c.Unit,
                        ClassName = c.ClassName,
                        Gpa = c.Gpa,
                        TotalCreditsEarned = c.TotalCreditsEarned,
                        MissingSubjectsCount = c.MissingSubjectsCount,
                        MissingSubjectsDisplay = c.MissingSubjectsDisplay,
                        FailedSubjectsCount = failedCount,
                        FailedSubjectsDisplay = failedDisplay,
                        WarningReason = reason,
                        ActionPlan = failedCount > 0 ? "Đăng ký thi lại học phần; tham gia phụ đạo chuyên đề" : "Tổ chức kiểm tra bù; hoàn thành chuẩn tín chỉ"
                    });
                }
            }

            return list.OrderBy(w => w.Gpa).ThenByDescending(w => w.MissingSubjectsCount + w.FailedSubjectsCount).ToList();
        }

        public async Task<List<AcademicCadetAnalyticsDto>> GetCadetCumulativeAnalyticsAsync(DashboardFilterCriteria criteria)
        {
            var academicData = await GetRawAcademicAnalyticsAsync(criteria);
            return academicData.CadetAnalytics;
        }

        public async Task<List<UntestedCadetDto>> GetUntestedCadetsAsync(DashboardFilterCriteria criteria)
        {
            var academicData = await GetRawAcademicAnalyticsAsync(criteria);
            var untested = academicData.CadetAnalytics
                .Where(c => c.HasMissingSubjects)
                .Select(c => new UntestedCadetDto
                {
                    CadetId = c.CadetId,
                    CadetCode = c.CadetCode,
                    FullName = c.FullName,
                    Rank = c.Rank,
                    Unit = c.Unit,
                    ClassName = c.ClassName,
                    MissingSubjects = c.MissingSubjectsDisplay,
                    MissingCount = c.MissingSubjectsCount,
                    ExamType = "Học phần Tín chỉ",
                    Status = "Chưa hoàn thành",
                    Note = $"Còn thiếu {c.MissingSubjectsCount} nội dung cần tổ chức kiểm tra bù"
                })
                .OrderByDescending(u => u.MissingCount)
                .ThenBy(u => u.Unit)
                .ToList();

            return untested;
        }

        public async Task<List<PhysicalExamRecord>> GetFilteredRecordsAsync(DashboardFilterCriteria criteria)
        {
            if (_examService != null)
            {
                var allRecords = await _examService.GetAllRecordsAsync();
                var query = allRecords.AsQueryable();

                if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
                    query = query.Where(r => r.Cadet != null && r.Cadet.Unit == criteria.Unit);

                if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Tất cả")
                    query = query.Where(r => r.Cadet != null && 
                        ((r.Cadet.ClassName != null && r.Cadet.ClassName.Equals(criteria.ClassName, StringComparison.OrdinalIgnoreCase)) ||
                         (r.Cadet.MilitaryClass != null && r.Cadet.MilitaryClass.ClassName.Equals(criteria.ClassName, StringComparison.OrdinalIgnoreCase))));

                return query.ToList();
            }
            return new List<PhysicalExamRecord>();
        }

        public async Task<List<PhysicalExamRecord>> GetFailedRecordsAsync(DashboardFilterCriteria criteria)
        {
            var filtered = await GetFilteredRecordsAsync(criteria);
            return filtered.Where(r => r.Grade == "Không đạt").ToList();
        }

        public async Task<List<string>> GetAvailableUnitsAsync()
        {
            var units = await _cadetService.GetDistinctUnitsAsync();
            var list = units.OrderBy(u => u).ToList();
            list.Insert(0, "Tất cả");
            return list;
        }

        public async Task<List<string>> GetAvailableClassesAsync(string? unit = null)
        {
            List<string> list;
            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
            {
                list = await _context.Cadets
                    .Where(c => c.Unit == unit && !string.IsNullOrWhiteSpace(c.ClassName))
                    .Select(c => c.ClassName.Trim())
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            else
            {
                list = await _cadetService.GetDistinctClassesAsync();
            }
            list.Insert(0, "Tất cả");
            return list;
        }

        public async Task<List<string>> GetAvailableSessionsAsync()
        {
            var sessions = await _context.CreditScoreRecords
                .Select(r => r.ExamSession)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            if (sessions.Count == 0)
            {
                sessions = new List<string> { "Học kỳ 1", "Học kỳ 2", "Học kỳ Hè" };
            }

            sessions.Insert(0, "Tất cả");
            return sessions;
        }

        public async Task<List<Subject>> GetAvailableSubjectsAsync()
        {
            if (_subjectService != null)
            {
                var subjects = (await _subjectService.GetAllSubjectsAsync()).ToList();
                subjects.Insert(0, new Subject { Id = 0, SubjectCode = "ALL", SubjectName = "Tất cả các môn" });
                return subjects;
            }
            return new List<Subject> { new Subject { Id = 0, SubjectCode = "ALL", SubjectName = "Tất cả các môn" } };
        }

        public async Task<List<CreditSubject>> GetAvailableCreditSubjectsAsync()
        {
            var subjects = await _creditSubjectService.GetAllSubjectsAsync();
            subjects.Insert(0, new CreditSubject { Id = 0, SubjectCode = "ALL", SubjectName = "Tất cả học phần tín chỉ" });
            return subjects;
        }

        public async Task<List<TrainingEvent>> GetMonthlyFocusEventsAsync()
        {
            var today = DateTime.Today;
            var currentMonth = today.Month;
            var currentYear = today.Year;

            var events = await _context.TrainingEvents
                .Where(e => e.StartDate.Month == currentMonth && e.StartDate.Year == currentYear)
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            if (events.Count == 0)
            {
                events = await _context.TrainingEvents
                    .Where(e => e.StartDate >= today)
                    .OrderBy(e => e.StartDate)
                    .Take(10)
                    .ToListAsync();
            }

            return events;
        }

        public async Task<int> GetTotalTestedSubjectsCountAsync(DashboardFilterCriteria criteria)
        {
            return await _context.CreditSubjects.Where(s => !s.IsComponent).CountAsync();
        }
    }
}
