using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Services.Calculators;

namespace QL_HocVien.Services.Implementations
{
    public class AcademicAnalyticsService : IAcademicAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly ICreditSubjectService _creditSubjectService;
        private readonly ICreditGradeCalculator _calculator;

        public AcademicAnalyticsService(
            AppDbContext context,
            ICreditSubjectService creditSubjectService,
            ICreditGradeCalculator calculator)
        {
            _context = context;
            _creditSubjectService = creditSubjectService;
            _calculator = calculator;
        }

        public async Task<AcademicAnalyticsResultDto> GetAcademicAnalyticsAsync(
            string? unit = null,
            string? className = null,
            string? rating = null,
            string? status = null,
            string? keyword = null)
        {
            // 1. Láº¥y dá»¯ liá»‡u tá»•ng há»£p chuáº©n tá»« CreditSubjectService
            var summaries = await _creditSubjectService.GetCadetAcademicSummariesAsync(unit, className, keyword);

            // 2. Láº¥y danh sÃ¡ch toÃ n bá»™ cÃ¡c thÃ nh pháº§n kiá»ƒm tra
            var components = await _context.SubjectAssessmentComponents
                .Include(c => c.CreditSubject)
                .Where(c => c.CreditSubject != null && !c.CreditSubject.IsComponent)
                .OrderBy(c => c.CreditSubjectId)
                .ThenBy(c => c.OrderIndex)
                .AsNoTracking()
                .ToListAsync();

            var result = new AcademicAnalyticsResultDto();

            // 3. XÃ¢y dá»±ng danh sÃ¡ch phÃ¢n tÃ­ch chi tiáº¿t tá»«ng há»c viÃªn
            var cadetAnalyticsList = new List<AcademicCadetAnalyticsDto>();

            foreach (var s in summaries)
            {
                var cadetDto = new AcademicCadetAnalyticsDto
                {
                    CadetId = s.CadetId,
                    CadetCode = s.CadetCode,
                    FullName = s.FullName,
                    Rank = s.Rank,
                    Unit = s.Unit,
                    ClassName = s.ClassName,
                    Gpa = s.Gpa,
                    TotalCreditsEarned = s.TotalCreditsEarned,
                    TotalCurriculumCredits = s.TotalCurriculumCredits,
                    HasMissingSubjects = s.HasMissingSubjects,
                    MissingSubjectsCount = s.MissingSubjectsCount,
                    MissingSubjectsDisplay = s.MissingSubjectsDisplay
                };

                // PhÃ¢n tÃ­ch chi tiáº¿t tá»«ng mÃ´n/Ä‘á»£t thi
                foreach (var comp in components)
                {
                    s.ComponentScores.TryGetValue(comp.Id, out double? scoreVal);
                    bool isWarning = s.MissingSubjectsList.Contains(comp.ComponentName);

                    cadetDto.SubjectDetails.Add(new AcademicCadetSubjectDetailDto
                    {
                        SubjectName = comp.CreditSubject?.SubjectName ?? string.Empty,
                        ComponentName = comp.ComponentName,
                        Credits = comp.Credits,
                        Score = scoreVal,
                        IsWarning = isWarning,
                        WarningMessage = isWarning ? "Äá»£t kiá»ƒm tra Ä‘Ã£ diá»…n ra nhÆ°ng chÆ°a lÃ m bÃ i" : string.Empty
                    });
                }

                cadetAnalyticsList.Add(cadetDto);
            }

            // 4. Lá»c theo Xáº¿p loáº¡i náº¿u ngÆ°á»i dÃ¹ng chá»n
            if (!string.IsNullOrWhiteSpace(rating) && rating != "Táº¥t cáº£ xáº¿p loáº¡i" && rating != "Táº¥t cáº£")
            {
                cadetAnalyticsList = cadetAnalyticsList.Where(c => c.AcademicRating.Equals(rating, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 5. Lá»c theo Tráº¡ng thÃ¡i mÃ´n náº¿u ngÆ°á»i dÃ¹ng chá»n
            if (!string.IsNullOrWhiteSpace(status) && status != "Táº¥t cáº£ tráº¡ng thÃ¡i" && status != "Táº¥t cáº£")
            {
                if (status.Contains("Äá»§ mÃ´n"))
                {
                    cadetAnalyticsList = cadetAnalyticsList.Where(c => !c.HasMissingSubjects).ToList();
                }
                else if (status.Contains("Thiáº¿u mÃ´n"))
                {
                    cadetAnalyticsList = cadetAnalyticsList.Where(c => c.HasMissingSubjects).ToList();
                }
            }

            result.CadetAnalytics = cadetAnalyticsList;
            result.TotalCadetsEvaluated = cadetAnalyticsList.Count;

            if (result.TotalCadetsEvaluated > 0)
            {
                result.AverageGpa = Math.Round(cadetAnalyticsList.Average(c => c.Gpa), 2);
                result.ExcellentCount = cadetAnalyticsList.Count(c => c.AcademicRating == "Giá»i");
                result.ExcellentPercentage = Math.Round((double)result.ExcellentCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.GoodCount = cadetAnalyticsList.Count(c => c.AcademicRating == "KhÃ¡");
                result.GoodPercentage = Math.Round((double)result.GoodCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.AverageCount = cadetAnalyticsList.Count(c => c.AcademicRating == "Trung bÃ¬nh");
                result.AveragePercentage = Math.Round((double)result.AverageCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.WeakCount = cadetAnalyticsList.Count(c => c.AcademicRating == "Yáº¿u");
                result.WeakPercentage = Math.Round((double)result.WeakCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.MissingSubjectsCount = cadetAnalyticsList.Count(c => c.HasMissingSubjects);
                result.MissingSubjectsPercentage = Math.Round((double)result.MissingSubjectsCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.CompletedCadetsCount = result.TotalCadetsEvaluated - result.MissingSubjectsCount;
                result.CompletedCadetsPercentage = Math.Round((double)result.CompletedCadetsCount * 100.0 / result.TotalCadetsEvaluated, 1);
            }

            // 6. Tá»•ng há»£p Cáº¥p Äáº¡i Äá»™i & ToÃ n ÄÆ¡n Vá»‹
            var unitGroups = cadetAnalyticsList.GroupBy(c => c.Unit).ToList();
            var unitList = new List<AcademicUnitComparisonDto>();

            foreach (var grp in unitGroups)
            {
                var list = grp.ToList();
                int total = list.Count;
                double avgGpa = total > 0 ? Math.Round(list.Average(c => c.Gpa), 2) : 0;
                int exc = list.Count(c => c.AcademicRating == "Giá»i");
                int good = list.Count(c => c.AcademicRating == "KhÃ¡");
                int avg = list.Count(c => c.AcademicRating == "Trung bÃ¬nh");
                int weak = list.Count(c => c.AcademicRating == "Yáº¿u");
                int missing = list.Count(c => c.HasMissingSubjects);
                int complete = total - missing;

                string comment;
                if (avgGpa >= 7.5 && missing == 0)
                    comment = "ÄÆ¡n vá»‹ há»c táº­p xuáº¥t sáº¯c, quÃ¢n sá»‘ Ä‘á»§ 100% mÃ´n";
                else if (avgGpa >= 7.0)
                    comment = missing > 0 ? $"Há»c lá»±c KhÃ¡, cáº§n Ä‘Ã´n Ä‘á»‘c {missing} Ä‘/c thi bÃ¹" : "ÄÆ¡n vá»‹ Ä‘áº¡t danh hiá»‡u Há»c táº­p KhÃ¡ toÃ n diá»‡n";
                else if (avgGpa >= 6.0)
                    comment = $"Há»c lá»±c trung bÃ¬nh, cÃ³ {missing} Ä‘/c chÆ°a hoÃ n thÃ nh ná»™i dung";
                else
                    comment = "Cáº§n tÄƒng cÆ°á»ng phá»¥ Ä‘áº¡o vÃ  tá»• chá»©c Ã´n táº­p kiá»ƒm tra bÃ¹";

                unitList.Add(new AcademicUnitComparisonDto
                {
                    UnitName = grp.Key,
                    TotalCadets = total,
                    AverageGpa = avgGpa,
                    ExcellentCount = exc,
                    ExcellentRate = total > 0 ? Math.Round((double)exc * 100.0 / total, 1) : 0,
                    GoodCount = good,
                    GoodRate = total > 0 ? Math.Round((double)good * 100.0 / total, 1) : 0,
                    AverageCount = avg,
                    AverageRate = total > 0 ? Math.Round((double)avg * 100.0 / total, 1) : 0,
                    WeakCount = weak,
                    WeakRate = total > 0 ? Math.Round((double)weak * 100.0 / total, 1) : 0,
                    CompletedCount = complete,
                    MissingCount = missing,
                    EvaluationComment = comment
                });
            }

            // Xáº¿p háº¡ng thi Ä‘ua há»c táº­p giá»¯a cÃ¡c Ä‘Æ¡n vá»‹
            var sortedUnits = unitList.OrderByDescending(u => u.AverageGpa).ThenByDescending(u => u.GoodRate).ToList();
            for (int i = 0; i < sortedUnits.Count; i++)
            {
                sortedUnits[i].RankInBattalion = i + 1;
            }
            result.UnitComparisons = sortedUnits;

            // 7. Tá»•ng há»£p Cáº¥p Lá»›p & PhÃ¢n Äá»™i
            var classGroups = cadetAnalyticsList.GroupBy(c => new { c.ClassName, c.Unit }).ToList();
            var classList = new List<AcademicClassComparisonDto>();

            foreach (var grp in classGroups)
            {
                var list = grp.ToList();
                int total = list.Count;
                double avgGpa = total > 0 ? Math.Round(list.Average(c => c.Gpa), 2) : 0;
                int exc = list.Count(c => c.AcademicRating == "Giá»i");
                int good = list.Count(c => c.AcademicRating == "KhÃ¡");
                int avgWeak = list.Count(c => c.AcademicRating == "Trung bÃ¬nh" || c.AcademicRating == "Yáº¿u");
                int missing = list.Count(c => c.HasMissingSubjects);
                int complete = total - missing;
                double goodOrAboveRate = total > 0 ? Math.Round((double)(exc + good) * 100.0 / total, 1) : 0;

                string comment;
                if (avgGpa >= 7.5)
                    comment = "Lá»›p dáº«n Ä‘áº§u phong trÃ o thi Ä‘ua há»c táº­p";
                else if (avgGpa >= 6.8)
                    comment = "Lá»›p Ä‘áº¡t yÃªu cáº§u há»c táº­p khÃ¡, cáº§n duy trÃ¬";
                else
                    comment = $"Cáº§n kÃ¨m cáº·p cÃ¡c há»c viÃªn TB, cÃ²n {missing} Ä‘/c ná»£ mÃ´n";

                classList.Add(new AcademicClassComparisonDto
                {
                    ClassName = grp.Key.ClassName,
                    Unit = grp.Key.Unit,
                    TotalCadets = total,
                    AverageGpa = avgGpa,
                    ExcellentCount = exc,
                    GoodCount = good,
                    AverageWeakCount = avgWeak,
                    GoodOrAboveRate = goodOrAboveRate,
                    CompletedCount = complete,
                    MissingCount = missing,
                    EvaluationComment = comment
                });
            }

            var sortedClasses = classList.OrderByDescending(c => c.AverageGpa).ThenByDescending(c => c.GoodOrAboveRate).ToList();
            for (int i = 0; i < sortedClasses.Count; i++)
            {
                sortedClasses[i].RankInUnit = i + 1;
            }
            result.ClassComparisons = sortedClasses;

            return result;
        }

        public async Task<(bool Success, string Message)> ExportAcademicAnalyticsToExcelAsync(
            AcademicAnalyticsResultDto result,
            string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var wb = new XLWorkbook();

                    // SHEET 1: Cáº¤P Äáº I Äá»˜I
                    var wsUnit = wb.Worksheets.Add("Cáº¥p Äáº¡i Äá»™i");
                    wsUnit.Cell(1, 1).Value = "BÃO CÃO SO SÃNH Há»ŒC Lá»°C Cáº¤P Äáº I Äá»˜I & TOÃ€N ÄÆ N Vá»Š";
                    wsUnit.Range(1, 1, 1, 11).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    wsUnit.Cell(2, 1).Value = $"Tá»•ng quÃ¢n sá»‘: {result.TotalCadetsEvaluated} há»c viÃªn | TBM chung: {result.AverageGpa:F2} | NgÃ y xuáº¥t: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    wsUnit.Range(2, 1, 2, 11).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetItalic();

                    int uRow = 4;
                    string[] uHeaders = { "Thá»© háº¡ng", "ÄÆ¡n vá»‹ / Äáº¡i Ä‘á»™i", "QuÃ¢n sá»‘", "TBM BÃ¬nh QuÃ¢n", "% Giá»i", "% KhÃ¡", "% Trung bÃ¬nh", "% Yáº¿u", "Äá»§ mÃ´n", "Ná»£/Thiáº¿u mÃ´n", "Nháº­n xÃ©t thi Ä‘ua" };
                    for (int i = 0; i < uHeaders.Length; i++)
                    {
                        wsUnit.Cell(uRow, i + 1).Value = uHeaders[i];
                    }
                    wsUnit.Range(uRow, 1, uRow, uHeaders.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    uRow++;
                    foreach (var u in result.UnitComparisons)
                    {
                        wsUnit.Cell(uRow, 1).Value = u.RankInBattalion;
                        wsUnit.Cell(uRow, 2).Value = u.UnitName;
                        wsUnit.Cell(uRow, 3).Value = u.TotalCadets;
                        wsUnit.Cell(uRow, 4).Value = u.AverageGpa;
                        wsUnit.Cell(uRow, 5).Value = $"{u.ExcellentRate:F1}%";
                        wsUnit.Cell(uRow, 6).Value = $"{u.GoodRate:F1}%";
                        wsUnit.Cell(uRow, 7).Value = $"{u.AverageRate:F1}%";
                        wsUnit.Cell(uRow, 8).Value = $"{u.WeakRate:F1}%";
                        wsUnit.Cell(uRow, 9).Value = u.CompletedCount;
                        wsUnit.Cell(uRow, 10).Value = u.MissingCount;
                        wsUnit.Cell(uRow, 11).Value = u.EvaluationComment;

                        wsUnit.Cell(uRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsUnit.Cell(uRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsUnit.Cell(uRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsUnit.Cell(uRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsUnit.Cell(uRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsUnit.Cell(uRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsUnit.Cell(uRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsUnit.Cell(uRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsUnit.Cell(uRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        uRow++;
                    }
                    wsUnit.Range(4, 1, uRow - 1, uHeaders.Length).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    wsUnit.Columns().AdjustToContents();

                    // SHEET 2: Cáº¤P Lá»šP
                    var wsClass = wb.Worksheets.Add("Cáº¥p Lá»›p");
                    wsClass.Cell(1, 1).Value = "BÃO CÃO Xáº¾P Háº NG Há»ŒC Lá»°C Cáº¤P Lá»šP & PHÃ‚N Äá»˜I";
                    wsClass.Range(1, 1, 1, 10).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    int cRow = 3;
                    string[] cHeaders = { "Thá»© háº¡ng", "Lá»›p / PhÃ¢n Ä‘á»™i", "Äáº¡i Ä‘á»™i", "QuÃ¢n sá»‘", "TBM BÃ¬nh QuÃ¢n", "% KhÃ¡/Giá»i", "Sá»‘ Giá»i", "Sá»‘ KhÃ¡", "Sá»‘ TB/Yáº¿u", "Ná»£/Thiáº¿u mÃ´n" };
                    for (int i = 0; i < cHeaders.Length; i++)
                    {
                        wsClass.Cell(cRow, i + 1).Value = cHeaders[i];
                    }
                    wsClass.Range(cRow, 1, cRow, cHeaders.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    cRow++;
                    foreach (var c in result.ClassComparisons)
                    {
                        wsClass.Cell(cRow, 1).Value = c.RankInUnit;
                        wsClass.Cell(cRow, 2).Value = c.ClassName;
                        wsClass.Cell(cRow, 3).Value = c.Unit;
                        wsClass.Cell(cRow, 4).Value = c.TotalCadets;
                        wsClass.Cell(cRow, 5).Value = c.AverageGpa;
                        wsClass.Cell(cRow, 6).Value = $"{c.GoodOrAboveRate:F1}%";
                        wsClass.Cell(cRow, 7).Value = c.ExcellentCount;
                        wsClass.Cell(cRow, 8).Value = c.GoodCount;
                        wsClass.Cell(cRow, 9).Value = c.AverageWeakCount;
                        wsClass.Cell(cRow, 10).Value = c.MissingCount;

                        wsClass.Cell(cRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsClass.Cell(cRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsClass.Cell(cRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsClass.Cell(cRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsClass.Cell(cRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsClass.Cell(cRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsClass.Cell(cRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsClass.Cell(cRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cRow++;
                    }
                    wsClass.Range(3, 1, cRow - 1, cHeaders.Length).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    wsClass.Columns().AdjustToContents();

                    // SHEET 3: CHI TIáº¾T CÃ NHÃ‚N Há»ŒC VIÃŠN
                    var wsCadet = wb.Worksheets.Add("Chi Tiáº¿t CÃ¡ NhÃ¢n");
                    wsCadet.Cell(1, 1).Value = "Báº¢NG Káº¾T QUáº¢ VÃ€ PHÃ‚N TÃCH Há»ŒC Lá»°C CÃ NHÃ‚N Há»ŒC VIÃŠN";
                    wsCadet.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    int pRow = 3;
                    string[] pHeaders = { "STT", "MÃ£ HV", "Há» vÃ  tÃªn", "Cáº¥p báº­c", "ÄÆ¡n vá»‹", "Lá»›p", "TBM ToÃ n KhÃ³a", "Xáº¿p loáº¡i", "TÃ¬nh tráº¡ng mÃ´n" };
                    for (int i = 0; i < pHeaders.Length; i++)
                    {
                        wsCadet.Cell(pRow, i + 1).Value = pHeaders[i];
                    }
                    wsCadet.Range(pRow, 1, pRow, pHeaders.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    pRow++;
                    int stt = 1;
                    foreach (var cadet in result.CadetAnalytics)
                    {
                        if (cadet.HasMissingSubjects)
                        {
                            wsCadet.Range(pRow, 1, pRow, pHeaders.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF9C3");
                        }

                        wsCadet.Cell(pRow, 1).Value = stt++;
                        wsCadet.Cell(pRow, 2).Value = cadet.CadetCode;
                        wsCadet.Cell(pRow, 3).Value = cadet.FullName;
                        wsCadet.Cell(pRow, 4).Value = cadet.Rank;
                        wsCadet.Cell(pRow, 5).Value = cadet.Unit;
                        wsCadet.Cell(pRow, 6).Value = cadet.ClassName;
                        wsCadet.Cell(pRow, 7).Value = cadet.Gpa;
                        wsCadet.Cell(pRow, 8).Value = cadet.AcademicRating;
                        wsCadet.Cell(pRow, 9).Value = cadet.HasMissingSubjects ? $"Thiáº¿u {cadet.MissingSubjectsCount} Ä‘á»£t thi" : "Äá»§ táº¥t cáº£ mÃ´n";

                        wsCadet.Cell(pRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsCadet.Cell(pRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsCadet.Cell(pRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsCadet.Cell(pRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsCadet.Cell(pRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsCadet.Cell(pRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsCadet.Cell(pRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsCadet.Cell(pRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        pRow++;
                    }
                    wsCadet.Range(3, 1, pRow - 1, pHeaders.Length).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    wsCadet.Columns().AdjustToContents();

                    wb.SaveAs(filePath);
                    return (true, $"ÄÃ£ xuáº¥t bÃ¡o cÃ¡o phÃ¢n tÃ­ch há»c lá»±c thÃ nh cÃ´ng ({result.TotalCadetsEvaluated} há»c viÃªn, 3 cáº¥p phÃ¢n tÃ­ch).");
                }
                catch (Exception ex)
                {
                    return (false, $"Lá»—i khi xuáº¥t file Excel: {ex.Message}");
                }
            });
        }
    }
}

