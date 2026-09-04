using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Models.Filters;

namespace QL_HocVien.Services
{
    public class DashboardAnalyticsService : IDashboardAnalyticsService
    {
        private readonly ICadetService _cadetService;
        private readonly IPhysicalExamService _examService;
        private readonly ISubjectService _subjectService;
        private readonly IClassService _classService;
        private readonly ICatalogService _catalogService;

        public DashboardAnalyticsService(
            ICadetService cadetService,
            IPhysicalExamService examService,
            ISubjectService subjectService,
            IClassService classService,
            ICatalogService catalogService)
        {
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

            var summary = new DashboardSummaryDto
            {
                TotalCadets = targetedCadets.Count,
                TotalUnitsCount = targetedCadets.Select(c => c.Unit).Where(u => !string.IsNullOrEmpty(u)).Distinct().Count(),
                TotalClassesCount = targetedCadets.Select(c => c.ClassName ?? c.MilitaryClass?.ClassName).Where(cl => !string.IsNullOrEmpty(cl)).Distinct().Count(),
                TotalExamRecords = filteredRecords.Count,
                UniqueTestedCadets = filteredRecords.Select(r => r.CadetId).Distinct().Count(),
                ExcellentCount = filteredRecords.Count(r => r.Grade == "Xuất sắc"),
                GoodCount = filteredRecords.Count(r => r.Grade == "Giỏi"),
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

                list.Add(new UnitLeaderboardDto
                {
                    UnitName = unitName,
                    TotalCadets = cadetsInUnit > 0 ? cadetsInUnit : g.Select(r => r.CadetId).Distinct().Count(),
                    TotalExamRecords = totalExams,
                    PassedCount = passed,
                    EliteCount = elite,
                    FailedCount = fail
                });
            }

            // Sắp xếp thứ tự: Tỷ lệ đạt cao nhất, nếu bằng nhau thì tỷ lệ Giỏi cao hơn
            var sorted = list.OrderByDescending(u => u.PassRate)
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
            var units = await _catalogService.GetAllUnitsAsync();
            var list = units.Select(u => u.UnitName).OrderBy(u => u).ToList();
            list.Insert(0, "Tất cả");
            return list;
        }

        public async Task<List<string>> GetAvailableClassesAsync(string? unit = null)
        {
            var classes = (await _classService.GetAllClassesAsync()).ToList();
            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
            {
                // Có thể lọc lớp theo đơn vị nếu lớp có UnitId/UnitName
            }
            var list = classes.Select(c => c.ClassName).Distinct().OrderBy(c => c).ToList();
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
    }
}
