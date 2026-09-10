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
            // 1. Lấy dữ liệu tổng hợp chuẩn từ CreditSubjectService
            var summaries = await _creditSubjectService.GetCadetAcademicSummariesAsync(unit, className, keyword);

            // 2. Lấy danh sách toàn bộ các thành phần kiểm tra
            var components = await _context.SubjectAssessmentComponents
                .Include(c => c.CreditSubject)
                .Where(c => c.CreditSubject != null && !c.CreditSubject.IsComponent)
                .OrderBy(c => c.CreditSubjectId)
                .ThenBy(c => c.OrderIndex)
                .AsNoTracking()
                .ToListAsync();

            var result = new AcademicAnalyticsResultDto();

            // 3. Xây dựng danh sách phân tích chi tiết từng học viên
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

                // Phân tích chi tiết từng môn/đợt thi
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
                        WarningMessage = isWarning ? "Đợt kiểm tra đã diễn ra nhưng chưa làm bài" : string.Empty
                    });
                }

                cadetAnalyticsList.Add(cadetDto);
            }

            // 4. Lọc theo Xếp loại nếu người dùng chọn
            if (!string.IsNullOrWhiteSpace(rating) && rating != "Tất cả xếp loại" && rating != "Tất cả")
            {
                cadetAnalyticsList = cadetAnalyticsList.Where(c => c.AcademicRating.Equals(rating, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 5. Lọc theo Trạng thái môn nếu người dùng chọn
            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả trạng thái" && status != "Tất cả")
            {
                if (status.Contains("Đủ môn"))
                {
                    cadetAnalyticsList = cadetAnalyticsList.Where(c => !c.HasMissingSubjects).ToList();
                }
                else if (status.Contains("Thiếu môn"))
                {
                    cadetAnalyticsList = cadetAnalyticsList.Where(c => c.HasMissingSubjects).ToList();
                }
            }

            result.CadetAnalytics = cadetAnalyticsList;
            result.TotalCadetsEvaluated = cadetAnalyticsList.Count;

            if (result.TotalCadetsEvaluated > 0)
            {
                result.AverageGpa = Math.Round(cadetAnalyticsList.Average(c => c.Gpa), 2);
                result.ExcellentCount = cadetAnalyticsList.Count(c => c.AcademicRating == "Giỏi");
                result.ExcellentPercentage = Math.Round((double)result.ExcellentCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.GoodCount = cadetAnalyticsList.Count(c => c.AcademicRating == "Khá");
                result.GoodPercentage = Math.Round((double)result.GoodCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.AverageCount = cadetAnalyticsList.Count(c => c.AcademicRating == "Trung bình");
                result.AveragePercentage = Math.Round((double)result.AverageCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.WeakCount = cadetAnalyticsList.Count(c => c.AcademicRating == "Yếu");
                result.WeakPercentage = Math.Round((double)result.WeakCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.MissingSubjectsCount = cadetAnalyticsList.Count(c => c.HasMissingSubjects);
                result.MissingSubjectsPercentage = Math.Round((double)result.MissingSubjectsCount * 100.0 / result.TotalCadetsEvaluated, 1);

                result.CompletedCadetsCount = result.TotalCadetsEvaluated - result.MissingSubjectsCount;
                result.CompletedCadetsPercentage = Math.Round((double)result.CompletedCadetsCount * 100.0 / result.TotalCadetsEvaluated, 1);
            }

            // 6. Tổng hợp Cấp Đại Đội & Toàn Đơn Vị
            var unitGroups = cadetAnalyticsList.GroupBy(c => c.Unit).ToList();
            var unitList = new List<AcademicUnitComparisonDto>();

            foreach (var grp in unitGroups)
            {
                var list = grp.ToList();
                int total = list.Count;
                double avgGpa = total > 0 ? Math.Round(list.Average(c => c.Gpa), 2) : 0;
                int exc = list.Count(c => c.AcademicRating == "Giỏi");
                int good = list.Count(c => c.AcademicRating == "Khá");
                int avg = list.Count(c => c.AcademicRating == "Trung bình");
                int weak = list.Count(c => c.AcademicRating == "Yếu");
                int missing = list.Count(c => c.HasMissingSubjects);
                int complete = total - missing;

                string comment;
                if (avgGpa >= 7.5 && missing == 0)
                    comment = "Đơn vị học tập xuất sắc, quân số đủ 100% môn";
                else if (avgGpa >= 7.0)
                    comment = missing > 0 ? $"Học lực Khá, cần đôn đốc {missing} đ/c thi bù" : "Đơn vị đạt danh hiệu Học tập Khá toàn diện";
                else if (avgGpa >= 6.0)
                    comment = $"Học lực trung bình, có {missing} đ/c chưa hoàn thành nội dung";
                else
                    comment = "Cần tăng cường phụ đạo và tổ chức ôn tập kiểm tra bù";

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

            // Xếp hạng thi đua học tập giữa các đơn vị
            var sortedUnits = unitList.OrderByDescending(u => u.AverageGpa).ThenByDescending(u => u.GoodRate).ToList();
            for (int i = 0; i < sortedUnits.Count; i++)
            {
                sortedUnits[i].RankInBattalion = i + 1;
            }
            result.UnitComparisons = sortedUnits;

            // 7. Tổng hợp Cấp Lớp & Phân Đội
            var classGroups = cadetAnalyticsList.GroupBy(c => new { c.ClassName, c.Unit }).ToList();
            var classList = new List<AcademicClassComparisonDto>();

            foreach (var grp in classGroups)
            {
                var list = grp.ToList();
                int total = list.Count;
                double avgGpa = total > 0 ? Math.Round(list.Average(c => c.Gpa), 2) : 0;
                int exc = list.Count(c => c.AcademicRating == "Giỏi");
                int good = list.Count(c => c.AcademicRating == "Khá");
                int avgWeak = list.Count(c => c.AcademicRating == "Trung bình" || c.AcademicRating == "Yếu");
                int missing = list.Count(c => c.HasMissingSubjects);
                int complete = total - missing;
                double goodOrAboveRate = total > 0 ? Math.Round((double)(exc + good) * 100.0 / total, 1) : 0;

                string comment;
                if (avgGpa >= 7.5)
                    comment = "Lớp dẫn đầu phong trào thi đua học tập";
                else if (avgGpa >= 6.8)
                    comment = "Lớp đạt yêu cầu học tập khá, cần duy trì";
                else
                    comment = $"Cần kèm cặp các học viên TB, còn {missing} đ/c nợ môn";

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

                    // SHEET 1: CẤP ĐẠI ĐỘI
                    var wsUnit = wb.Worksheets.Add("Cấp Đại Đội");
                    wsUnit.Cell(1, 1).Value = "BÁO CÁO SO SÁNH HỌC LỰC CẤP ĐẠI ĐỘI & TOÀN ĐƠN VỊ";
                    wsUnit.Range(1, 1, 1, 11).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    wsUnit.Cell(2, 1).Value = $"Tổng quân số: {result.TotalCadetsEvaluated} học viên | TBM chung: {result.AverageGpa:F2} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    wsUnit.Range(2, 1, 2, 11).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetItalic();

                    int uRow = 4;
                    string[] uHeaders = { "Thứ hạng", "Đơn vị / Đại đội", "Quân số", "TBM Bình Quân", "% Giỏi", "% Khá", "% Trung bình", "% Yếu", "Đủ môn", "Nợ/Thiếu môn", "Nhận xét thi đua" };
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

                    // SHEET 2: CẤP LỚP
                    var wsClass = wb.Worksheets.Add("Cấp Lớp");
                    wsClass.Cell(1, 1).Value = "BÁO CÁO XẾP HẠNG HỌC LỰC CẤP LỚP & PHÂN ĐỘI";
                    wsClass.Range(1, 1, 1, 10).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    int cRow = 3;
                    string[] cHeaders = { "Thứ hạng", "Lớp / Phân đội", "Đại đội", "Quân số", "TBM Bình Quân", "% Khá/Giỏi", "Số Giỏi", "Số Khá", "Số TB/Yếu", "Nợ/Thiếu môn" };
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

                    // SHEET 3: CHI TIẾT CÁ NHÂN HỌC VIÊN
                    var wsCadet = wb.Worksheets.Add("Chi Tiết Cá Nhân");
                    wsCadet.Cell(1, 1).Value = "BẢNG KẾT QUẢ VÀ PHÂN TÍCH HỌC LỰC CÁ NHÂN HỌC VIÊN";
                    wsCadet.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    int pRow = 3;
                    string[] pHeaders = { "STT", "Mã HV", "Họ và tên", "Cấp bậc", "Đơn vị", "Lớp", "TBM Toàn Khóa", "Xếp loại", "Tình trạng môn" };
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
                        wsCadet.Cell(pRow, 9).Value = cadet.HasMissingSubjects ? $"Thiếu {cadet.MissingSubjectsCount} đợt thi" : "Đủ tất cả môn";

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
                    return (true, $"Đã xuất báo cáo phân tích học lực thành công ({result.TotalCadetsEvaluated} học viên, 3 cấp phân tích).");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi xuất file Excel: {ex.Message}");
                }
            });
        }
    }
}
