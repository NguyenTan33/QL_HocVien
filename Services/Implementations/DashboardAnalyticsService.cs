using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Models.Filters;

namespace QL_HocVien.Services
{
    public class DashboardAnalyticsService : IDashboardAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly ICadetService _cadetService;
        private readonly IPhysicalExamService _examService;
        private readonly ISubjectService _subjectService;
        private readonly IClassService _classService;
        private readonly ICatalogService _catalogService;

        public DashboardAnalyticsService(
            AppDbContext context,
            ICadetService cadetService,
            IPhysicalExamService examService,
            ISubjectService subjectService,
            IClassService classService,
            ICatalogService catalogService)
        {
            _context = context;
            _cadetService = cadetService;
            _examService = examService;
            _subjectService = subjectService;
            _classService = classService;
            _catalogService = catalogService;
        }

        public async Task<List<PhysicalExamRecord>> GetFilteredRecordsAsync(DashboardFilterCriteria criteria)
        {
            var allRecords = await _examService.GetAllRecordsAsync();
            var query = allRecords.AsQueryable();

            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
            {
                query = query.Where(r => r.Cadet != null && r.Cadet.Unit == criteria.Unit);
            }

            if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Tất cả")
            {
                query = query.Where(r => r.Cadet != null && 
                    ((r.Cadet.ClassName != null && r.Cadet.ClassName.Equals(criteria.ClassName, StringComparison.OrdinalIgnoreCase)) ||
                     (r.Cadet.MilitaryClass != null && r.Cadet.MilitaryClass.ClassName.Equals(criteria.ClassName, StringComparison.OrdinalIgnoreCase))));
            }

            if (!string.IsNullOrWhiteSpace(criteria.ExamSession) && criteria.ExamSession != "Tất cả")
            {
                query = query.Where(r => r.ExamSession != null && r.ExamSession.Equals(criteria.ExamSession, StringComparison.OrdinalIgnoreCase));
            }

            if (criteria.SubjectId.HasValue && criteria.SubjectId.Value > 0)
            {
                query = query.Where(r => r.SubjectId == criteria.SubjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(criteria.Grade) && criteria.Grade != "Tất cả")
            {
                query = query.Where(r => r.Grade != null && r.Grade.Equals(criteria.Grade, StringComparison.OrdinalIgnoreCase));
            }

            if (criteria.FromDate.HasValue)
            {
                query = query.Where(r => r.ExamDate.Date >= criteria.FromDate.Value.Date);
            }

            if (criteria.ToDate.HasValue)
            {
                query = query.Where(r => r.ExamDate.Date <= criteria.ToDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(criteria.SearchKeyword))
            {
                var kw = criteria.SearchKeyword.Trim().ToLower();
                query = query.Where(r => r.Cadet != null && (
                    (!string.IsNullOrEmpty(r.Cadet.FullName) && r.Cadet.FullName.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(r.Cadet.CadetCode) && r.Cadet.CadetCode.ToLower().Contains(kw))
                ));
            }

            return query.OrderByDescending(r => r.ExamDate).ToList();
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(DashboardFilterCriteria criteria)
        {
            var allCadets = (await _cadetService.GetAllCadetsAsync()).ToList();
            var filteredRecords = await GetFilteredRecordsAsync(criteria);

            // Lọc quân số học viên theo phạm vi Unit/Class nếu có chọn
            var cadetsQuery = allCadets.AsQueryable();
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
            {
                cadetsQuery = cadetsQuery.Where(c => c.Unit == criteria.Unit);
            }
            if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Tất cả")
            {
                cadetsQuery = cadetsQuery.Where(c => 
                    (c.ClassName != null && c.ClassName.Equals(criteria.ClassName, StringComparison.OrdinalIgnoreCase)) ||
                    (c.MilitaryClass != null && c.MilitaryClass.ClassName.Equals(criteria.ClassName, StringComparison.OrdinalIgnoreCase)));
            }
            if (!string.IsNullOrWhiteSpace(criteria.SearchKeyword))
            {
                var kw = criteria.SearchKeyword.Trim().ToLower();
                cadetsQuery = cadetsQuery.Where(c => 
                    (!string.IsNullOrEmpty(c.FullName) && c.FullName.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(c.CadetCode) && c.CadetCode.ToLower().Contains(kw)));
            }

            var targetedCadets = cadetsQuery.ToList();
            int totalTestedSubjects = await GetTotalTestedSubjectsCountAsync(criteria);

            var summary = new DashboardSummaryDto
            {
                TotalCadets = targetedCadets.Count,
                TotalUnitsCount = targetedCadets.Select(c => c.Unit).Where(u => !string.IsNullOrEmpty(u)).Distinct().Count(),
                TotalClassesCount = targetedCadets.Select(c => c.ClassName ?? c.MilitaryClass?.ClassName).Where(cl => !string.IsNullOrEmpty(cl)).Distinct().Count(),
                TotalExamRecords = filteredRecords.Count,
                UniqueTestedCadets = filteredRecords.Select(r => r.CadetId).Distinct().Count(),
                TotalTestedSubjects = totalTestedSubjects,
                ExcellentCount = filteredRecords.Count(r => r.Grade == "Xuất sắc"),
                GoodCount = filteredRecords.Count(r => r.Grade == "Giỏi"),
                FairCount = filteredRecords.Count(r => r.Grade == "Khá"),
                PassCount = filteredRecords.Count(r => r.Grade == "Khá" || r.Grade == "Đạt"),
                FailCount = filteredRecords.Count(r => r.Grade == "Không đạt")
            };

            return summary;
        }

        public async Task<List<UnitLeaderboardDto>> GetUnitLeaderboardAsync(DashboardFilterCriteria criteria)
        {
            var filteredRecords = await GetFilteredRecordsAsync(criteria);
            var allCadets = (await _cadetService.GetAllCadetsAsync()).ToList();

            var unitsGroup = filteredRecords
                .Where(r => r.Cadet != null && !string.IsNullOrWhiteSpace(r.Cadet.Unit))
                .GroupBy(r => r.Cadet!.Unit)
                .ToList();

            var list = new List<UnitLeaderboardDto>();
            foreach (var g in unitsGroup)
            {
                var unitName = g.Key;
                int totalExams = g.Count();
                int fail = g.Count(r => r.Grade == "Không đạt");
                int passed = totalExams - fail;
                int elite = g.Count(r => r.Grade == "Xuất sắc" || r.Grade == "Giỏi");
                int cadetsInUnit = allCadets.Count(c => c.Unit == unitName);

                int excellent = g.Count(r => r.Grade == "Xuất sắc");
                int good = g.Count(r => r.Grade == "Giỏi");
                int fair = g.Count(r => r.Grade == "Khá" || r.Grade == "Đạt");

                list.Add(new UnitLeaderboardDto
                {
                    UnitName = unitName,
                    TotalCadets = cadetsInUnit > 0 ? cadetsInUnit : g.Select(r => r.CadetId).Distinct().Count(),
                    TotalExamRecords = totalExams,
                    PassedCount = passed,
                    EliteCount = elite,
                    FailedCount = fail,
                    ExcellentCount = excellent,
                    GoodCount = good,
                    FairCount = fair
                });
            }

            // Sắp xếp thứ tự theo chuẩn: Xuất sắc -> Giỏi -> Khá -> Trung bình, sau đó theo PassRate, EliteRate
            var sorted = list.OrderByDescending(u => u.EvaluationStatus == "Đơn vị Xuất sắc" ? 4 :
                                                     (u.EvaluationStatus == "Đơn vị Giỏi" ? 3 :
                                                     (u.EvaluationStatus == "Đơn vị Khá" ? 2 : 1)))
                             .ThenByDescending(u => u.PassRate)
                             .ThenByDescending(u => u.EliteRate)
                             .ThenByDescending(u => u.TotalExamRecords)
                             .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].Rank = i + 1;
            }

            return sorted;
        }

        public async Task<List<SubjectPerformanceDto>> GetSubjectPerformancesAsync(DashboardFilterCriteria criteria)
        {
            var filteredRecords = await GetFilteredRecordsAsync(criteria);
            var subjects = (await _subjectService.GetAllSubjectsAsync()).ToList();

            var groups = filteredRecords
                .Where(r => r.Subject != null)
                .GroupBy(r => r.SubjectId)
                .ToList();

            var list = new List<SubjectPerformanceDto>();
            foreach (var g in groups)
            {
                var first = g.First();
                int total = g.Count();
                int fail = g.Count(r => r.Grade == "Không đạt");
                int passed = total - fail;
                int elite = g.Count(r => r.Grade == "Xuất sắc" || r.Grade == "Giỏi");

                list.Add(new SubjectPerformanceDto
                {
                    SubjectId = g.Key,
                    SubjectCode = first.Subject?.SubjectCode ?? $"M{g.Key}",
                    SubjectName = first.Subject?.SubjectName ?? $"Môn {g.Key}",
                    TotalTested = total,
                    PassedCount = passed,
                    EliteCount = elite,
                    FailedCount = fail
                });
            }

            // Sắp xếp theo môn có tỷ lệ trượt cao nhất lên đầu để cảnh báo chỉ huy
            return list.OrderByDescending(s => s.FailRate)
                       .ThenBy(s => s.PassRate)
                       .ToList();
        }

        public async Task<List<CadetHonorDto>> GetHonoredCadetsAsync(DashboardFilterCriteria criteria, int topCount = 10)
        {
            var filteredRecords = await GetFilteredRecordsAsync(criteria);

            var eliteGroups = filteredRecords
                .Where(r => r.Cadet != null && (r.Grade == "Xuất sắc" || r.Grade == "Giỏi"))
                .GroupBy(r => r.CadetId)
                .ToList();

            var list = new List<CadetHonorDto>();
            foreach (var g in eliteGroups)
            {
                var cadet = g.First().Cadet!;
                int totalExams = g.Count();
                int excellentCount = g.Count(r => r.Grade == "Xuất sắc");
                int goodCount = g.Count(r => r.Grade == "Giỏi");

                var bestRecord = g.OrderByDescending(r => r.Grade == "Xuất sắc").First();

                string title = excellentCount >= 2 ? "🥇 Kiện Tướng Thể Lực" :
                               (excellentCount >= 1 ? "🥈 Chiến Sĩ Rèn Luyện Xuất Sắc" : "🥉 Chiến Sĩ Khỏe");

                list.Add(new CadetHonorDto
                {
                    CadetId = cadet.Id,
                    CadetCode = cadet.CadetCode,
                    FullName = cadet.FullName,
                    Rank = cadet.Rank,
                    Unit = cadet.Unit,
                    ClassName = cadet.ClassName ?? cadet.MilitaryClass?.ClassName ?? "",
                    TotalExams = totalExams,
                    ExcellentExams = excellentCount,
                    GoodExams = goodCount,
                    HonorTitle = title,
                    BestSubject = bestRecord.Subject?.SubjectName ?? "Toàn diện",
                    BestScore = bestRecord.ScoreValue.ToString("0.##")
                });
            }

            return list.OrderByDescending(c => c.ExcellentExams)
                       .ThenByDescending(c => c.GoodExams)
                       .ThenByDescending(c => c.TotalExams)
                       .Take(topCount)
                       .ToList();
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
            var records = await _examService.GetAllRecordsAsync();
            var sessions = records
                .Select(r => r.ExamSession)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            sessions.Insert(0, "Tất cả");
            return sessions;
        }

        public async Task<List<Subject>> GetAvailableSubjectsAsync()
        {
            var subjects = (await _subjectService.GetAllSubjectsAsync()).ToList();
            var allSubject = new Subject { Id = 0, SubjectCode = "ALL", SubjectName = "Tất cả các môn" };
            subjects.Insert(0, allSubject);
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

            // Nếu trong tháng chưa có sự kiện nào, lấy các sự kiện từ hôm nay trở đi
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

        public async Task<List<UntestedCadetDto>> GetUntestedCadetsAsync(DashboardFilterCriteria criteria)
        {
            var query = _context.Cadets
                .Include(c => c.MilitaryClass)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
                query = query.Where(c => c.Unit == criteria.Unit);

            if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Tất cả")
                query = query.Where(c => (c.ClassName != null && c.ClassName == criteria.ClassName) || (c.MilitaryClass != null && c.MilitaryClass.ClassName == criteria.ClassName));

            if (!string.IsNullOrWhiteSpace(criteria.SearchKeyword))
            {
                var kw = criteria.SearchKeyword.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(kw) || c.CadetCode.ToLower().Contains(kw));
            }

            var cadets = await query.ToListAsync();
            var allCreditSubjs = await _context.CreditSubjects.AsNoTracking().ToListAsync();
            var allScores = await _context.CreditScoreRecords.AsNoTracking().ToListAsync();
            var physicalRecords = await _context.PhysicalExamRecords.AsNoTracking().ToListAsync();

            var result = new List<UntestedCadetDto>();

            foreach (var cadet in cadets)
            {
                var takenSubjectIds = allScores
                    .Where(s => s.CadetId == cadet.Id)
                    .Select(s => s.CreditSubjectId)
                    .Distinct()
                    .ToHashSet();

                var missingSubjects = allCreditSubjs
                    .Where(s => !takenSubjectIds.Contains(s.Id))
                    .Select(s => s.SubjectName)
                    .ToList();

                bool hasPhysicalExam = physicalRecords.Any(p => p.CadetId == cadet.Id);

                if (missingSubjects.Count > 0 || !hasPhysicalExam)
                {
                    var missingList = new List<string>(missingSubjects);
                    if (!hasPhysicalExam)
                        missingList.Add("Rèn luyện thể lực (chưa có điểm)");

                    result.Add(new UntestedCadetDto
                    {
                        CadetId = cadet.Id,
                        CadetCode = cadet.CadetCode,
                        FullName = cadet.FullName,
                        Rank = cadet.Rank,
                        Unit = cadet.Unit,
                        ClassName = cadet.ClassName ?? cadet.MilitaryClass?.ClassName ?? cadet.Unit,
                        MissingSubjects = string.Join(", ", missingList),
                        MissingCount = missingList.Count,
                        ExamType = missingSubjects.Count > 0 && !hasPhysicalExam 
                            ? "Môn Tín chỉ & Thể lực" 
                            : (missingSubjects.Count > 0 ? "Môn Tín chỉ" : "Rèn luyện Thể lực"),
                        Status = "Chưa hoàn thành",
                        Note = $"Còn thiếu {missingList.Count} nội dung cần tổ chức kiểm tra bù"
                    });
                }
            }

            return result.OrderByDescending(u => u.MissingCount).ThenBy(u => u.Unit).ToList();
        }

        public async Task<int> GetTotalTestedSubjectsCountAsync(DashboardFilterCriteria criteria)
        {
            var physTested = await _context.PhysicalExamRecords
                .Select(r => r.SubjectId)
                .Distinct()
                .CountAsync();

            var creditTested = await _context.CreditScoreRecords
                .Select(r => r.CreditSubjectId)
                .Distinct()
                .CountAsync();

            int total = physTested + creditTested;
            return total > 0 ? total : physTested;
        }
    }
}
