using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _context;

        public AnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetAvailableSessionsAsync()
        {
            return await _context.PhysicalExamRecords
                .Select(r => r.ExamSession)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderByDescending(s => s)
                .ToListAsync();
        }

        private static int GetGradeWeight(string grade) => grade switch
        {
            "Xuất sắc" => 5,
            "Giỏi" => 4,
            "Khá" => 3,
            "Đạt" => 2,
            "Không đạt" => 1,
            _ => 0
        };

        public async Task<List<CadetTrendDto>> CompareCadetsAsync(
            string baselineSession, 
            string compareSession, 
            string? unit = null, 
            int? classId = null, 
            string? keyword = null, 
            TrendDirection? trendFilter = null)
        {
            var baselineRecords = await _context.PhysicalExamRecords
                .Include(r => r.Subject)
                .Where(r => r.ExamSession == baselineSession)
                .ToListAsync();

            var compareRecords = await _context.PhysicalExamRecords
                .Include(r => r.Subject)
                .Where(r => r.ExamSession == compareSession)
                .ToListAsync();

            var cadetIds = baselineRecords.Select(r => r.CadetId)
                .Union(compareRecords.Select(r => r.CadetId))
                .Distinct()
                .ToList();

            var cadetsQuery = _context.Cadets
                .Where(c => cadetIds.Contains(c.Id))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
            {
                cadetsQuery = cadetsQuery.Where(c => c.Unit == unit);
            }

            if (classId.HasValue && classId.Value > 0)
            {
                cadetsQuery = cadetsQuery.Where(c => c.ClassId == classId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                cadetsQuery = cadetsQuery.Where(c => c.FullName.ToLower().Contains(kw) || c.CadetCode.ToLower().Contains(kw));
            }

            var cadets = await cadetsQuery.ToListAsync();
            var subjects = await _context.Subjects.ToListAsync();

            var result = new List<CadetTrendDto>();

            foreach (var cadet in cadets)
            {
                var cBaseline = baselineRecords.Where(r => r.CadetId == cadet.Id).ToList();
                var cCompare = compareRecords.Where(r => r.CadetId == cadet.Id).ToList();

                var trendDto = new CadetTrendDto
                {
                    CadetId = cadet.Id,
                    CadetCode = cadet.CadetCode,
                    FullName = cadet.FullName,
                    Rank = cadet.Rank,
                    Unit = cadet.Unit,
                    ClassName = cadet.ClassName
                };

                var commonSubjectIds = cBaseline.Select(r => r.SubjectId)
                    .Intersect(cCompare.Select(r => r.SubjectId))
                    .Distinct()
                    .ToList();

                var evalSubjectIds = commonSubjectIds.Any() ? commonSubjectIds : cCompare.Select(r => r.SubjectId).Distinct().ToList();

                int growth = 0, unchanged = 0, regression = 0;

                foreach (var subId in evalSubjectIds)
                {
                    var baseRec = cBaseline.FirstOrDefault(r => r.SubjectId == subId);
                    var compRec = cCompare.FirstOrDefault(r => r.SubjectId == subId);
                    var sub = subjects.FirstOrDefault(s => s.Id == subId) ?? baseRec?.Subject ?? compRec?.Subject;

                    if (baseRec == null || compRec == null || sub == null)
                        continue;

                    var item = new SubjectTrendItemDto
                    {
                        SubjectId = sub.Id,
                        SubjectCode = sub.SubjectCode,
                        SubjectName = sub.SubjectName,
                        Category = sub.Category,
                        Unit = sub.Unit,
                        IsHigherBetter = sub.IsHigherBetter,
                        BaselineScore = baseRec.ScoreValue,
                        CompareScore = compRec.ScoreValue,
                        ScoreDelta = Math.Round(compRec.ScoreValue - baseRec.ScoreValue, 2),
                        BaselineGrade = baseRec.Grade,
                        CompareGrade = compRec.Grade
                    };

                    // Tiêu chí tăng trưởng dựa trên IsHigherBetter và Grade
                    bool isBetter;
                    bool isWorse;

                    if (sub.IsHigherBetter)
                    {
                        isBetter = item.ScoreDelta > 0.0001;
                        isWorse = item.ScoreDelta < -0.0001;
                    }
                    else
                    {
                        isBetter = item.ScoreDelta < -0.0001;
                        isWorse = item.ScoreDelta > 0.0001;
                    }

                    int baseWeight = GetGradeWeight(baseRec.Grade);
                    int compWeight = GetGradeWeight(compRec.Grade);

                    if (isBetter || (!isWorse && compWeight > baseWeight))
                    {
                        item.Trend = TrendDirection.Growth;
                        growth++;
                    }
                    else if (isWorse || compWeight < baseWeight)
                    {
                        item.Trend = TrendDirection.Regression;
                        regression++;
                    }
                    else
                    {
                        item.Trend = TrendDirection.Unchanged;
                        unchanged++;
                    }

                    string sign = item.ScoreDelta > 0 ? "+" : "";
                    item.DetailDescription = $"{sign}{item.ScoreDelta} {item.Unit} ({item.BaselineGrade} ➔ {item.CompareGrade})";

                    trendDto.SubjectTrends.Add(item);
                }

                trendDto.GrowthCount = growth;
                trendDto.UnchangedCount = unchanged;
                trendDto.RegressionCount = regression;

                if (growth > regression)
                {
                    trendDto.OverallTrend = TrendDirection.Growth;
                }
                else if (regression > growth)
                {
                    trendDto.OverallTrend = TrendDirection.Regression;
                }
                else
                {
                    trendDto.OverallTrend = TrendDirection.Unchanged;
                }

                trendDto.OverallBaselineGrade = EvaluateCadetOverallGrade(cBaseline);
                trendDto.OverallCompareGrade = EvaluateCadetOverallGrade(cCompare);

                trendDto.SummaryText = $"{growth} môn tăng, {unchanged} môn giữ, {regression} môn giảm";

                if (trendFilter.HasValue && trendDto.OverallTrend != trendFilter.Value)
                {
                    continue;
                }

                result.Add(trendDto);
            }

            return result;
        }

        private static string EvaluateCadetOverallGrade(List<PhysicalExamRecord> records)
        {
            if (!records.Any()) return "Chưa kiểm tra";
            if (records.Any(r => r.Grade == "Không đạt")) return "Không đạt";
            if (records.All(r => r.Grade == "Xuất sắc")) return "Xuất sắc";
            if (records.All(r => r.Grade == "Xuất sắc" || r.Grade == "Giỏi")) return "Giỏi";
            if (records.All(r => r.Grade == "Xuất sắc" || r.Grade == "Giỏi" || r.Grade == "Khá")) return "Khá";
            return "Đạt";
        }

        public async Task<List<UnitComparisonDto>> CompareUnitsAsync(string baselineSession, string compareSession)
        {
            var cadetTrends = await CompareCadetsAsync(baselineSession, compareSession);
            var result = new List<UnitComparisonDto>();

            var groupedUnits = cadetTrends.GroupBy(c => c.Unit).OrderBy(g => g.Key);

            foreach (var group in groupedUnits)
            {
                var unitName = string.IsNullOrWhiteSpace(group.Key) ? "Chưa phân đơn vị" : group.Key;
                int total = group.Count();
                if (total == 0) continue;

                int basePass = group.Count(c => c.OverallBaselineGrade != "Không đạt" && c.OverallBaselineGrade != "Chưa kiểm tra");
                int compPass = group.Count(c => c.OverallCompareGrade != "Không đạt" && c.OverallCompareGrade != "Chưa kiểm tra");

                int baseExc = group.Count(c => c.OverallBaselineGrade == "Xuất sắc" || c.OverallBaselineGrade == "Giỏi");
                int compExc = group.Count(c => c.OverallCompareGrade == "Xuất sắc" || c.OverallCompareGrade == "Giỏi");

                int growthCount = group.Count(c => c.OverallTrend == TrendDirection.Growth);
                int unchangedCount = group.Count(c => c.OverallTrend == TrendDirection.Unchanged);
                int regressionCount = group.Count(c => c.OverallTrend == TrendDirection.Regression);

                var dto = new UnitComparisonDto
                {
                    UnitName = unitName,
                    TotalCadets = total,
                    BaselinePassCount = basePass,
                    BaselinePassRate = Math.Round((double)basePass / total * 100, 1),
                    ComparePassCount = compPass,
                    ComparePassRate = Math.Round((double)compPass / total * 100, 1),
                    BaselineExcellentRate = Math.Round((double)baseExc / total * 100, 1),
                    CompareExcellentRate = Math.Round((double)compExc / total * 100, 1),
                    GrowthCadetsCount = growthCount,
                    UnchangedCadetsCount = unchangedCount,
                    RegressionCadetsCount = regressionCount
                };

                if (dto.PassRateDelta > 0)
                {
                    dto.EvaluationComment = $"Tỷ lệ đạt tăng {dto.PassRateDelta:F1}%, có {growthCount} đồng chí tiến bộ vượt bậc.";
                }
                else if (dto.PassRateDelta < 0)
                {
                    dto.EvaluationComment = $"Tỷ lệ đạt giảm {Math.Abs(dto.PassRateDelta):F1}%, cần chấn chỉnh {regressionCount} đồng chí sút giảm.";
                }
                else
                {
                    dto.EvaluationComment = $"Duy trì kết quả rèn luyện ổn định ({dto.ComparePassRate:F1}%).";
                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<List<ClassComparisonDto>> CompareClassesAsync(string baselineSession, string compareSession, string? unit = null)
        {
            var cadetTrends = await CompareCadetsAsync(baselineSession, compareSession, unit);
            var result = new List<ClassComparisonDto>();

            var groupedClasses = cadetTrends.GroupBy(c => new { c.Unit, c.ClassName }).OrderBy(g => g.Key.Unit).ThenBy(g => g.Key.ClassName);

            foreach (var group in groupedClasses)
            {
                string className = string.IsNullOrWhiteSpace(group.Key.ClassName) ? "Chưa xếp lớp" : group.Key.ClassName;
                int total = group.Count();
                if (total == 0) continue;

                int basePass = group.Count(c => c.OverallBaselineGrade != "Không đạt" && c.OverallBaselineGrade != "Chưa kiểm tra");
                int compPass = group.Count(c => c.OverallCompareGrade != "Không đạt" && c.OverallCompareGrade != "Chưa kiểm tra");

                int baseExc = group.Count(c => c.OverallBaselineGrade == "Xuất sắc" || c.OverallBaselineGrade == "Giỏi");
                int compExc = group.Count(c => c.OverallCompareGrade == "Xuất sắc" || c.OverallCompareGrade == "Giỏi");

                var dto = new ClassComparisonDto
                {
                    ClassName = className,
                    Unit = group.Key.Unit,
                    TotalCadets = total,
                    BaselinePassRate = Math.Round((double)basePass / total * 100, 1),
                    ComparePassRate = Math.Round((double)compPass / total * 100, 1),
                    BaselineExcellentRate = Math.Round((double)baseExc / total * 100, 1),
                    CompareExcellentRate = Math.Round((double)compExc / total * 100, 1),
                    GrowthCadetsCount = group.Count(c => c.OverallTrend == TrendDirection.Growth),
                    UnchangedCadetsCount = group.Count(c => c.OverallTrend == TrendDirection.Unchanged),
                    RegressionCadetsCount = group.Count(c => c.OverallTrend == TrendDirection.Regression)
                };

                result.Add(dto);
            }

            // Xếp hạng thi đua các lớp trong từng đơn vị
            foreach (var uGroup in result.GroupBy(c => c.Unit))
            {
                int rank = 1;
                foreach (var cls in uGroup.OrderByDescending(c => c.ComparePassRate).ThenByDescending(c => c.GrowthCadetsCount))
                {
                    cls.RankInUnit = rank++;
                }
            }

            return result;
        }

        public async Task<ExamComparisonResultDto> CompareSessionsAsync(
            string baselineSession, 
            string compareSession, 
            string? unit = null, 
            int? classId = null, 
            string? keyword = null)
        {
            var cadetTrends = await CompareCadetsAsync(baselineSession, compareSession, unit, classId, keyword);
            var unitComparisons = await CompareUnitsAsync(baselineSession, compareSession);
            var classComparisons = await CompareClassesAsync(baselineSession, compareSession, unit);

            int total = cadetTrends.Count;
            int growth = cadetTrends.Count(c => c.OverallTrend == TrendDirection.Growth);
            int unchanged = cadetTrends.Count(c => c.OverallTrend == TrendDirection.Unchanged);
            int regression = cadetTrends.Count(c => c.OverallTrend == TrendDirection.Regression);

            int basePass = cadetTrends.Count(c => c.OverallBaselineGrade != "Không đạt" && c.OverallBaselineGrade != "Chưa kiểm tra");
            int compPass = cadetTrends.Count(c => c.OverallCompareGrade != "Không đạt" && c.OverallCompareGrade != "Chưa kiểm tra");

            return new ExamComparisonResultDto
            {
                BaselineSession = baselineSession,
                CompareSession = compareSession,
                TotalEvaluatedCadets = total,
                OverallGrowthCount = growth,
                OverallUnchangedCount = unchanged,
                OverallRegressionCount = regression,
                BaselinePassRate = total > 0 ? Math.Round((double)basePass / total * 100, 1) : 0,
                ComparePassRate = total > 0 ? Math.Round((double)compPass / total * 100, 1) : 0,
                UnitComparisons = unitComparisons,
                ClassComparisons = classComparisons,
                CadetTrends = cadetTrends
            };
        }
    }
}
