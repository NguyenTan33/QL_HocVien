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

namespace QL_HocVien.Services
{
    public class CreditSubjectService : ICreditSubjectService
    {
        private readonly AppDbContext _context;
        private readonly ICreditGradeCalculator _calculator;

        public CreditSubjectService(AppDbContext context, ICreditGradeCalculator? calculator = null)
        {
            _context = context;
            _calculator = calculator ?? new CreditGradeCalculator();
        }

        public async Task<List<CreditSubject>> GetAllSubjectsAsync()
        {
            return await _context.CreditSubjects
                .Include(s => s.Components)
                .Where(s => !s.IsComponent)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }

        public async Task<List<CreditSubject>> GetMajorSubjectsAsync()
        {
            return await _context.CreditSubjects
                .Include(s => s.Components)
                .Where(s => !s.IsComponent)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }

        public async Task<CreditSubject?> GetSubjectByIdAsync(int id)
        {
            return await _context.CreditSubjects.FindAsync(id);
        }

        public async Task<(bool Success, string Message)> AddSubjectAsync(CreditSubject subject)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subject.SubjectCode))
                    return (false, "Mã môn học không được để trống.");
                if (string.IsNullOrWhiteSpace(subject.SubjectName))
                    return (false, "Tên môn học không được để trống.");
                if (subject.Credits <= 0)
                    return (false, "Số tín chỉ phải lớn hơn 0.");

                subject.SubjectCode = subject.SubjectCode.Trim().ToUpper();
                subject.SubjectName = subject.SubjectName.Trim();

                bool exists = await _context.CreditSubjects.AnyAsync(s => s.SubjectCode == subject.SubjectCode);
                if (exists)
                    return (false, $"Mã môn học '{subject.SubjectCode}' đã tồn tại trong hệ thống.");

                _context.CreditSubjects.Add(subject);
                await _context.SaveChangesAsync();
                return (true, "Thêm môn học tín chỉ thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi thêm môn học: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateSubjectAsync(CreditSubject subject)
        {
            try
            {
                var existing = await _context.CreditSubjects.FindAsync(subject.Id);
                if (existing == null)
                    return (false, "Không tìm thấy môn học cần sửa.");

                subject.SubjectCode = subject.SubjectCode.Trim().ToUpper();
                subject.SubjectName = subject.SubjectName.Trim();

                bool codeConflict = await _context.CreditSubjects
                    .AnyAsync(s => s.SubjectCode == subject.SubjectCode && s.Id != subject.Id);
                if (codeConflict)
                    return (false, $"Mã môn học '{subject.SubjectCode}' đã được sử dụng bởi môn khác.");

                existing.SubjectCode = subject.SubjectCode;
                existing.SubjectName = subject.SubjectName;
                existing.Credits = subject.Credits;
                existing.AssessmentType = subject.AssessmentType;
                existing.Description = subject.Description;

                await _context.SaveChangesAsync();
                return (true, "Cập nhật môn học tín chỉ thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi cập nhật môn học: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteSubjectAsync(int id)
        {
            try
            {
                var subject = await _context.CreditSubjects.FindAsync(id);
                if (subject == null)
                    return (false, "Không tìm thấy môn học.");

                _context.CreditSubjects.Remove(subject);
                await _context.SaveChangesAsync();
                return (true, "Xóa môn học tín chỉ thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xóa môn học: {ex.Message}");
            }
        }

        public async Task<List<CreditScoreRecord>> GetAllScoresAsync()
        {
            return await _context.CreditScoreRecords
                .Include(s => s.Cadet)
                .Include(s => s.CreditSubject)
                .OrderByDescending(s => s.ExamDate)
                .ToListAsync();
        }

        public async Task<List<CreditScoreRecord>> GetScoresByCadetIdAsync(int cadetId)
        {
            return await _context.CreditScoreRecords
                .Include(s => s.CreditSubject)
                .Where(s => s.CadetId == cadetId)
                .OrderBy(s => s.CreditSubject != null ? s.CreditSubject.SubjectName : string.Empty)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> SaveScoreAsync(CreditScoreRecord score)
        {
            try
            {
                if (score.FinalScore < 0 || score.FinalScore > 10)
                    return (false, "Điểm môn học phải nằm trong khoảng từ 0.0 đến 10.0.");

                var existing = await _context.CreditScoreRecords
                    .FirstOrDefaultAsync(s => s.CadetId == score.CadetId && s.CreditSubjectId == score.CreditSubjectId && s.ExamSession == score.ExamSession);

                if (existing != null)
                {
                    existing.RegularScore = score.RegularScore;
                    existing.ExamScore = score.ExamScore;
                    existing.FinalScore = score.FinalScore;
                    existing.ExamDate = score.ExamDate;
                    existing.Notes = score.Notes;
                }
                else
                {
                    _context.CreditScoreRecords.Add(score);
                }

                await _context.SaveChangesAsync();
                return (true, "Lưu điểm môn học tín chỉ thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi lưu điểm: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteScoreAsync(int scoreId)
        {
            try
            {
                var record = await _context.CreditScoreRecords.FindAsync(scoreId);
                if (record == null)
                    return (false, "Không tìm thấy bản ghi điểm.");

                _context.CreditScoreRecords.Remove(record);
                await _context.SaveChangesAsync();
                return (true, "Xóa điểm thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xóa điểm: {ex.Message}");
            }
        }

        public async Task<List<CadetAcademicSummaryDto>> GetCadetAcademicSummariesAsync(
            string? unit = null, string? className = null, string? keyword = null)
        {
            var query = _context.Cadets
                .Include(c => c.MilitaryClass)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
                query = query.Where(c => c.Unit == unit);

            if (!string.IsNullOrWhiteSpace(className) && className != "Tất cả")
                query = query.Where(c => c.MilitaryClass != null && c.MilitaryClass.ClassName == className);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(keyword) || c.CadetCode.ToLower().Contains(keyword));
            }

            var cadets = await query.ToListAsync();
            var allScores = await _context.CreditScoreRecords
                .AsNoTracking()
                .ToListAsync();

            // Lấy danh sách các môn lớn và các đợt thi thành phần trực thuộc
            var majorSubjects = await _context.CreditSubjects
                .Include(s => s.Components)
                .Where(s => !s.IsComponent)
                .AsNoTracking()
                .ToListAsync();

            var allComponents = majorSubjects
                .SelectMany(s => s.Components)
                .OrderBy(c => c.CreditSubjectId)
                .ThenBy(c => c.OrderIndex)
                .ToList();

            // Đếm số lượng học viên có điểm cho từng đợt kiểm tra / thành phần con
            // Quy tắc: Nếu có >= 20 học viên có điểm thì đợt kiểm tra đó coi như đã diễn ra
            var componentScoreCounts = allScores
                .Where(s => s.ComponentId.HasValue && s.FinalScore >= 0)
                .GroupBy(s => s.ComponentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(s => s.CadetId).Distinct().Count());

            var activeComponentIds = componentScoreCounts
                .Where(kvp => kvp.Value >= 20)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            // Tổng tín chỉ toàn khóa chuẩn (tính từ các đợt thi hoặc 62.90 theo file Excel chuẩn)
            double curriculumCredits = allComponents.Sum(c => c.Credits);
            if (curriculumCredits <= 0) curriculumCredits = 62.90;

            var result = new List<CadetAcademicSummaryDto>();

            foreach (var cadet in cadets)
            {
                var cadetScores = allScores.Where(s => s.CadetId == cadet.Id).ToList();
                var dto = new CadetAcademicSummaryDto
                {
                    CadetId = cadet.Id,
                    CadetCode = cadet.CadetCode,
                    FullName = cadet.FullName,
                    Rank = cadet.Rank,
                    Unit = cadet.Unit,
                    ClassName = cadet.MilitaryClass?.ClassName ?? cadet.Unit,
                    TotalCurriculumCredits = Math.Round(curriculumCredits, 2)
                };

                // Điểm theo từng đợt kiểm tra của học viên này
                var cadetCompScores = new Dictionary<int, double?>();
                foreach (var comp in allComponents)
                {
                    var rec = cadetScores.FirstOrDefault(s => s.ComponentId == comp.Id);
                    if (rec != null && rec.FinalScore >= 0)
                    {
                        cadetCompScores[comp.Id] = rec.FinalScore;
                        dto.ComponentScores[comp.Id] = rec.FinalScore;
                    }
                    else
                    {
                        cadetCompScores[comp.Id] = null;
                        dto.ComponentScores[comp.Id] = null;
                    }
                }

                // Tính điểm môn lớn: Điểm trung bình môn CHỈ CÓ KHI 100% CÁC CỘT CỦA MÔN CHÍNH ĐƯỢC NHẬP
                foreach (var subj in majorSubjects)
                {
                    var subjComps = subj.Components.OrderBy(c => c.OrderIndex).ToList();
                    if (subjComps.Count == 0)
                    {
                        dto.SubjectScores[subj.Id] = null;
                        continue;
                    }

                    bool allEntered = subjComps.All(c => cadetCompScores.TryGetValue(c.Id, out var sc) && sc.HasValue && sc.Value >= 0);
                    if (allEntered)
                    {
                        var pairs = subjComps.Select(c => (score: cadetCompScores[c.Id], credits: c.Credits));
                        dto.SubjectScores[subj.Id] = _calculator.CalculateSubjectScoreFromComponents(pairs, subj.Credits);
                    }
                    else
                    {
                        dto.SubjectScores[subj.Id] = null;
                    }
                }

                // Kiểm tra thiếu đợt thi trong các đợt ĐÃ DIỄN RA (>= 20 học viên có điểm)
                // Nếu đợt kiểm tra chưa có ai thi (< 20 học sinh) thì coi như chưa diễn ra và KHÔNG bị đánh vàng
                var missingActiveComponentNames = new List<string>();
                foreach (var comp in allComponents)
                {
                    if (activeComponentIds.Contains(comp.Id))
                    {
                        if (!cadetCompScores.TryGetValue(comp.Id, out var sc) || !sc.HasValue || sc.Value < 0)
                        {
                            missingActiveComponentNames.Add(comp.ComponentName);
                        }
                    }
                }

                dto.MissingSubjectsList = missingActiveComponentNames;
                dto.MissingSubjectsCount = missingActiveComponentNames.Count;

                // Tính GPA tích lũy toàn khóa trên các đợt thi đã hoàn thành
                var scoredComponents = allComponents
                    .Where(c => cadetCompScores.TryGetValue(c.Id, out var sc) && sc.HasValue && sc.Value >= 0)
                    .Select(c => (score: cadetCompScores[c.Id]!.Value, credits: c.Credits));

                dto.Gpa = _calculator.CalculateCurriculumTbm(scoredComponents, curriculumCredits);
                dto.TotalCreditsEarned = Math.Round(scoredComponents.Sum(sc => sc.credits), 2);
                dto.TotalSubjectsCompleted = majorSubjects.Count(s => dto.SubjectScores.TryGetValue(s.Id, out var sc) && sc.HasValue);

                // Xây dựng bảng phân rã điểm thành phần
                dto.MajorSubjectBreakdowns = new List<MajorSubjectBreakdownDto>();
                foreach (var subj in majorSubjects)
                {
                    var compList = new List<ComponentScoreDto>();
                    var subjComps = subj.Components.OrderBy(c => c.OrderIndex).ToList();
                    foreach (var comp in subjComps)
                    {
                        cadetCompScores.TryGetValue(comp.Id, out var compScore);
                        double weightRatio = subj.Credits > 0 ? comp.Credits / subj.Credits : 1.0;
                        double? contrib = compScore.HasValue && compScore.Value >= 0
                            ? Math.Round((compScore.Value * comp.Credits) / (subj.Credits > 0 ? subj.Credits : 1.0), 2)
                            : null;

                        compList.Add(new ComponentScoreDto
                        {
                            ComponentName = comp.ComponentName,
                            Credits = comp.Credits,
                            RecordedScore = compScore,
                            WeightRatio = Math.Round(weightRatio, 4),
                            ContributionScore = contrib
                        });
                    }

                    bool isComplete = compList.All(c => c.RecordedScore.HasValue && c.RecordedScore.Value >= 0);
                    double? finalScore = dto.SubjectScores.TryGetValue(subj.Id, out var fs) ? fs : null;

                    dto.MajorSubjectBreakdowns.Add(new MajorSubjectBreakdownDto
                    {
                        MajorSubjectName = subj.SubjectName,
                        TotalCredits = subj.Credits,
                        FinalScore = finalScore,
                        IsComplete = isComplete,
                        Components = compList
                    });
                }

                result.Add(dto);
            }

            return result.OrderByDescending(r => r.Gpa).ThenBy(r => r.FullName).ToList();
        }

        public async Task<List<UntestedCadetDto>> GetUntestedCadetsAsync(
            string? unit = null, string? className = null, string? keyword = null)
        {
            var query = _context.Cadets
                .Include(c => c.MilitaryClass)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
                query = query.Where(c => c.Unit == unit);

            if (!string.IsNullOrWhiteSpace(className) && className != "Tất cả")
                query = query.Where(c => c.MilitaryClass != null && c.MilitaryClass.ClassName == className);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(keyword) || c.CadetCode.ToLower().Contains(keyword));
            }

            var cadets = await query.ToListAsync();
            var allSubjects = await _context.CreditSubjects.AsNoTracking().ToListAsync();
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

                var missingSubjects = allSubjects
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
                        ClassName = cadet.MilitaryClass?.ClassName ?? cadet.Unit,
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

        public async Task<List<MajorSubjectBreakdownDto>> GetSubjectBreakdownForCadetAsync(int cadetId)
        {
            var cadetScores = await _context.CreditScoreRecords
                .Where(s => s.CadetId == cadetId)
                .OrderByDescending(s => s.ExamDate)
                .ToListAsync();

            var subjects = await _context.CreditSubjects.AsNoTracking().ToListAsync();
            var dict = cadetScores
                .GroupBy(s => s.CreditSubjectId)
                .ToDictionary(g => g.Key, g => (double?)g.First().FinalScore);

            return _calculator.BuildMajorSubjectBreakdowns(subjects, dict);
        }

        public async Task<(bool Success, string Message)> ExportAcademicReportAsync(
            string filePath, List<CadetAcademicSummaryDto> summaries, List<CreditSubject> subjects)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Bảng Điểm TBM Chuẩn");

                    // Lấy danh sách toàn bộ các đợt thi / kiểm tra thành phần sắp xếp theo môn chính
                    var components = _context.SubjectAssessmentComponents
                        .Include(c => c.CreditSubject)
                        .Where(c => c.CreditSubject != null && !c.CreditSubject.IsComponent)
                        .OrderBy(c => c.CreditSubjectId)
                        .ThenBy(c => c.OrderIndex)
                        .ToList();

                    if (!components.Any())
                    {
                        components = subjects.Select(s => new SubjectAssessmentComponent
                        {
                            Id = s.Id,
                            CreditSubjectId = s.Id,
                            ComponentName = s.SubjectName,
                            Credits = s.Credits
                        }).ToList();
                    }

                    // Dòng 1: Số tín chỉ của từng đợt kiểm tra / thi (Khớp file Điểm TBM chuẩn .xlsx)
                    int startCol = 6;
                    int col = startCol;
                    double totalCurriculumCredits = components.Sum(c => c.Credits);
                    if (totalCurriculumCredits <= 0) totalCurriculumCredits = 62.90;

                    foreach (var comp in components)
                    {
                        ws.Cell(1, col).Value = comp.Credits;
                        ws.Cell(1, col).Style.NumberFormat.Format = "0.00";
                        ws.Cell(1, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        col++;
                    }

                    // Ô tổng tín chỉ toàn khóa tại dòng 1 của cột TBM (ví dụ ô BK1 = 62.90)
                    int tbmCol = col;
                    ws.Cell(1, tbmCol).Value = totalCurriculumCredits;
                    ws.Cell(1, tbmCol).Style.NumberFormat.Format = "0.00";
                    ws.Cell(1, tbmCol).Style.Font.Bold = true;
                    ws.Cell(1, tbmCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Tiêu đề báo cáo
                    ws.Cell(3, 1).Value = "KẾT QUẢ HỌC TẬP TOÀN KHÓA VÀ ĐIỂM TRUNG BÌNH MÔN (TBM)";
                    ws.Range(3, 1, 3, tbmCol + 3).Merge();
                    ws.Cell(3, 1).Style.Font.Bold = true;
                    ws.Cell(3, 1).Style.Font.FontSize = 14;
                    ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
                    ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Dòng 4 & 5: Tiêu đề cột
                    int hRow1 = 4;
                    int hRow2 = 5;

                    ws.Cell(hRow1, 1).Value = "TT";
                    ws.Cell(hRow1, 2).Value = "Đơn vị";
                    ws.Cell(hRow1, 3).Value = "Họ và tên đệm";
                    ws.Cell(hRow1, 4).Value = "Tên";
                    ws.Cell(hRow1, 5).Value = "Họ và tên ghép";

                    col = startCol;
                    foreach (var comp in components)
                    {
                        // Xuất tên từng đợt kiểm tra / thi (vd: CNTT1, CNTT2, CNTT, Thi CNTT...)
                        ws.Cell(hRow2, col).Value = comp.ComponentName;
                        col++;
                    }

                    ws.Cell(hRow2, tbmCol).Value = "TBM";
                    ws.Cell(hRow1, tbmCol + 1).Value = "Xếp loại học tập";
                    ws.Cell(hRow1, tbmCol + 2).Value = "Số môn <7";
                    ws.Cell(hRow1, tbmCol + 3).Value = "Số môn đã học";

                    int lastCol = tbmCol + 3;

                    var headerRange = ws.Range(hRow1, 1, hRow2, lastCol);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    headerRange.Style.Alignment.WrapText = true;

                    // Dòng dữ liệu học viên (bắt đầu từ dòng 6)
                    int row = 6;
                    int stt = 1;

                    foreach (var item in summaries)
                    {
                        // Kiểm tra học viên thiếu môn: TÔ TOÀN BỘ DÒNG MÀU VÀNG (#FFFF00) NHƯ FILE EXCEL GỐC
                        if (item.HasMissingSubjects)
                        {
                            ws.Range(row, 1, row, lastCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF00");
                        }

                        ws.Cell(row, 1).Value = stt++;
                        ws.Cell(row, 2).Value = item.Unit; // Phân đội (b1, b2, b3...)

                        // Tách họ đệm và tên
                        string name = item.FullName.Trim();
                        int lastSpace = name.LastIndexOf(' ');
                        string lastName = lastSpace > 0 ? name.Substring(0, lastSpace).Trim() : string.Empty;
                        string firstName = lastSpace > 0 ? name.Substring(lastSpace + 1).Trim() : name;

                        ws.Cell(row, 3).Value = lastName;
                        ws.Cell(row, 4).Value = firstName;
                        ws.Cell(row, 5).Value = item.FullName;

                        col = startCol;
                        int belowSevenCount = 0;
                        int completedCount = 0;

                        foreach (var comp in components)
                        {
                            // Lấy điểm theo từng đợt kiểm tra / thành phần con
                            if (item.ComponentScores.TryGetValue(comp.Id, out var score) && score.HasValue && score.Value >= 0)
                            {
                                ws.Cell(row, col).Value = score.Value;
                                ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                completedCount++;
                                if (score.Value <= 6.99) belowSevenCount++;
                            }
                            else
                            {
                                // Nếu các đợt kiểm tra chưa có điểm khi xuất excel CỨ ĐỂ TRỐNG Ô HOÀN TOÀN
                                ws.Cell(row, col).Value = string.Empty;
                            }
                            col++;
                        }

                        // Cột TBM (ROUNDDOWN 2 số thập phân)
                        ws.Cell(row, tbmCol).Value = item.Gpa;
                        ws.Cell(row, tbmCol).Style.NumberFormat.Format = "0.00";
                        ws.Cell(row, tbmCol).Style.Font.Bold = true;
                        ws.Cell(row, tbmCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Cột Xếp loại
                        ws.Cell(row, tbmCol + 1).Value = item.AcademicRating;
                        ws.Cell(row, tbmCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, tbmCol + 1).Style.Font.Bold = true;

                        // Cột Số môn < 7
                        ws.Cell(row, tbmCol + 2).Value = belowSevenCount;
                        ws.Cell(row, tbmCol + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Cột Số môn đã học
                        ws.Cell(row, tbmCol + 3).Value = completedCount;
                        ws.Cell(row, tbmCol + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        row++;
                    }

                    // HÀNG CUỐI CÙNG CỦA EXCEL: DÒNG TÍN CHỈ CỦA TỪNG ĐỢT KIỂM TRA HOẶC THI
                    int footerRow = row;
                    ws.Cell(footerRow, 1).Value = "TÍN CHỈ ĐỢT THI / KIỂM TRA";
                    ws.Range(footerRow, 1, footerRow, 5).Merge();
                    ws.Cell(footerRow, 1).Style.Font.Bold = true;
                    ws.Cell(footerRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(footerRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

                    col = startCol;
                    foreach (var comp in components)
                    {
                        ws.Cell(footerRow, col).Value = comp.Credits;
                        ws.Cell(footerRow, col).Style.NumberFormat.Format = "0.00";
                        ws.Cell(footerRow, col).Style.Font.Bold = true;
                        ws.Cell(footerRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(footerRow, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                        col++;
                    }

                    ws.Cell(footerRow, tbmCol).Value = totalCurriculumCredits;
                    ws.Cell(footerRow, tbmCol).Style.NumberFormat.Format = "0.00";
                    ws.Cell(footerRow, tbmCol).Style.Font.Bold = true;
                    ws.Cell(footerRow, tbmCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(footerRow, tbmCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

                    ws.Range(hRow1, 1, footerRow, lastCol).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    ws.Range(hRow1, 1, footerRow, lastCol).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    ws.Columns().AdjustToContents();

                    wb.SaveAs(filePath);
                    return (true, $"Đã xuất báo cáo bảng điểm chuẩn TBM thành công ({summaries.Count} học viên, các đợt thi chưa có điểm để trống, các dòng thiếu môn đã tô màu vàng).");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi xuất file Excel: {ex.Message}");
                }
            });
        }

        public async Task<(bool Success, string Message, int ImportedCadets, int ImportedScores)> ImportStandardTbmExcelAsync(string filePath)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (!File.Exists(filePath))
                        return (false, "File không tồn tại trên hệ thống.", 0, 0);

                    using var wb = new XLWorkbook(filePath);
                    var ws = wb.Worksheets.FirstOrDefault();
                    if (ws == null)
                        return (false, "File Excel không chứa bất kỳ sheet nào.", 0, 0);

                    var subjectsInDb = await _context.CreditSubjects.ToListAsync();
                    var cadetsInDb = await _context.Cadets.ToListAsync();

                    int importedCadetsCount = 0;
                    int importedScoresCount = 0;

                    // 0. Dọn dẹp các môn học mẫu ban đầu nếu có (để dữ liệu đồng bộ chuẩn 100% với 57 môn của Excel)
                    var dummySubjs = subjectsInDb.Where(s => s.SubjectCode.StartsWith("TOAN01") || 
                                                             s.SubjectCode.StartsWith("TRIET01") || 
                                                             s.SubjectCode.StartsWith("ANH01") || 
                                                             s.SubjectCode.StartsWith("CHIEN01") || 
                                                             s.SubjectCode.StartsWith("PHAP01")).ToList();
                    if (dummySubjs.Any())
                    {
                        var dummyIds = dummySubjs.Select(s => s.Id).ToList();
                        var dummyScores = await _context.CreditScoreRecords.Where(s => dummyIds.Contains(s.CreditSubjectId)).ToListAsync();
                        _context.CreditScoreRecords.RemoveRange(dummyScores);
                        _context.CreditSubjects.RemoveRange(dummySubjs);
                        await _context.SaveChangesAsync();
                        subjectsInDb = await _context.CreditSubjects.ToListAsync();
                    }

                    // 1. Đọc danh mục môn học và số tín chỉ (Cột 6 đến khi gặp TBM, thường là cột 62)
                    var colSubjectMap = new Dictionary<int, CreditSubject>();

                    for (int c = 6; c <= 100; c++)
                    {
                        string rawName = ws.Cell(5, c).GetString();
                        string subjName = System.Text.RegularExpressions.Regex.Replace(rawName, @"\s+", " ").Trim();

                        if (string.IsNullOrWhiteSpace(subjName))
                        {
                            // Nếu ô TBM thì dừng lại
                            if (ws.Cell(4, c).GetString().Trim().Contains("TBM") || 
                                ws.Cell(5, c).GetString().Trim().Contains("TBM"))
                                break;
                            
                            // Kiểm tra nếu 2 cột liên tiếp trống thì dừng
                            if (string.IsNullOrWhiteSpace(ws.Cell(5, c + 1).GetString().Trim()))
                                break;

                            continue;
                        }

                        if (subjName.Equals("TBM", StringComparison.OrdinalIgnoreCase))
                            break;

                        // Xử lý các cột bị trùng tên trong file Excel thực tế
                        if (c == 20) subjName = "ĐHQS 1";
                        else if (c == 60) subjName = "ĐHQS 2";
                        else if (c == 27) subjName = "KT Xe 1";
                        else if (c == 42) subjName = "KT Xe 2";
                        else if (c == 44) subjName = "Võ 1";
                        else if (c == 57) subjName = "Võ 2";

                        // Đọc số tín chỉ ở dòng 1
                        double credits = 1.0;
                        string creditStr = ws.Cell(1, c).GetString().Trim().Replace(',', '.');
                        if (double.TryParse(creditStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedCredits) && parsedCredits > 0)
                        {
                            credits = parsedCredits;
                        }

                        string subjCode = $"TC{c - 5:D2}";

                        // Tự động suy luận nhóm môn (SubjectGroup) cho các môn thành phần
                        string group = string.Empty;
                        bool isComponent = false;

                        if (subjName.StartsWith("CNTT", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "CNTT";
                            isComponent = true;
                        }
                        else if (subjName.Contains("ĐLQLBĐ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "ĐLQLBĐ";
                            isComponent = true;
                        }
                        else if (subjName.Contains("TH M-L", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "TH M-L";
                            isComponent = true;
                        }
                        else if (subjName.Contains("KTCT", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "KTCT";
                            isComponent = true;
                        }
                        else if (subjName.Contains("CNXH", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "CNXH";
                            isComponent = true;
                        }
                        else if (subjName.Contains("LSĐ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "LSĐ";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("bBB", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "bBB";
                            isComponent = true;
                        }
                        else if (subjName.Contains("VKHD", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "VKHD";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("KT Xe", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "KT Xe";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("ĐHQS", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "ĐHQS";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("Võ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "Võ";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("HC", StringComparison.OrdinalIgnoreCase) && subjName.Contains("QS", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "HCQS";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("TT HCM", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "TT HCM";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("LĐ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "Lựu đạn";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = subjName.Substring(4).Trim();
                            isComponent = true;
                        }

                        // Tìm hoặc tạo môn trong CSDL theo mã TCxx hoặc tên
                        var existingSubj = subjectsInDb.FirstOrDefault(s => s.SubjectCode == subjCode || s.SubjectName.Equals(subjName, StringComparison.OrdinalIgnoreCase));
                        if (existingSubj == null)
                        {
                            var newSubj = new CreditSubject
                            {
                                SubjectCode = subjCode,
                                SubjectName = subjName,
                                Credits = credits,
                                AssessmentType = subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase) ? "Kiểm tra và thi" : "Kiểm tra thường xuyên",
                                SubjectGroup = group,
                                IsComponent = isComponent,
                                Description = $"Nhập tự động từ file Excel ({credits} tín chỉ)",
                                CreatedAt = DateTime.Now
                            };

                            _context.CreditSubjects.Add(newSubj);
                            subjectsInDb.Add(newSubj);
                            colSubjectMap[c] = newSubj;
                        }
                        else
                        {
                            existingSubj.SubjectCode = subjCode;
                            existingSubj.SubjectName = subjName;
                            existingSubj.Credits = credits;
                            existingSubj.SubjectGroup = group;
                            existingSubj.IsComponent = isComponent;
                            colSubjectMap[c] = existingSubj;
                        }
                    }

                    await _context.SaveChangesAsync();

                    // 2. Đọc danh sách học viên và điểm từng môn (từ dòng 6 trở đi)
                    int lastRow = ws.LastRowUsed()?.RowNumber() ?? 70;
                    for (int r = 6; r <= lastRow; r++)
                    {
                        string fullName = ws.Cell(r, 5).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            string last = ws.Cell(r, 3).GetString().Trim();
                            string first = ws.Cell(r, 4).GetString().Trim();
                            fullName = $"{last} {first}".Trim();
                        }

                        fullName = System.Text.RegularExpressions.Regex.Replace(fullName, @"\s+", " ").Trim();
                        if (string.IsNullOrWhiteSpace(fullName)) continue;

                        string unit = ws.Cell(r, 2).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(unit)) unit = "b1";

                        // Tìm hoặc tạo học viên
                        var cadet = cadetsInDb.FirstOrDefault(cd => System.Text.RegularExpressions.Regex.Replace(cd.FullName, @"\s+", " ").Equals(fullName, StringComparison.OrdinalIgnoreCase));
                        if (cadet == null)
                        {
                            cadet = new Cadet
                            {
                                CadetCode = $"HV{DateTime.Now:yy}{cadetsInDb.Count + 1:D3}",
                                FullName = fullName,
                                Unit = unit,
                                Rank = "Binh nhì",
                                Position = "Học viên",
                                DateOfBirth = new DateTime(2002, 1, 1),
                                CreatedAt = DateTime.Now
                            };

                            _context.Cadets.Add(cadet);
                            cadetsInDb.Add(cadet);
                            importedCadetsCount++;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(unit) && cadet.Unit != unit)
                            {
                                cadet.Unit = unit;
                            }
                        }

                        // Đọc điểm cho từng môn
                        foreach (var (c, subj) in colSubjectMap)
                        {
                            string scoreStr = ws.Cell(r, c).GetString().Trim().Replace(',', '.');
                            if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double scoreVal))
                            {
                                if (scoreVal >= 0 && scoreVal <= 10)
                                {
                                    var existingScore = await _context.CreditScoreRecords
                                        .FirstOrDefaultAsync(s => s.CadetId == cadet.Id && s.CreditSubjectId == subj.Id);

                                    if (existingScore != null)
                                    {
                                        existingScore.FinalScore = scoreVal;
                                        existingScore.ExamDate = DateTime.Today;
                                    }
                                    else
                                    {
                                        _context.CreditScoreRecords.Add(new CreditScoreRecord
                                        {
                                            CadetId = cadet.Id,
                                            CreditSubjectId = subj.Id,
                                            FinalScore = scoreVal,
                                            RegularScore = scoreVal,
                                            ExamSession = "Toàn khóa",
                                            ExamDate = DateTime.Today,
                                            Notes = "Nhập tự động từ file chuẩn TBM",
                                            CreatedAt = DateTime.Now
                                        });
                                    }

                                    importedScoresCount++;
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    return (true, $"Đã nhập thành công từ file Excel: {colSubjectMap.Count} môn/thành phần, {importedCadetsCount} học viên mới, cập nhật {importedScoresCount} đầu điểm!", importedCadetsCount, importedScoresCount);
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi nhập file Excel: {ex.Message}", 0, 0);
                }
            });
        }

        public async Task<(bool Success, string Message, int Cadets, int Subjects, int Scores)> ResetAndImportFreshFromExcelAsync(string filePath)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (!File.Exists(filePath))
                        return (false, "File Excel không tồn tại trên hệ thống.", 0, 0, 0);

                    // 1. Xóa sạch toàn bộ dữ liệu học viên, môn học, thành phần và điểm số
                    var allScores = await _context.CreditScoreRecords.ToListAsync();
                    _context.CreditScoreRecords.RemoveRange(allScores);

                    var allPhysical = await _context.PhysicalExamRecords.ToListAsync();
                    _context.PhysicalExamRecords.RemoveRange(allPhysical);

                    var allComps = await _context.SubjectAssessmentComponents.ToListAsync();
                    _context.SubjectAssessmentComponents.RemoveRange(allComps);

                    var allSubjs = await _context.CreditSubjects.ToListAsync();
                    _context.CreditSubjects.RemoveRange(allSubjs);

                    var allCadets = await _context.Cadets.ToListAsync();
                    _context.Cadets.RemoveRange(allCadets);

                    await _context.SaveChangesAsync();

                    // 2. Nạp mới toàn bộ từ file Excel
                    using var wb = new XLWorkbook(filePath);
                    var ws = wb.Worksheets.FirstOrDefault();
                    if (ws == null)
                        return (false, "File Excel không chứa bất kỳ sheet nào.", 0, 0, 0);

                    var colSubjectMap = new Dictionary<int, CreditSubject>();

                    for (int c = 6; c <= 100; c++)
                    {
                        string rawName = ws.Cell(5, c).GetString();
                        string subjName = System.Text.RegularExpressions.Regex.Replace(rawName, @"\s+", " ").Trim();

                        if (string.IsNullOrWhiteSpace(subjName))
                        {
                            if (ws.Cell(4, c).GetString().Trim().Contains("TBM") || 
                                ws.Cell(5, c).GetString().Trim().Contains("TBM"))
                                break;
                            
                            if (string.IsNullOrWhiteSpace(ws.Cell(5, c + 1).GetString().Trim()))
                                break;

                            continue;
                        }

                        if (subjName.Equals("TBM", StringComparison.OrdinalIgnoreCase))
                            break;

                        if (c == 20) subjName = "ĐHQS 1";
                        else if (c == 60) subjName = "ĐHQS 2";
                        else if (c == 27) subjName = "KT Xe 1";
                        else if (c == 42) subjName = "KT Xe 2";
                        else if (c == 44) subjName = "Võ 1";
                        else if (c == 57) subjName = "Võ 2";

                        double credits = 1.0;
                        string creditStr = ws.Cell(1, c).GetString().Trim().Replace(',', '.');
                        if (double.TryParse(creditStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedCredits) && parsedCredits > 0)
                        {
                            credits = parsedCredits;
                        }

                        string subjCode = $"TC{c - 5:D2}";
                        string group = string.Empty;
                        bool isComponent = false;

                        if (subjName.StartsWith("CNTT", StringComparison.OrdinalIgnoreCase)) { group = "CNTT"; isComponent = true; }
                        else if (subjName.Contains("ĐLQLBĐ", StringComparison.OrdinalIgnoreCase)) { group = "ĐLQLBĐ"; isComponent = true; }
                        else if (subjName.Contains("TH M-L", StringComparison.OrdinalIgnoreCase)) { group = "TH M-L"; isComponent = true; }
                        else if (subjName.Contains("KTCT", StringComparison.OrdinalIgnoreCase)) { group = "KTCT"; isComponent = true; }
                        else if (subjName.Contains("CNXH", StringComparison.OrdinalIgnoreCase)) { group = "CNXH"; isComponent = true; }
                        else if (subjName.Contains("LSĐ", StringComparison.OrdinalIgnoreCase)) { group = "LSĐ"; isComponent = true; }
                        else if (subjName.StartsWith("bBB", StringComparison.OrdinalIgnoreCase)) { group = "bBB"; isComponent = true; }
                        else if (subjName.Contains("VKHD", StringComparison.OrdinalIgnoreCase)) { group = "VKHD"; isComponent = true; }
                        else if (subjName.StartsWith("KT Xe", StringComparison.OrdinalIgnoreCase)) { group = "KT Xe"; isComponent = true; }
                        else if (subjName.StartsWith("ĐHQS", StringComparison.OrdinalIgnoreCase)) { group = "ĐHQS"; isComponent = true; }
                        else if (subjName.StartsWith("Võ", StringComparison.OrdinalIgnoreCase)) { group = "Võ"; isComponent = true; }
                        else if (subjName.StartsWith("HC", StringComparison.OrdinalIgnoreCase) && subjName.Contains("QS", StringComparison.OrdinalIgnoreCase)) { group = "HCQS"; isComponent = true; }
                        else if (subjName.StartsWith("TT HCM", StringComparison.OrdinalIgnoreCase)) { group = "TT HCM"; isComponent = true; }
                        else if (subjName.StartsWith("LĐ", StringComparison.OrdinalIgnoreCase)) { group = "Lựu đạn"; isComponent = true; }
                        else if (subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase)) { group = subjName.Substring(4).Trim(); isComponent = true; }

                        var newSubj = new CreditSubject
                        {
                            SubjectCode = subjCode,
                            SubjectName = subjName,
                            Credits = credits,
                            AssessmentType = subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase) ? "Kiểm tra và thi" : "Kiểm tra thường xuyên",
                            SubjectGroup = group,
                            IsComponent = isComponent,
                            Description = $"Nhập tự động từ file Excel ({credits} tín chỉ)",
                            CreatedAt = DateTime.Now
                        };

                        _context.CreditSubjects.Add(newSubj);
                        colSubjectMap[c] = newSubj;
                    }

                    await _context.SaveChangesAsync();

                    // Đọc danh sách học viên
                    int lastRow = ws.LastRowUsed()?.RowNumber() ?? 70;
                    var addedCadets = new List<Cadet>();
                    int importedCadetsCount = 0;
                    int importedScoresCount = 0;

                    for (int r = 6; r <= lastRow; r++)
                    {
                        string fullName = ws.Cell(r, 5).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            string last = ws.Cell(r, 3).GetString().Trim();
                            string first = ws.Cell(r, 4).GetString().Trim();
                            fullName = $"{last} {first}".Trim();
                        }

                        fullName = System.Text.RegularExpressions.Regex.Replace(fullName, @"\s+", " ").Trim();
                        if (string.IsNullOrWhiteSpace(fullName)) continue;

                        string unit = ws.Cell(r, 2).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(unit)) unit = "b1";

                        var cadet = new Cadet
                        {
                            CadetCode = $"HV{DateTime.Now:yy}{addedCadets.Count + 1:D3}",
                            FullName = fullName,
                            Unit = unit,
                            Rank = "Binh nhì",
                            Position = "Học viên",
                            DateOfBirth = new DateTime(2002, 1, 1),
                            CreatedAt = DateTime.Now
                        };

                        _context.Cadets.Add(cadet);
                        addedCadets.Add(cadet);
                        importedCadetsCount++;
                    }

                    await _context.SaveChangesAsync();

                    // Đọc điểm
                    int cadetIdx = 0;
                    for (int r = 6; r <= lastRow; r++)
                    {
                        if (cadetIdx >= addedCadets.Count) break;
                        string fullName = ws.Cell(r, 5).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            string last = ws.Cell(r, 3).GetString().Trim();
                            string first = ws.Cell(r, 4).GetString().Trim();
                            fullName = $"{last} {first}".Trim();
                        }
                        if (string.IsNullOrWhiteSpace(fullName)) continue;

                        var cadet = addedCadets[cadetIdx++];

                        foreach (var (c, subj) in colSubjectMap)
                        {
                            string scoreStr = ws.Cell(r, c).GetString().Trim().Replace(',', '.');
                            if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double scoreVal))
                            {
                                if (scoreVal >= 0 && scoreVal <= 10)
                                {
                                    _context.CreditScoreRecords.Add(new CreditScoreRecord
                                    {
                                        CadetId = cadet.Id,
                                        CreditSubjectId = subj.Id,
                                        FinalScore = scoreVal,
                                        RegularScore = scoreVal,
                                        ExamSession = "Toàn khóa",
                                        ExamDate = DateTime.Today,
                                        Notes = "Nhập tự động từ file chuẩn TBM",
                                        CreatedAt = DateTime.Now
                                    });
                                    importedScoresCount++;
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    // 3. Tái cấu trúc chuẩn thành 41 môn lớn và các đợt kiểm tra / thi trực thuộc
                    await ConsolidateMajorSubjectsAsync();

                    // 4. Đảm bảo toàn bộ CreditScoreRecords đều có ComponentId
                    var orphanScores = await _context.CreditScoreRecords
                        .Where(s => s.ComponentId == null)
                        .ToListAsync();

                    if (orphanScores.Any())
                    {
                        var allComponents = await _context.SubjectAssessmentComponents.ToListAsync();
                        foreach (var sc in orphanScores)
                        {
                            var matchedComp = allComponents.FirstOrDefault(comp => comp.CreditSubjectId == sc.CreditSubjectId);
                            if (matchedComp != null)
                            {
                                sc.ComponentId = matchedComp.Id;
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    int totalMajorSubjects = await _context.CreditSubjects.CountAsync(s => !s.IsComponent);
                    return (true, $"Làm sạch và nạp lại CSDL thành công: {importedCadetsCount} học viên, {totalMajorSubjects} môn lớn, {importedScoresCount} điểm số!", importedCadetsCount, totalMajorSubjects, importedScoresCount);
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi làm sạch và nạp lại: {ex.Message}", 0, 0, 0);
                }
            });
        }

        public async Task<(bool Success, string Message)> SaveSubjectWithComponentsAsync(
            CreditSubject subject, IEnumerable<SubjectAssessmentComponent> components)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subject.SubjectCode))
                    return (false, "Mã môn học không được để trống.");
                if (string.IsNullOrWhiteSpace(subject.SubjectName))
                    return (false, "Tên môn học không được để trống.");

                var compList = components.ToList();
                if (!compList.Any())
                {
                    compList.Add(new SubjectAssessmentComponent
                    {
                        ComponentName = subject.SubjectName.Trim(),
                        Credits = subject.Credits > 0 ? subject.Credits : 1.0,
                        OrderIndex = 1,
                        CreatedAt = DateTime.Now
                    });
                }

                subject.SubjectCode = subject.SubjectCode.Trim().ToUpper();
                subject.SubjectName = subject.SubjectName.Trim();
                subject.Credits = Math.Round(compList.Sum(c => c.Credits), 2);

                if (subject.Id == 0)
                {
                    bool exists = await _context.CreditSubjects.AnyAsync(s => s.SubjectCode == subject.SubjectCode);
                    if (exists)
                        return (false, $"Mã môn học '{subject.SubjectCode}' đã tồn tại trong hệ thống.");

                    subject.CreatedAt = DateTime.Now;
                    _context.CreditSubjects.Add(subject);
                    await _context.SaveChangesAsync();

                    int order = 1;
                    foreach (var c in compList)
                    {
                        c.CreditSubjectId = subject.Id;
                        c.OrderIndex = order++;
                        c.CreatedAt = DateTime.Now;
                        _context.SubjectAssessmentComponents.Add(c);
                    }
                    await _context.SaveChangesAsync();
                    return (true, $"Thêm mới môn học '{subject.SubjectName}' cùng {compList.Count} đợt kiểm tra thành công.");
                }
                else
                {
                    var existing = await _context.CreditSubjects
                        .Include(s => s.Components)
                        .FirstOrDefaultAsync(s => s.Id == subject.Id);

                    if (existing == null)
                        return (false, "Không tìm thấy môn học cần sửa.");

                    bool codeConflict = await _context.CreditSubjects
                        .AnyAsync(s => s.SubjectCode == subject.SubjectCode && s.Id != subject.Id);
                    if (codeConflict)
                        return (false, $"Mã môn học '{subject.SubjectCode}' đã được sử dụng.");

                    existing.SubjectCode = subject.SubjectCode;
                    existing.SubjectName = subject.SubjectName;
                    existing.AssessmentType = subject.AssessmentType;
                    existing.Description = subject.Description;
                    existing.Credits = Math.Round(compList.Sum(c => c.Credits), 2);

                    // Đồng bộ các đợt kiểm tra
                    var newCompNames = compList.Select(c => c.ComponentName.Trim().ToLower()).ToHashSet();
                    var toRemove = existing.Components.Where(c => !newCompNames.Contains(c.ComponentName.Trim().ToLower())).ToList();
                    _context.SubjectAssessmentComponents.RemoveRange(toRemove);

                    int order = 1;
                    foreach (var c in compList)
                    {
                        var match = existing.Components.FirstOrDefault(x => x.ComponentName.Trim().Equals(c.ComponentName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            match.Credits = c.Credits;
                            match.OrderIndex = order++;
                        }
                        else
                        {
                            existing.Components.Add(new SubjectAssessmentComponent
                            {
                                CreditSubjectId = existing.Id,
                                ComponentName = c.ComponentName.Trim(),
                                Credits = c.Credits,
                                OrderIndex = order++,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return (true, $"Cập nhật môn học '{existing.SubjectName}' và các đợt kiểm tra thành công.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi lưu môn học: {ex.Message}");
            }
        }

        public async Task<List<SubjectAssessmentComponent>> GetComponentsBySubjectIdAsync(int subjectId)
        {
            return await _context.SubjectAssessmentComponents
                .Where(c => c.CreditSubjectId == subjectId)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();
        }

        public async Task<(CreditSubject? Subject, List<SubjectAssessmentComponent> Components, List<CadetSubjectGradeRowDto> Rows)> GetSubjectGradeMatrixAsync(
            int subjectId, string? unit = null, string? className = null)
        {
            var subject = await _context.CreditSubjects
                .Include(s => s.Components)
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null)
                return (null, new List<SubjectAssessmentComponent>(), new List<CadetSubjectGradeRowDto>());

            var components = subject.Components.OrderBy(c => c.OrderIndex).ToList();
            if (!components.Any())
            {
                var def = new SubjectAssessmentComponent
                {
                    CreditSubjectId = subject.Id,
                    ComponentName = subject.SubjectName,
                    Credits = subject.Credits > 0 ? subject.Credits : 1.0,
                    OrderIndex = 1,
                    CreatedAt = DateTime.Now
                };
                _context.SubjectAssessmentComponents.Add(def);
                await _context.SaveChangesAsync();
                components.Add(def);
            }

            var cadetQuery = _context.Cadets
                .Include(c => c.MilitaryClass)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
                cadetQuery = cadetQuery.Where(c => c.Unit == unit);

            if (!string.IsNullOrWhiteSpace(className) && className != "Tất cả")
                cadetQuery = cadetQuery.Where(c => c.MilitaryClass != null && c.MilitaryClass.ClassName == className);

            var cadets = await cadetQuery.OrderBy(c => c.Unit).ThenBy(c => c.FullName).ToListAsync();
            var compIds = components.Select(c => c.Id).ToList();

            var scores = await _context.CreditScoreRecords
                .Where(s => s.CreditSubjectId == subjectId || (s.ComponentId.HasValue && compIds.Contains(s.ComponentId.Value)))
                .AsNoTracking()
                .ToListAsync();

            var scoreMap = new Dictionary<(int CadetId, int ComponentId), double>();
            foreach (var sc in scores)
            {
                if (sc.ComponentId.HasValue)
                {
                    scoreMap[(sc.CadetId, sc.ComponentId.Value)] = sc.FinalScore;
                }
                else if (components.Count == 1)
                {
                    scoreMap[(sc.CadetId, components[0].Id)] = sc.FinalScore;
                }
            }

            // Đếm số lượng học viên có điểm cho từng đợt kiểm tra
            var activeComponentIds = new HashSet<int>();
            foreach (var comp in components)
            {
                int countWithScore = cadets.Count(c => scoreMap.ContainsKey((c.Id, comp.Id)));
                if (countWithScore >= 20)
                {
                    activeComponentIds.Add(comp.Id);
                }
            }

            var rows = new List<CadetSubjectGradeRowDto>();

            foreach (var cadet in cadets)
            {
                var row = new CadetSubjectGradeRowDto
                {
                    CadetId = cadet.Id,
                    CadetCode = cadet.CadetCode,
                    FullName = cadet.FullName,
                    Unit = cadet.Unit,
                    ClassName = cadet.MilitaryClass?.ClassName ?? cadet.Unit
                };

                for (int i = 0; i < components.Count; i++)
                {
                    var comp = components[i];
                    double? scoreVal = scoreMap.TryGetValue((cadet.Id, comp.Id), out var sc) ? sc : null;
                    row.ComponentScores[comp.Id] = scoreVal;

                    if (i == 0) row.Score1 = scoreVal;
                    else if (i == 1) row.Score2 = scoreVal;
                    else if (i == 2) row.Score3 = scoreVal;
                    else if (i == 3) row.Score4 = scoreVal;
                    else if (i == 4) row.Score5 = scoreVal;
                    else if (i == 5) row.Score6 = scoreVal;
                }

                var compScorePairs = components.Select(c => (
                    score: row.ComponentScores.TryGetValue(c.Id, out var s) ? s : null,
                    credits: c.Credits
                ));
                row.CalculatedSubjectScore = _calculator.CalculateSubjectScoreFromComponents(compScorePairs, subject.Credits);

                var (hasMissingInActive, missingNames) = _calculator.CheckMissingInActiveComponents(
                    components, row.ComponentScores, activeComponentIds);

                row.HasMissingInActiveComponent = hasMissingInActive;
                if (hasMissingInActive)
                {
                    row.MissingComponentsDisplay = $"Chưa thi: {string.Join(", ", missingNames)}";
                }

                row.OnScoreUpdatedCallback = (r, colIdx, newScore) =>
                {
                    if (colIdx >= 1 && colIdx <= components.Count)
                    {
                        var comp = components[colIdx - 1];
                        r.ComponentScores[comp.Id] = newScore;
                        var pairs = components.Select(c => (
                            score: r.ComponentScores.TryGetValue(c.Id, out var s) ? s : null,
                            credits: c.Credits
                        ));
                        r.CalculatedSubjectScore = _calculator.CalculateSubjectScoreFromComponents(pairs, subject.Credits);

                        var (missing, mNames) = _calculator.CheckMissingInActiveComponents(
                            components, r.ComponentScores, activeComponentIds);
                        r.HasMissingInActiveComponent = missing;
                        r.MissingComponentsDisplay = missing ? $"Chưa thi: {string.Join(", ", mNames)}" : string.Empty;
                    }
                };

                rows.Add(row);
            }

            return (subject, components, rows);
        }

        public async Task<(bool Success, string Message)> SaveSubjectGradeMatrixAsync(
            int subjectId, List<CadetSubjectGradeRowDto> rows)
        {
            try
            {
                var subject = await _context.CreditSubjects
                    .Include(s => s.Components)
                    .FirstOrDefaultAsync(s => s.Id == subjectId);

                if (subject == null)
                    return (false, "Không tìm thấy môn học.");

                var components = subject.Components.OrderBy(c => c.OrderIndex).ToList();
                int savedScoresCount = 0;

                foreach (var row in rows)
                {
                    for (int i = 0; i < components.Count; i++)
                    {
                        var comp = components[i];
                        double? scoreVal = null;
                        if (i == 0) scoreVal = row.Score1;
                        else if (i == 1) scoreVal = row.Score2;
                        else if (i == 2) scoreVal = row.Score3;
                        else if (i == 3) scoreVal = row.Score4;
                        else if (i == 4) scoreVal = row.Score5;
                        else if (i == 5) scoreVal = row.Score6;

                        if (!scoreVal.HasValue && row.ComponentScores.TryGetValue(comp.Id, out var dictVal))
                        {
                            scoreVal = dictVal;
                        }

                        var existingScore = await _context.CreditScoreRecords
                            .FirstOrDefaultAsync(s => s.CadetId == row.CadetId && 
                                                      (s.ComponentId == comp.Id || 
                                                       (s.CreditSubjectId == subjectId && s.ComponentId == null && components.Count == 1)));

                        if (scoreVal.HasValue && scoreVal.Value >= 0 && scoreVal.Value <= 10)
                        {
                            if (existingScore != null)
                            {
                                existingScore.FinalScore = scoreVal.Value;
                                existingScore.RegularScore = scoreVal.Value;
                                existingScore.ComponentId = comp.Id;
                                existingScore.CreditSubjectId = subjectId;
                                existingScore.ExamDate = DateTime.Today;
                            }
                            else
                            {
                                _context.CreditScoreRecords.Add(new CreditScoreRecord
                                {
                                    CadetId = row.CadetId,
                                    CreditSubjectId = subjectId,
                                    ComponentId = comp.Id,
                                    FinalScore = scoreVal.Value,
                                    RegularScore = scoreVal.Value,
                                    ExamDate = DateTime.Today,
                                    CreatedAt = DateTime.Now,
                                    Notes = "Nhập từ bảng điểm môn học"
                                });
                            }
                            savedScoresCount++;
                        }
                        else
                        {
                            if (existingScore != null && (!scoreVal.HasValue || scoreVal.Value < 0))
                            {
                                _context.CreditScoreRecords.Remove(existingScore);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return (true, $"Đã lưu thành công {savedScoresCount} đầu điểm cho môn '{subject.SubjectName}'!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi lưu bảng điểm: {ex.Message}");
            }
        }

        public async Task EnsureComponentsMigratedAsync()
        {
            try
            {
                var subjects = await _context.CreditSubjects
                    .Include(s => s.Components)
                    .ToListAsync();

                bool changed = false;
                foreach (var subj in subjects)
                {
                    if (!subj.Components.Any())
                    {
                        var comp = new SubjectAssessmentComponent
                        {
                            CreditSubjectId = subj.Id,
                            ComponentName = subj.SubjectName,
                            Credits = subj.Credits > 0 ? subj.Credits : 1.0,
                            OrderIndex = 1,
                            CreatedAt = DateTime.Now
                        };
                        _context.SubjectAssessmentComponents.Add(comp);
                        changed = true;
                    }
                }

                if (changed)
                {
                    await _context.SaveChangesAsync();

                    var allComps = await _context.SubjectAssessmentComponents.ToListAsync();
                    var compBySubj = allComps.ToDictionary(c => c.CreditSubjectId);

                    var unlinkedScores = await _context.CreditScoreRecords
                        .Where(s => s.ComponentId == null)
                        .ToListAsync();

                    foreach (var sc in unlinkedScores)
                    {
                        if (compBySubj.TryGetValue(sc.CreditSubjectId, out var comp))
                        {
                            sc.ComponentId = comp.Id;
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                await ConsolidateMajorSubjectsAsync();
            }
            catch { }
        }

        public async Task ConsolidateMajorSubjectsAsync()
        {
            try
            {
                var groups = await _context.CreditSubjects
                    .Where(s => !string.IsNullOrWhiteSpace(s.SubjectGroup))
                    .Select(s => s.SubjectGroup!)
                    .Distinct()
                    .ToListAsync();

                bool changed = false;

                foreach (var group in groups)
                {
                    var subjs = await _context.CreditSubjects
                        .Where(s => s.SubjectGroup == group)
                        .OrderBy(s => s.Id)
                        .ToListAsync();

                    if (subjs.Count <= 1) continue;

                    var primary = subjs.FirstOrDefault(s => s.SubjectName.Equals(group, StringComparison.OrdinalIgnoreCase)) ?? subjs[0];

                    if (!primary.SubjectName.Equals(group, StringComparison.OrdinalIgnoreCase) && 
                        !subjs.Any(s => s.SubjectName.Equals(group, StringComparison.OrdinalIgnoreCase)))
                    {
                        primary.SubjectName = group;
                    }

                    primary.IsComponent = false;
                    primary.Credits = Math.Round(subjs.Sum(s => s.Credits), 2);

                    // Xóa các components cũ của môn chính nếu cần tái cấu trúc
                    var existingComps = await _context.SubjectAssessmentComponents
                        .Where(c => c.CreditSubjectId == primary.Id)
                        .ToListAsync();

                    _context.SubjectAssessmentComponents.RemoveRange(existingComps);
                    await _context.SaveChangesAsync();

                    var compMap = new Dictionary<int, int>(); // Old subject Id -> new component Id
                    for (int i = 0; i < subjs.Count; i++)
                    {
                        var s = subjs[i];
                        var newComp = new SubjectAssessmentComponent
                        {
                            CreditSubjectId = primary.Id,
                            ComponentName = s.SubjectName,
                            Credits = s.Credits,
                            OrderIndex = i + 1,
                            CreatedAt = DateTime.Now
                        };
                        _context.SubjectAssessmentComponents.Add(newComp);
                        await _context.SaveChangesAsync();
                        compMap[s.Id] = newComp.Id;
                    }

                    // Liên kết lại điểm số sang môn chính và ComponentId tương ứng
                    foreach (var s in subjs)
                    {
                        if (compMap.TryGetValue(s.Id, out int newCompId))
                        {
                            var scores = await _context.CreditScoreRecords
                                .Where(sc => sc.CreditSubjectId == s.Id)
                                .ToListAsync();

                            foreach (var sc in scores)
                            {
                                sc.CreditSubjectId = primary.Id;
                                sc.ComponentId = newCompId;
                            }
                        }

                        if (s.Id != primary.Id)
                        {
                            s.IsComponent = true;
                        }
                    }

                    changed = true;
                }

                // Đối với các môn độc lập (IsComponent == false), đảm bảo có ít nhất 1 component
                var majorSubjs = await _context.CreditSubjects
                    .Include(s => s.Components)
                    .Where(s => !s.IsComponent)
                    .ToListAsync();

                foreach (var ms in majorSubjs)
                {
                    if (!ms.Components.Any())
                    {
                        var comp = new SubjectAssessmentComponent
                        {
                            CreditSubjectId = ms.Id,
                            ComponentName = ms.SubjectName,
                            Credits = ms.Credits > 0 ? ms.Credits : 1.0,
                            OrderIndex = 1,
                            CreatedAt = DateTime.Now
                        };
                        _context.SubjectAssessmentComponents.Add(comp);
                        changed = true;
                    }
                }

                if (changed)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch { }
        }

        public async Task<(CreditSubject? Subject, List<CadetSingleSubjectGradeDto> Components, double? CalculatedSubjectScore, bool HasMissingWarning)> GetCadetSubjectGradesAsync(int cadetId, int subjectId)
        {
            var subject = await _context.CreditSubjects
                .Include(s => s.Components)
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null)
                return (null, new List<CadetSingleSubjectGradeDto>(), null, false);

            var components = subject.Components.OrderBy(c => c.OrderIndex).ToList();
            var componentIds = components.Select(c => c.Id).ToList();

            // Lấy toàn bộ điểm của học viên này ở môn học
            var scores = await _context.CreditScoreRecords
                .Where(s => s.CadetId == cadetId && (s.CreditSubjectId == subjectId || (s.ComponentId.HasValue && componentIds.Contains(s.ComponentId.Value))))
                .ToListAsync();

            var scoreMap = new Dictionary<int, double?>();
            foreach (var sc in scores)
            {
                if (sc.ComponentId.HasValue)
                {
                    scoreMap[sc.ComponentId.Value] = sc.FinalScore;
                }
            }

            // Tìm các component active (>10 học viên khác đã có điểm)
            var activeComponentIds = new HashSet<int>();
            if (componentIds.Any())
            {
                var componentCounts = await _context.CreditScoreRecords
                    .Where(s => s.CreditSubjectId == subjectId && s.ComponentId.HasValue && componentIds.Contains(s.ComponentId.Value))
                    .GroupBy(s => s.ComponentId!.Value)
                    .Select(g => new { ComponentId = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var cc in componentCounts)
                {
                    if (cc.Count >= 20)
                        activeComponentIds.Add(cc.ComponentId);
                }
            }

            var dtoList = new List<CadetSingleSubjectGradeDto>();
            foreach (var comp in components)
            {
                scoreMap.TryGetValue(comp.Id, out double? sVal);
                bool isMissingInActive = activeComponentIds.Contains(comp.Id) && (!sVal.HasValue || sVal.Value < 0);

                dtoList.Add(new CadetSingleSubjectGradeDto
                {
                    ComponentId = comp.Id,
                    ComponentName = comp.ComponentName,
                    Credits = comp.Credits,
                    Score = sVal,
                    HasMissingWarning = isMissingInActive
                });
            }

            var pairs = components.Select(c => (
                score: scoreMap.TryGetValue(c.Id, out var s) ? s : null,
                credits: c.Credits
            ));
            var calcScore = _calculator.CalculateSubjectScoreFromComponents(pairs, subject.Credits);
            var (hasMissing, _) = _calculator.CheckMissingInActiveComponents(components, scoreMap, activeComponentIds);

            return (subject, dtoList, calcScore, hasMissing);
        }

        public async Task<(bool Success, string Message)> SaveCadetSubjectGradesAsync(
            int cadetId, int subjectId, List<(int componentId, double? score)> componentScores)
        {
            try
            {
                var subject = await _context.CreditSubjects
                    .Include(s => s.Components)
                    .FirstOrDefaultAsync(s => s.Id == subjectId);

                if (subject == null)
                    return (false, "Không tìm thấy môn học.");

                var cadet = await _context.Cadets.FindAsync(cadetId);
                if (cadet == null)
                    return (false, "Không tìm thấy học viên.");

                int savedCount = 0;
                foreach (var (compId, scoreVal) in componentScores)
                {
                    var existingScore = await _context.CreditScoreRecords
                        .FirstOrDefaultAsync(s => s.CadetId == cadetId && 
                                                  (s.ComponentId == compId || 
                                                   (s.CreditSubjectId == subjectId && s.ComponentId == null && subject.Components.Count == 1)));

                    if (scoreVal.HasValue && scoreVal.Value >= 0 && scoreVal.Value <= 10)
                    {
                        if (existingScore != null)
                        {
                            existingScore.FinalScore = scoreVal.Value;
                            existingScore.RegularScore = scoreVal.Value;
                            existingScore.ComponentId = compId;
                            existingScore.CreditSubjectId = subjectId;
                            existingScore.ExamDate = DateTime.Today;
                        }
                        else
                        {
                            _context.CreditScoreRecords.Add(new CreditScoreRecord
                            {
                                CadetId = cadetId,
                                CreditSubjectId = subjectId,
                                ComponentId = compId,
                                FinalScore = scoreVal.Value,
                                RegularScore = scoreVal.Value,
                                ExamDate = DateTime.Today,
                                CreatedAt = DateTime.Now,
                                Notes = "Nhập điểm từ hồ sơ học viên"
                            });
                        }
                        savedCount++;
                    }
                    else
                    {
                        if (existingScore != null && (!scoreVal.HasValue || scoreVal.Value < 0))
                        {
                            _context.CreditScoreRecords.Remove(existingScore);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return (true, $"Lưu điểm học viên {cadet.FullName} thành công ({savedCount} cột điểm).");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi lưu điểm học viên: {ex.Message}");
            }
        }
    }
}
