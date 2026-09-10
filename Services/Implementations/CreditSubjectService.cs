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
                    return (false, "MÃ£ mÃ´n há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
                if (string.IsNullOrWhiteSpace(subject.SubjectName))
                    return (false, "TÃªn mÃ´n há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
                if (subject.Credits <= 0)
                    return (false, "Sá»‘ tÃ­n chá»‰ pháº£i lá»›n hÆ¡n 0.");

                subject.SubjectCode = subject.SubjectCode.Trim().ToUpper();
                subject.SubjectName = subject.SubjectName.Trim();

                bool exists = await _context.CreditSubjects.AnyAsync(s => s.SubjectCode == subject.SubjectCode);
                if (exists)
                    return (false, $"MÃ£ mÃ´n há»c '{subject.SubjectCode}' Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.");

                _context.CreditSubjects.Add(subject);
                await _context.SaveChangesAsync();
                return (true, "ThÃªm mÃ´n há»c tÃ­n chá»‰ thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi thÃªm mÃ´n há»c: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateSubjectAsync(CreditSubject subject)
        {
            try
            {
                var existing = await _context.CreditSubjects.FindAsync(subject.Id);
                if (existing == null)
                    return (false, "KhÃ´ng tÃ¬m tháº¥y mÃ´n há»c cáº§n sá»­a.");

                subject.SubjectCode = subject.SubjectCode.Trim().ToUpper();
                subject.SubjectName = subject.SubjectName.Trim();

                bool codeConflict = await _context.CreditSubjects
                    .AnyAsync(s => s.SubjectCode == subject.SubjectCode && s.Id != subject.Id);
                if (codeConflict)
                    return (false, $"MÃ£ mÃ´n há»c '{subject.SubjectCode}' Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng bá»Ÿi mÃ´n khÃ¡c.");

                existing.SubjectCode = subject.SubjectCode;
                existing.SubjectName = subject.SubjectName;
                existing.Credits = subject.Credits;
                existing.AssessmentType = subject.AssessmentType;
                existing.Description = subject.Description;

                await _context.SaveChangesAsync();
                return (true, "Cáº­p nháº­t mÃ´n há»c tÃ­n chá»‰ thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi cáº­p nháº­t mÃ´n há»c: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteSubjectAsync(int id)
        {
            try
            {
                var subject = await _context.CreditSubjects.FindAsync(id);
                if (subject == null)
                    return (false, "KhÃ´ng tÃ¬m tháº¥y mÃ´n há»c.");

                _context.CreditSubjects.Remove(subject);
                await _context.SaveChangesAsync();
                return (true, "XÃ³a mÃ´n há»c tÃ­n chá»‰ thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xÃ³a mÃ´n há»c: {ex.Message}");
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
                    return (false, "Äiá»ƒm mÃ´n há»c pháº£i náº±m trong khoáº£ng tá»« 0.0 Ä‘áº¿n 10.0.");

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
                return (true, "LÆ°u Ä‘iá»ƒm mÃ´n há»c tÃ­n chá»‰ thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi lÆ°u Ä‘iá»ƒm: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteScoreAsync(int scoreId)
        {
            try
            {
                var record = await _context.CreditScoreRecords.FindAsync(scoreId);
                if (record == null)
                    return (false, "KhÃ´ng tÃ¬m tháº¥y báº£n ghi Ä‘iá»ƒm.");

                _context.CreditScoreRecords.Remove(record);
                await _context.SaveChangesAsync();
                return (true, "XÃ³a Ä‘iá»ƒm thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xÃ³a Ä‘iá»ƒm: {ex.Message}");
            }
        }

        public async Task<List<CadetAcademicSummaryDto>> GetCadetAcademicSummariesAsync(
            string? unit = null, string? className = null, string? keyword = null)
        {
            var query = _context.Cadets
                .Include(c => c.MilitaryClass)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(unit) && !unit.Contains("Tất cả") && !unit.Contains("Táº¥t cáº£") && !unit.Equals("All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.Unit == unit);

            if (!string.IsNullOrWhiteSpace(className) && !className.Contains("Tất cả") && !className.Contains("Táº¥t cáº£") && !className.Equals("All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.MilitaryClass != null && c.MilitaryClass.ClassName == className);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(keyword) || c.CadetCode.ToLower().Contains(keyword));
            }

            var cadets = await query.ToListAsync();
            var allScores = await _context.CreditScoreRecords
                .Include(s => s.CreditSubject)
                .AsNoTracking()
                .ToListAsync();

            // Láº¥y danh sÃ¡ch cÃ¡c mÃ´n lá»›n vÃ  cÃ¡c Ä‘á»£t thi thÃ nh pháº§n trá»±c thuá»™c
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

            // Äáº¿m sá»‘ lÆ°á»£ng há»c viÃªn cÃ³ Ä‘iá»ƒm cho tá»«ng Ä‘á»£t kiá»ƒm tra / thÃ nh pháº§n con
            // Quy táº¯c: Náº¿u cÃ³ >= 20 há»c viÃªn cÃ³ Ä‘iá»ƒm thÃ¬ Ä‘á»£t kiá»ƒm tra Ä‘Ã³ coi nhÆ° Ä‘Ã£ diá»…n ra
            var componentScoreCounts = allScores
                .Where(s => s.ComponentId.HasValue && s.FinalScore >= 0)
                .GroupBy(s => s.ComponentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(s => s.CadetId).Distinct().Count());

            var activeComponentIds = componentScoreCounts
                .Where(kvp => kvp.Value >= 20)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            // Tổng tín chỉ toàn khóa chuẩn theo file Excel chuẩn (62.90 TC)
            double curriculumCredits = allComponents.Sum(c => c.Credits);
            if (curriculumCredits <= 0 || curriculumCredits > 100) curriculumCredits = 62.90;

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

                // Ä iá»ƒm theo tá»«ng Ä‘á»£t kiá»ƒm tra cá»§a há» c viÃªn nÃ y
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

                // TÃ­nh Ä‘iá»ƒm mÃ´n lá»›n: Ä Điểm trung bình môn CHỈ CÓ KHI 100% CÁC CỘT CỦA MÔN CHÍNH ĐƯỢC NHẬP
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

                // Kiá»ƒm tra thiáº¿u Ä‘á»£t thi trong cÃ¡c Ä‘á»£t Ä Ãƒ DIá»„N RA (>= 20 há» c viÃªn cÃ³ Ä‘iá»ƒm)
                // Náº¿u Ä‘á»£t kiá»ƒm tra chÆ°a cÃ³ ai thi (< 20 há» c sinh) thÃ¬ coi nhÆ° chÆ°a diá»…n ra vÃ  KHÃ”NG bá»‹ Ä‘Ã¡nh vÃ ng
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

                // Tính GPA tích lũy toàn khóa trên các đợt thi đã hoàn thành:
                // Chuẩn Excel: TBM = SUM(điểm đợt kiểm tra có điểm * tín chỉ) / tổng tín chỉ của các đợt kiểm tra đó
                if (allComponents.Count > 0)
                {
                    var scoredComponents = allComponents
                        .Where(c => cadetCompScores.TryGetValue(c.Id, out var sc) && sc.HasValue && sc.Value >= 0)
                        .Select(c => (score: cadetCompScores[c.Id]!.Value, credits: c.Credits))
                        .ToList();

                    double earnedCredits = Math.Round(scoredComponents.Sum(sc => sc.credits), 2);
                    dto.TotalCreditsEarned = earnedCredits;

                    // Chia cho tổng tín chỉ các đợt kiểm tra có điểm (không chia tổng cả môn nếu chưa học đủ)
                    dto.Gpa = _calculator.CalculateCurriculumTbm(scoredComponents, earnedCredits);
                    dto.TotalSubjectsCompleted = majorSubjects.Count(s => dto.SubjectScores.TryGetValue(s.Id, out var sc) && sc.HasValue);
                }
                else
                {
                    // Fallback tính GPA trực tiếp theo môn học nếu chưa cấu hình đợt thi thành phần
                    var directScores = cadetScores
                        .Where(s => s.CreditSubject != null && s.FinalScore >= 0)
                        .ToList();

                    if (directScores.Count > 0)
                    {
                        double sumCredits = directScores.Sum(s => s.CreditSubject!.Credits);
                        double weightedScore = directScores.Sum(s => s.FinalScore * s.CreditSubject!.Credits);
                        dto.Gpa = sumCredits > 0 ? Math.Round(weightedScore / sumCredits, 2) : 0.0;
                        dto.TotalCreditsEarned = Math.Round(sumCredits, 1);
                        dto.TotalSubjectsCompleted = directScores.Count;
                    }
                }

                // XÃ¢y dá»±ng báº£ng phÃ¢n rÃ£ Ä‘iá»ƒm thÃ nh pháº§n
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

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Táº¥t cáº£")
                query = query.Where(c => c.Unit == unit);

            if (!string.IsNullOrWhiteSpace(className) && className != "Táº¥t cáº£")
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
                        missingList.Add("RÃ¨n luyá»‡n thá»ƒ lá»±c (chÆ°a cÃ³ Ä‘iá»ƒm)");

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
                            ? "MÃ´n TÃ­n chá»‰ & Thá»ƒ lá»±c" 
                            : (missingSubjects.Count > 0 ? "MÃ´n TÃ­n chá»‰" : "RÃ¨n luyá»‡n Thá»ƒ lá»±c"),
                        Status = "ChÆ°a hoÃ n thÃ nh",
                        Note = $"CÃ²n thiáº¿u {missingList.Count} ná»™i dung cáº§n tá»• chá»©c kiá»ƒm tra bÃ¹"
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
                        .OrderBy(c => c.CreditSubject!.SubjectCode)
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

                    // Dòng 1: Số tín chỉ của từng đợt kiểm tra / thi
                    int startCol = 7; // Cột 1: TT, Cột 2: Mã học viên, Cột 3: Đơn vị, Cột 4: Họ đệm, Cột 5: Tên, Cột 6: Họ và tên ghép
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

                    // Vị trí các cột tổng kết ở cuối bảng (Khớp chuẩn OOP và giao diện)
                    int creditCol = col;       // Cột Tổng Tín Chỉ
                    int tbmCol = creditCol + 1; // Cột TBM
                    int ratingCol = creditCol + 2; // Cột Xếp loại
                    int belowSevenCol = creditCol + 3; // Cột Số môn <7
                    int completedCol = creditCol + 4;  // Cột Số môn đã học
                    int lastCol = completedCol;

                    // Ô tổng tín chỉ toàn khóa tại dòng 1 của cột Tổng Tín Chỉ
                    ws.Cell(1, creditCol).Value = totalCurriculumCredits;
                    ws.Cell(1, creditCol).Style.NumberFormat.Format = "0.00";
                    ws.Cell(1, creditCol).Style.Font.Bold = true;
                    ws.Cell(1, creditCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Tiêu đề báo cáo
                    ws.Cell(3, 1).Value = "KẾT QUẢ HỌC TẬP TOÀN KHÓA VÀ ĐIỂM TRUNG BÌNH MÔN (TBM)";
                    ws.Range(3, 1, 3, lastCol).Merge();
                    ws.Cell(3, 1).Style.Font.Bold = true;
                    ws.Cell(3, 1).Style.Font.FontSize = 14;
                    ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
                    ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Dòng 4 & 5: Tiêu đề cột
                    int hRow1 = 4;
                    int hRow2 = 5;

                    ws.Cell(hRow1, 1).Value = "TT";
                    ws.Cell(hRow1, 2).Value = "Mã học viên";
                    ws.Cell(hRow1, 3).Value = "Đơn vị";
                    ws.Cell(hRow1, 4).Value = "Họ và tên đệm";
                    ws.Cell(hRow1, 5).Value = "Tên";
                    ws.Cell(hRow1, 6).Value = "Họ và tên ghép";

                    col = startCol;
                    foreach (var comp in components)
                    {
                        ws.Cell(hRow2, col).Value = comp.ComponentName;
                        col++;
                    }

                    // Giữ lại cột tín chỉ ở cuối bảng cùng cột TBM và xếp loại
                    ws.Cell(hRow2, creditCol).Value = "Tổng Tín Chỉ";
                    ws.Cell(hRow2, tbmCol).Value = "TBM";
                    ws.Cell(hRow1, ratingCol).Value = "Xếp loại học tập";
                    ws.Cell(hRow1, belowSevenCol).Value = "Số môn <7";
                    ws.Cell(hRow1, completedCol).Value = "Số môn đã học";

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
                        // Kiểm tra học viên thiếu môn: tô màu vàng
                        if (item.HasMissingSubjects)
                        {
                            ws.Range(row, 1, row, lastCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF00");
                        }

                        ws.Cell(row, 1).Value = stt++;
                        ws.Cell(row, 2).Value = item.CadetCode;
                        ws.Cell(row, 3).Value = item.Unit;

                        // Tách họ đệm và tên
                        string name = item.FullName.Trim();
                        int lastSpace = name.LastIndexOf(' ');
                        string lastName = lastSpace > 0 ? name.Substring(0, lastSpace).Trim() : string.Empty;
                        string firstName = lastSpace > 0 ? name.Substring(lastSpace + 1).Trim() : name;

                        ws.Cell(row, 4).Value = lastName;
                        ws.Cell(row, 5).Value = firstName;
                        ws.Cell(row, 6).Value = item.FullName;

                        col = startCol;
                        int belowSevenCount = 0;
                        int completedCount = 0;

                        foreach (var comp in components)
                        {
                            if (item.ComponentScores.TryGetValue(comp.Id, out var score) && score.HasValue && score.Value >= 0)
                            {
                                ws.Cell(row, col).Value = score.Value;
                                ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                completedCount++;
                                if (score.Value <= 6.99) belowSevenCount++;
                            }
                            else
                            {
                                ws.Cell(row, col).Value = string.Empty;
                            }
                            col++;
                        }

                        // Cột Tổng Tín Chỉ (ở cuối danh sách môn, cạnh TBM)
                        ws.Cell(row, creditCol).Value = item.TotalCreditsEarned;
                        ws.Cell(row, creditCol).Style.NumberFormat.Format = "0.00";
                        ws.Cell(row, creditCol).Style.Font.Bold = true;
                        ws.Cell(row, creditCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Cột TBM
                        ws.Cell(row, tbmCol).Value = item.Gpa;
                        ws.Cell(row, tbmCol).Style.NumberFormat.Format = "0.00";
                        ws.Cell(row, tbmCol).Style.Font.Bold = true;
                        ws.Cell(row, tbmCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Cột Xếp loại
                        ws.Cell(row, ratingCol).Value = item.AcademicRating;
                        ws.Cell(row, ratingCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, ratingCol).Style.Font.Bold = true;

                        // Cột Số môn < 7
                        ws.Cell(row, belowSevenCol).Value = belowSevenCount;
                        ws.Cell(row, belowSevenCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Cột Số môn đã học
                        ws.Cell(row, completedCol).Value = completedCount;
                        ws.Cell(row, completedCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        row++;
                    }

                    ws.Range(hRow1, 1, row - 1, lastCol).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    ws.Range(hRow1, 1, row - 1, lastCol).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    ws.Columns().AdjustToContents();

                    wb.SaveAs(filePath);
                    return (true, $"Đã xuất báo cáo bảng điểm chuẩn TBM thành công ({summaries.Count} học viên, cột Tổng Tín Chỉ và TBM ở cuối, các đợt thi chưa có điểm để trống, các dòng thiếu môn đã tô màu vàng).");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi xuất file Excel: {ex.Message}");
                }
            });
        }

        #region EXCEL IMPORT HELPERS
        private static bool IsSummaryOrEndColumn(string headerRow4, string headerRow5)
        {
            return IsSummaryOrEndColumn(headerRow4) || IsSummaryOrEndColumn(headerRow5);
        }

        private static bool IsSummaryOrEndColumn(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            string v = val.Trim();
            return v.Equals("TBM", StringComparison.OrdinalIgnoreCase) ||
                   v.StartsWith("TBM", StringComparison.OrdinalIgnoreCase) ||
                   v.Contains("Tổng Tín Chỉ", StringComparison.OrdinalIgnoreCase) ||
                   v.Contains("Xếp loại", StringComparison.OrdinalIgnoreCase) ||
                   v.StartsWith("Số môn", StringComparison.OrdinalIgnoreCase) ||
                   v.Contains("Số môn <", StringComparison.OrdinalIgnoreCase) ||
                   v.Contains("Số môn<", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFooterOrSummaryRow(string cell1, string fullName)
        {
            string c1 = cell1?.Trim().ToUpperInvariant() ?? string.Empty;
            string fn = fullName?.Trim().ToUpperInvariant() ?? string.Empty;

            return c1.StartsWith("TÍN CHỈ") || c1.StartsWith("TỔNG") || c1.Contains("TÍN CHỈ") ||
                   c1.Contains("SỐ TÍN CHỈ") ||
                   fn.StartsWith("TÍN CHỈ") || fn.StartsWith("TỔNG") || fn.Contains("TÍN CHỈ");
        }

        public static (string Group, bool IsComponent) InferSubjectGroup(string rawName)
        {
            string subjName = (rawName ?? string.Empty).Trim();
            string upper = subjName.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(subjName))
                return (string.Empty, false);

            // 1. Triết học Mác - Lênin (TH M-L, THI TH M-L)
            if (upper.Contains("TH M-L") || upper.Contains("TH ML") || upper.Contains("TRIẾT HỌC") || upper.Contains("TRIET HOC"))
                return ("TH M-L", true);

            // 2. Kinh tế chính trị (KTCT, KT KTCT, Thi KTCT)
            if (upper.Contains("KTCT") || upper.Contains("KINH TẾ CHÍNH TRỊ") || upper.Contains("KINH TE CHINH TRI"))
                return ("KTCT", true);

            // 3. Chủ nghĩa xã hội (CNXH, Thi CNXH)
            if (upper.Contains("CNXH") || upper.Contains("CHỦ NGHĨA XÃ HỘI") || upper.Contains("CHU NGHIA XA HOI"))
                return ("CNXH", true);

            // 4. Lịch sử Đảng (LSĐ, Thi LSĐ)
            if (upper.Contains("LSĐ") || upper.Contains("LSD") || upper.Contains("LỊCH SỬ ĐẢNG") || upper.Contains("LICH SU DANG"))
                return ("LSĐ", true);

            // 5. Tư tưởng Hồ Chí Minh (TT HCM, TT HCM VĐ)
            if (upper.Contains("TT HCM") || upper.Contains("TTHCM") || upper.Contains("HỒ CHÍ MINH") || upper.Contains("HO CHI MINH"))
                return ("TT HCM", true);

            // 6. Chiến thuật bộ binh (bBB, bBB thi)
            if (upper.StartsWith("BBB") || upper.StartsWith("BB") || upper.Contains("BỘ BINH") || upper.Contains("BO BINH"))
                return ("bBB", true);

            // 7. Công nghệ thông tin (CNTT, CNTT1, CNTT2, Thi CNTT)
            if (upper.Contains("CNTT") || upper.Contains("TIN HỌC") || upper.Contains("TIN HOC"))
                return ("CNTT", true);

            // 8. Điều lệnh QLBĐ (ĐLQLBĐ, Thi ĐLQLBĐ)
            if (upper.Contains("ĐLQLBĐ") || upper.Contains("DLQLBD") || upper.Contains("QLBĐ") || upper.Contains("QLBD"))
                return ("ĐLQLBĐ", true);

            // 9. Vũ khí hủy diệt lớn (VKHD, Thi VKHDL)
            if (upper.Contains("VKHD") || upper.Contains("VKHDL") || upper.Contains("HỦY DIỆT") || upper.Contains("HUY DIET"))
                return ("VKHD", true);

            // 10. Kỹ thuật xe (KT Xe, KT XE)
            if (upper.StartsWith("KT XE") || upper.StartsWith("KTXE") || upper.Contains("KỸ THUẬT XE") || upper.Contains("KY THUAT XE"))
                return ("KT Xe", true);

            // 11. Địa hình quân sự (ĐHQS)
            if (upper.StartsWith("ĐHQS") || upper.StartsWith("DHQS") || upper.Contains("ĐỊA HÌNH") || upper.Contains("DIA HINH"))
                return ("ĐHQS", true);

            // 12. Võ thuật (Võ, Võ 1, Võ 2)
            if (upper.StartsWith("VÕ") || upper.StartsWith("VO") || upper.Contains("VÕ THUẬT") || upper.Contains("VO THUAT"))
                return ("Võ", true);

            // 13. Hậu cần quân sự (HC QS, HCQS)
            if (upper.Contains("HCQS") || (upper.StartsWith("HC") && upper.Contains("QS")) || upper.Contains("HẬU CẦN") || upper.Contains("HAU CAN"))
                return ("HCQS", true);

            // 14. Lựu đạn (LĐ 1, LĐ 2)
            if (upper.StartsWith("LĐ") || upper.StartsWith("LD") || upper.Contains("LỰU ĐẠN") || upper.Contains("LUU DAN"))
                return ("Lựu đạn", true);

            // 15. Bắn súng (1 B41, 2 AK, 2 RPK, AK 1, Bắn cđ, BS1)
            if (upper.Contains("B41") || upper.Contains("AK") || upper.Contains("RPK") || 
                upper.Contains("BẮN") || upper.Contains("BAN") || upper.StartsWith("BS"))
                return ("Bắn súng", true);

            // 16. Thể lực (100m, Bơi, bơi bao gói, Xà)
            if (upper.Contains("100M") || upper.Contains("BƠI") || upper.Contains("BOI") || 
                upper.Contains("XÀ") || upper.Contains("XA ") || upper.Equals("XÀ") || upper.Equals("XA"))
                return ("Thể lực", true);

            // 17. Chiến thuật (CSVC, CT (tổ), CT (CN), CT a)
            if (upper.Equals("CSVC") || upper.StartsWith("CT (") || upper.StartsWith("CT A") || upper.StartsWith("CT(") || upper.StartsWith("CT A"))
                return ("Chiến thuật", true);

            // 18. Chính trị & Tham mưu (CTĐ, CTTM, GDCT)
            if (upper.Equals("CTĐ") || upper.Equals("CTD") || upper.Equals("CTTM") || upper.Equals("GDCT"))
                return ("CTĐ & CTTM", true);

            // Môn bắt đầu bằng "Thi ..." nhưng không thuộc các môn trên
            if (subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase))
                return (subjName.Substring(4).Trim(), true);

            return (subjName, false);
        }

        private sealed class CadetMetadataColumns
        {
            public int ColStt { get; set; } = 1;
            public int ColCode { get; set; } = -1;
            public int ColUnit { get; set; } = -1;
            public int ColLastName { get; set; } = -1;
            public int ColFirstName { get; set; } = -1;
            public int ColFullName { get; set; } = -1;
            public int StartSubjectCol { get; set; } = 7;
        }

        private sealed class ExcelSheetLayout
        {
            public CadetMetadataColumns Meta { get; set; } = new();
            public int HeaderRow { get; set; } = 4;
            public int StudentStartRow { get; set; } = 5;
            public int CreditRow { get; set; } = 70;
            public int LastStudentRow { get; set; } = 69;
        }

        private static ExcelSheetLayout DetectExcelLayout(IXLWorksheet ws)
        {
            var layout = new ExcelSheetLayout();
            int lastRowUsed = ws.LastRowUsed()?.RowNumber() ?? 70;

            // 1. Tìm CreditRow (dòng có chữ "Số tín chỉ" hoặc "Tín chỉ")
            int detectedCreditRow = -1;
            for (int r = lastRowUsed; r >= 1; r--)
            {
                string c1 = ws.Cell(r, 1).GetString().Trim();
                string c2 = ws.Cell(r, 2).GetString().Trim();
                string c6 = ws.Cell(r, 6).GetString().Trim();
                if (c1.Contains("tín chỉ", StringComparison.OrdinalIgnoreCase) || 
                    c2.Contains("tín chỉ", StringComparison.OrdinalIgnoreCase) ||
                    c6.Contains("tín chỉ", StringComparison.OrdinalIgnoreCase))
                {
                    detectedCreditRow = r;
                    break;
                }
            }

            // 2. Tìm StudentStartRow
            int studentStartRow = -1;
            for (int r = 1; r <= Math.Min(25, lastRowUsed); r++)
            {
                string c1 = ws.Cell(r, 1).GetString().Trim();
                string c2 = ws.Cell(r, 2).GetString().Trim();
                string c4 = ws.Cell(r, 4).GetString().Trim();
                string c6 = ws.Cell(r, 6).GetString().Trim();

                bool isSttOne = int.TryParse(c1, out int stt) && stt == 1;
                bool hasCadetCode = !string.IsNullOrWhiteSpace(c2) && 
                                   !c2.Equals("MSSV", StringComparison.OrdinalIgnoreCase) && 
                                   !c2.Equals("Mã học viên", StringComparison.OrdinalIgnoreCase) &&
                                   !c2.Equals("Đơn vị", StringComparison.OrdinalIgnoreCase);

                bool hasCadetName = (!string.IsNullOrWhiteSpace(c4) && !c4.Contains("họ và tên", StringComparison.OrdinalIgnoreCase) && !c4.Contains("họ đệm", StringComparison.OrdinalIgnoreCase)) ||
                                    (!string.IsNullOrWhiteSpace(c6) && !c6.Contains("kết quả", StringComparison.OrdinalIgnoreCase) && !c6.Contains("họ và tên", StringComparison.OrdinalIgnoreCase));

                if ((isSttOne || hasCadetCode) && hasCadetName)
                {
                    studentStartRow = r;
                    break;
                }
            }

            if (studentStartRow == -1) studentStartRow = 5;

            // 3. HeaderRow
            int headerRow = studentStartRow - 1;
            bool hasSubjectHeader = false;
            for (int c = 6; c <= 15; c++)
            {
                string txt = ws.Cell(headerRow, c).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(txt) && !double.TryParse(txt, out _))
                {
                    hasSubjectHeader = true;
                    break;
                }
            }
            if (!hasSubjectHeader && headerRow > 1)
            {
                for (int c = 6; c <= 15; c++)
                {
                    string txt = ws.Cell(headerRow - 1, c).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(txt) && !double.TryParse(txt, out _))
                    {
                        headerRow--;
                        break;
                    }
                }
            }

            // 4. CreditRow
            if (detectedCreditRow == -1)
            {
                bool row1HasNumbers = false;
                for (int c = 6; c <= 12; c++)
                {
                    string v = ws.Cell(1, c).GetString().Trim().Replace(',', '.');
                    if (double.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double cv) && cv > 0)
                    {
                        row1HasNumbers = true;
                        break;
                    }
                }
                detectedCreditRow = row1HasNumbers ? 1 : 70;
            }

            // 5. LastStudentRow
            int lastStudentRow = detectedCreditRow > studentStartRow ? detectedCreditRow - 1 : lastRowUsed;
            while (lastStudentRow >= studentStartRow)
            {
                string c1 = ws.Cell(lastStudentRow, 1).GetString().Trim();
                string c4 = ws.Cell(lastStudentRow, 4).GetString().Trim();
                string c6 = ws.Cell(lastStudentRow, 6).GetString().Trim();
                if (c1.Contains("tín chỉ", StringComparison.OrdinalIgnoreCase) || 
                    c1.Contains("tổng", StringComparison.OrdinalIgnoreCase) ||
                    (string.IsNullOrWhiteSpace(c1) && string.IsNullOrWhiteSpace(c4) && string.IsNullOrWhiteSpace(c6)))
                {
                    lastStudentRow--;
                }
                else
                {
                    break;
                }
            }

            // 6. CadetMetadataColumns
            var meta = new CadetMetadataColumns();
            int maxMetaCol = 1;

            for (int c = 1; c <= 15; c++)
            {
                var texts = new List<string>();
                for (int r = 1; r < studentStartRow; r++)
                {
                    string s = ws.Cell(r, c).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(s)) texts.Add(s);
                }
                string combined = string.Join(" ", texts).ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(combined)) continue;
                if (IsSummaryOrEndColumn(combined)) break;

                // Mã học viên / CadetCode / MSSV
                if (meta.ColCode == -1 &&
                    (combined.Contains("mssv") || combined.Contains("mã hv") || combined.Contains("mã học viên") ||
                     combined.Contains("mahv") || combined.Contains("shsv") || combined.Contains("cadetcode") ||
                     combined.Contains("mã sv") || combined.Contains("số hiệu") || combined.Equals("mã")))
                {
                    meta.ColCode = c;
                    maxMetaCol = Math.Max(maxMetaCol, c);
                    continue;
                }

                // Đơn vị / Lớp / Trung đội
                if (meta.ColUnit == -1 &&
                    (combined.Contains("đơn vị") || combined.Contains("đại đội") || combined.Contains("trung đội") ||
                     combined.Equals("lớp") || combined.Contains("don vi") || combined.Contains("unit")))
                {
                    meta.ColUnit = c;
                    maxMetaCol = Math.Max(maxMetaCol, c);
                    continue;
                }

                // Họ đệm
                if (meta.ColLastName == -1 &&
                    (combined.Contains("họ đệm") || combined.Contains("họ và tên đệm") || combined.Contains("họ và đệm") || combined.Contains("ho dem")))
                {
                    meta.ColLastName = c;
                    maxMetaCol = Math.Max(maxMetaCol, c);
                    continue;
                }

                // Tên
                if (meta.ColFirstName == -1 &&
                    (combined.Equals("tên") || combined.Contains("tên gọi") ||
                     (combined.Contains("tên") && !combined.Contains("họ") && !combined.Contains("môn") && !combined.Contains("ghép"))))
                {
                    meta.ColFirstName = c;
                    maxMetaCol = Math.Max(maxMetaCol, c);
                    continue;
                }

                // Họ và tên ghép / FullName
                if (meta.ColFullName == -1 &&
                    (combined.Contains("họ và tên ghép") || combined.Contains("họ tên ghép") ||
                     combined.Contains("họ và tên") || combined.Contains("họ tên") ||
                     combined.Contains("ho va ten") || combined.Contains("fullname")))
                {
                    meta.ColFullName = c;
                    maxMetaCol = Math.Max(maxMetaCol, c);
                    continue;
                }

                // STT / TT
                if (combined.Equals("tt") || combined.Equals("stt") || combined.Contains("thứ tự"))
                {
                    meta.ColStt = c;
                    maxMetaCol = Math.Max(maxMetaCol, c);
                    continue;
                }
            }

            // Kiểm tra phân tách Họ đệm, Tên, Họ và tên từ dòng học viên đầu tiên
            if (studentStartRow > 0)
            {
                for (int c = 1; c <= 8; c++)
                {
                    string v1 = ws.Cell(studentStartRow, c).GetString().Trim();
                    string v2 = ws.Cell(studentStartRow, c + 1).GetString().Trim();
                    string v3 = ws.Cell(studentStartRow, c + 2).GetString().Trim();

                    if (!string.IsNullOrWhiteSpace(v1) && !string.IsNullOrWhiteSpace(v2) && !string.IsNullOrWhiteSpace(v3))
                    {
                        string combinedName = $"{v1} {v2}".Trim();
                        if (v3.Equals(combinedName, StringComparison.OrdinalIgnoreCase) || 
                            (v3.StartsWith(v1, StringComparison.OrdinalIgnoreCase) && v3.EndsWith(v2, StringComparison.OrdinalIgnoreCase)))
                        {
                            meta.ColLastName = c;
                            meta.ColFirstName = c + 1;
                            meta.ColFullName = c + 2;
                            maxMetaCol = Math.Max(maxMetaCol, c + 2);
                            break;
                        }
                    }
                }
            }

            if (meta.ColCode == 2 && meta.ColUnit == -1)
            {
                string uVal = ws.Cell(studentStartRow, 3).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(uVal) && (uVal.StartsWith("b") || uVal.StartsWith("c") || uVal.StartsWith("d")))
                {
                    meta.ColUnit = 3;
                    maxMetaCol = Math.Max(maxMetaCol, 3);
                }
            }

            if (meta.ColFullName == -1)
            {
                string c4 = ws.Cell(studentStartRow, 4).GetString().Trim();
                string c5 = ws.Cell(studentStartRow, 5).GetString().Trim();
                string c6 = ws.Cell(studentStartRow, 6).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(c6) && (c6 == $"{c4} {c5}".Trim() || c6.Contains(c5)))
                {
                    meta.ColLastName = 4;
                    meta.ColFirstName = 5;
                    meta.ColFullName = 6;
                    maxMetaCol = Math.Max(maxMetaCol, 6);
                }
                else if (meta.ColLastName != -1 && meta.ColFirstName != -1)
                {
                    meta.ColFullName = Math.Max(meta.ColLastName, meta.ColFirstName) + 1;
                    maxMetaCol = Math.Max(maxMetaCol, meta.ColFullName);
                }
            }

            meta.StartSubjectCol = Math.Max(maxMetaCol + 1, meta.ColFullName > 0 ? meta.ColFullName + 1 : 7);

            layout.Meta = meta;
            layout.HeaderRow = headerRow;
            layout.StudentStartRow = studentStartRow;
            layout.CreditRow = detectedCreditRow;
            layout.LastStudentRow = lastStudentRow;

            return layout;
        }

        private static Dictionary<int, (string Name, double Credits)> ScanSubjectColumns(
            IXLWorksheet ws, int startCol, int headerRow, int creditRow)
        {
            var result = new Dictionary<int, (string Name, double Credits)>();
            var scannedNames = new List<(int Col, string RawName)>();
            int consecutiveEmpty = 0;

            for (int c = startCol; c <= 120; c++)
            {
                string headerVal = ws.Cell(headerRow, c).GetString();
                string subjName = System.Text.RegularExpressions.Regex.Replace(headerVal, @"\s+", " ").Trim();

                if (IsSummaryOrEndColumn(subjName))
                    break;

                if (string.IsNullOrWhiteSpace(subjName))
                {
                    consecutiveEmpty++;
                    if (scannedNames.Count > 0 && consecutiveEmpty >= 3)
                        break;
                    continue;
                }

                consecutiveEmpty = 0;
                scannedNames.Add((c, subjName));
            }

            var duplicateCounters = scannedNames
                .GroupBy(x => x.RawName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1 && !System.Text.RegularExpressions.Regex.IsMatch(g.Key, @"\d+$"))
                .ToDictionary(g => g.Key, g => 0, StringComparer.OrdinalIgnoreCase);

            foreach (var (c, rawName) in scannedNames)
            {
                string finalName = rawName;
                if (duplicateCounters.ContainsKey(rawName))
                {
                    duplicateCounters[rawName]++;
                    finalName = $"{rawName} {duplicateCounters[rawName]}";
                }

                double credits = 1.0;
                if (creditRow > 0)
                {
                    string creditStr = ws.Cell(creditRow, c).GetString().Trim().Replace(',', '.');
                    if (double.TryParse(creditStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedCredits) && parsedCredits > 0)
                    {
                        credits = parsedCredits;
                    }
                }

                result[c] = (finalName, credits);
            }

            return result;
        }

        private async Task<(bool Success, string Message, int ImportedCadets, int ImportedScores, int MajorSubjectsCount)> ExecuteImportStandardTbmExcelAsync(IXLWorksheet ws)
        {
            var layout = DetectExcelLayout(ws);
            var meta = layout.Meta;
            var scannedSubjects = ScanSubjectColumns(ws, meta.StartSubjectCol, layout.HeaderRow, layout.CreditRow);

            if (scannedSubjects.Count == 0)
            {
                return (false, "Không tìm thấy cột môn học/điểm kiểm tra nào hợp lệ trong file Excel.", 0, 0, 0);
            }

            // 1. Gom nhóm các cột kiểm tra thành các Môn lớn (Major Subjects)
            var groupBuckets = new Dictionary<string, List<(int Col, string CompName, double Credits)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (c, (compName, credits)) in scannedSubjects)
            {
                var (grp, _) = InferSubjectGroup(compName);
                string groupKey = !string.IsNullOrWhiteSpace(grp) ? grp : compName;
                if (!groupBuckets.ContainsKey(groupKey))
                    groupBuckets[groupKey] = new List<(int Col, string CompName, double Credits)>();
                groupBuckets[groupKey].Add((c, compName, credits));
            }

            var subjectsInDb = await _context.CreditSubjects.Include(s => s.Components).ToListAsync();
            var cadetsInDb = await _context.Cadets.ToListAsync();
            var existingScoresInDb = await _context.CreditScoreRecords.ToListAsync();

            // 2. Tạo hoặc cập nhật CreditSubject (Môn lớn) và SubjectAssessmentComponent (Đợt kiểm tra / thi con)
            var colToCompMap = new Dictionary<int, (CreditSubject MajorSubj, SubjectAssessmentComponent Comp)>();
            int subjOrder = subjectsInDb.Count + 1;

            foreach (var kvp in groupBuckets)
            {
                string groupKey = kvp.Key;
                var compItems = kvp.Value;
                double totalGroupCredits = Math.Round(compItems.Sum(x => x.Credits), 2);
                if (totalGroupCredits <= 0) totalGroupCredits = 1.0;

                var majorSubj = subjectsInDb.FirstOrDefault(s => 
                    s.SubjectName.Equals(groupKey, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(s.SubjectGroup) && s.SubjectGroup.Equals(groupKey, StringComparison.OrdinalIgnoreCase)));

                if (majorSubj == null)
                {
                    string subjCode = $"TC{subjOrder++:D2}";
                    majorSubj = new CreditSubject
                    {
                        SubjectCode = subjCode,
                        SubjectName = groupKey,
                        Credits = totalGroupCredits,
                        AssessmentType = compItems.Count > 1 ? "Kiểm tra và thi" : "Kiểm tra thường xuyên",
                        SubjectGroup = groupKey,
                        IsComponent = false,
                        Description = $"Môn học lớn gồm {compItems.Count} đợt kiểm tra / thi ({totalGroupCredits} tín chỉ)",
                        CreatedAt = DateTime.Now
                    };
                    _context.CreditSubjects.Add(majorSubj);
                    subjectsInDb.Add(majorSubj);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    majorSubj.Credits = totalGroupCredits;
                    majorSubj.SubjectName = groupKey;
                    majorSubj.SubjectGroup = groupKey;
                    majorSubj.IsComponent = false;
                }

                var existingComps = await _context.SubjectAssessmentComponents
                    .Where(cp => cp.CreditSubjectId == majorSubj.Id)
                    .ToListAsync();

                int orderIdx = 1;
                foreach (var (col, compName, compCredits) in compItems)
                {
                    var comp = existingComps.FirstOrDefault(cp => 
                        cp.ComponentName.Equals(compName, StringComparison.OrdinalIgnoreCase) ||
                        cp.OrderIndex == orderIdx);

                    if (comp == null)
                    {
                        comp = new SubjectAssessmentComponent
                        {
                            CreditSubjectId = majorSubj.Id,
                            ComponentName = compName,
                            Credits = compCredits > 0 ? compCredits : 1.0,
                            OrderIndex = orderIdx,
                            CreatedAt = DateTime.Now
                        };
                        _context.SubjectAssessmentComponents.Add(comp);
                        await _context.SaveChangesAsync();
                        existingComps.Add(comp);
                    }
                    else
                    {
                        comp.ComponentName = compName;
                        comp.Credits = compCredits > 0 ? compCredits : 1.0;
                        comp.OrderIndex = orderIdx;
                    }

                    colToCompMap[col] = (majorSubj, comp);
                    orderIdx++;
                }
            }
            await _context.SaveChangesAsync();

            // 3. Đọc danh sách học viên và điểm từng đợt kiểm tra
            int importedCadetsCount = 0;
            int importedScoresCount = 0;

            for (int r = layout.StudentStartRow; r <= layout.LastStudentRow; r++)
            {
                string cell1 = ws.Cell(r, meta.ColStt).GetString().Trim();
                string codeFromExcel = meta.ColCode > 0 ? ws.Cell(r, meta.ColCode).GetString().Trim() : string.Empty;
                string fullName = meta.ColFullName > 0 ? ws.Cell(r, meta.ColFullName).GetString().Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(fullName) && meta.ColLastName > 0 && meta.ColFirstName > 0)
                {
                    string last = ws.Cell(r, meta.ColLastName).GetString().Trim();
                    string first = ws.Cell(r, meta.ColFirstName).GetString().Trim();
                    fullName = $"{last} {first}".Trim();
                }

                if (IsFooterOrSummaryRow(cell1, fullName))
                    continue;

                fullName = System.Text.RegularExpressions.Regex.Replace(fullName, @"\s+", " ").Trim();
                if (string.IsNullOrWhiteSpace(fullName)) continue;

                string unit = meta.ColUnit > 0 ? ws.Cell(r, meta.ColUnit).GetString().Trim() : "b1";
                if (string.IsNullOrWhiteSpace(unit)) unit = "b1";

                // Khớp học viên đa tầng:
                // Ưu tiên 1: Khớp theo Mã học viên
                Cadet? cadet = null;
                if (!string.IsNullOrWhiteSpace(codeFromExcel))
                {
                    cadet = cadetsInDb.FirstOrDefault(cd => 
                        !string.IsNullOrWhiteSpace(cd.CadetCode) && 
                        cd.CadetCode.Equals(codeFromExcel, StringComparison.OrdinalIgnoreCase));
                }

                // Ưu tiên 2: Khớp theo Họ tên + Đơn vị
                if (cadet == null && !string.IsNullOrWhiteSpace(unit))
                {
                    cadet = cadetsInDb.FirstOrDefault(cd =>
                        System.Text.RegularExpressions.Regex.Replace(cd.FullName, @"\s+", " ").Equals(fullName, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(cd.Unit) &&
                        cd.Unit.Equals(unit, StringComparison.OrdinalIgnoreCase));
                }

                // Ưu tiên 3: Khớp theo Họ tên
                if (cadet == null)
                {
                    cadet = cadetsInDb.FirstOrDefault(cd =>
                        System.Text.RegularExpressions.Regex.Replace(cd.FullName, @"\s+", " ").Equals(fullName, StringComparison.OrdinalIgnoreCase));
                }

                // QUY TẮC BẢO LƯU: Nếu học viên đã tồn tại, TUYỆT ĐỐI KHÔNG ĐƯỢC sửa hoặc ghi đè CadetCode!
                if (cadet == null)
                {
                    string newCode = codeFromExcel;
                    if (string.IsNullOrWhiteSpace(newCode) || cadetsInDb.Any(c => c.CadetCode.Equals(newCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        int seq = cadetsInDb.Count + 1;
                        do
                        {
                            newCode = $"HV{DateTime.Now:yy}{seq:D3}";
                            seq++;
                        } while (cadetsInDb.Any(c => c.CadetCode.Equals(newCode, StringComparison.OrdinalIgnoreCase)));
                    }

                    cadet = new Cadet
                    {
                        CadetCode = newCode,
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
                    // Học viên đã tồn tại: giữ nguyên CadetCode 100%!
                    if (string.IsNullOrWhiteSpace(cadet.Unit) && !string.IsNullOrWhiteSpace(unit))
                    {
                        cadet.Unit = unit;
                    }
                }

                // Đọc điểm cho từng cột đợt thi
                foreach (var (c, (majorSubj, comp)) in colToCompMap)
                {
                    string scoreStr = ws.Cell(r, c).GetString().Trim().Replace(',', '.');
                    if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double scoreVal))
                    {
                        if (scoreVal >= 0 && scoreVal <= 10)
                        {
                            var existingScore = existingScoresInDb.FirstOrDefault(s => 
                                s.CadetId == cadet.Id && 
                                (s.ComponentId == comp.Id || (s.CreditSubjectId == majorSubj.Id && s.ComponentId == null)));

                            if (existingScore != null)
                            {
                                existingScore.FinalScore = scoreVal;
                                existingScore.RegularScore = scoreVal;
                                existingScore.CreditSubjectId = majorSubj.Id;
                                existingScore.ComponentId = comp.Id;
                                existingScore.ExamDate = DateTime.Today;
                            }
                            else
                            {
                                var newScore = new CreditScoreRecord
                                {
                                    CadetId = cadet.Id,
                                    CreditSubjectId = majorSubj.Id,
                                    ComponentId = comp.Id,
                                    FinalScore = scoreVal,
                                    RegularScore = scoreVal,
                                    ExamSession = "Toàn khóa",
                                    ExamDate = DateTime.Today,
                                    Notes = "Nhập tự động từ file chuẩn TBM",
                                    CreatedAt = DateTime.Now
                                };
                                _context.CreditScoreRecords.Add(newScore);
                                existingScoresInDb.Add(newScore);
                            }

                            importedScoresCount++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            int majorSubjsCount = await _context.CreditSubjects.CountAsync(s => !s.IsComponent);

            return (true, $"Đã nhập thành công từ file Excel: {majorSubjsCount} môn lớn ({colToCompMap.Count} đợt kiểm tra / thi), {importedCadetsCount} học viên mới (bảo lưu nguyên vẹn mã số học viên hiện có), cập nhật {importedScoresCount} đầu điểm!", importedCadetsCount, importedScoresCount, majorSubjsCount);
        }
        #endregion

        public async Task<(bool Success, string Message, int ImportedCadets, int ImportedScores)> ImportStandardTbmExcelAsync(string filePath)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (!File.Exists(filePath))
                        return (false, "File không tồn tại trên hệ thống.", 0, 0);

                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var wb = new XLWorkbook(fs);
                    var ws = wb.Worksheets.FirstOrDefault();
                    if (ws == null)
                        return (false, "File Excel không chứa bất kỳ sheet nào.", 0, 0);

                    // Dọn dẹp điểm số và môn học tín chỉ cũ để đồng bộ chuẩn theo cấu trúc mới của file Excel, bảo lưu 100% học viên
                    var oldScores = await _context.CreditScoreRecords.ToListAsync();
                    _context.CreditScoreRecords.RemoveRange(oldScores);

                    var oldComps = await _context.SubjectAssessmentComponents.ToListAsync();
                    _context.SubjectAssessmentComponents.RemoveRange(oldComps);

                    var oldSubjs = await _context.CreditSubjects.ToListAsync();
                    _context.CreditSubjects.RemoveRange(oldSubjs);

                    await _context.SaveChangesAsync();

                    var res = await ExecuteImportStandardTbmExcelAsync(ws);
                    return (res.Success, res.Message, res.ImportedCadets, res.ImportedScores);
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
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var wb = new XLWorkbook(fs);
                    var ws = wb.Worksheets.FirstOrDefault();
                    if (ws == null)
                        return (false, "File Excel không chứa bất kỳ sheet nào.", 0, 0, 0);

                    var res = await ExecuteImportStandardTbmExcelAsync(ws);
                    if (!res.Success)
                    {
                        return (false, res.Message, 0, 0, 0);
                    }

                    return (true, $"Làm sạch và nạp lại CSDL thành công: {res.ImportedCadets} học viên, {res.MajorSubjectsCount} môn lớn, {res.ImportedScores} điểm số!", res.ImportedCadets, res.MajorSubjectsCount, res.ImportedScores);
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
                    return (false, "MÃ£ mÃ´n há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
                if (string.IsNullOrWhiteSpace(subject.SubjectName))
                    return (false, "TÃªn mÃ´n há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

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
                        return (false, $"MÃ£ mÃ´n há»c '{subject.SubjectCode}' Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.");

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
                    return (true, $"ThÃªm má»›i mÃ´n há»c '{subject.SubjectName}' cÃ¹ng {compList.Count} Ä‘á»£t kiá»ƒm tra thÃ nh cÃ´ng.");
                }
                else
                {
                    var existing = await _context.CreditSubjects
                        .Include(s => s.Components)
                        .FirstOrDefaultAsync(s => s.Id == subject.Id);

                    if (existing == null)
                        return (false, "KhÃ´ng tÃ¬m tháº¥y mÃ´n há»c cáº§n sá»­a.");

                    bool codeConflict = await _context.CreditSubjects
                        .AnyAsync(s => s.SubjectCode == subject.SubjectCode && s.Id != subject.Id);
                    if (codeConflict)
                        return (false, $"MÃ£ mÃ´n há»c '{subject.SubjectCode}' Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng.");

                    existing.SubjectCode = subject.SubjectCode;
                    existing.SubjectName = subject.SubjectName;
                    existing.AssessmentType = subject.AssessmentType;
                    existing.Description = subject.Description;
                    existing.Credits = Math.Round(compList.Sum(c => c.Credits), 2);

                    // Äá»“ng bá»™ cÃ¡c Ä‘á»£t kiá»ƒm tra
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
                    return (true, $"Cáº­p nháº­t mÃ´n há»c '{existing.SubjectName}' vÃ  cÃ¡c Ä‘á»£t kiá»ƒm tra thÃ nh cÃ´ng.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i lÆ°u mÃ´n há»c: {ex.Message}");
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

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Táº¥t cáº£")
                cadetQuery = cadetQuery.Where(c => c.Unit == unit);

            if (!string.IsNullOrWhiteSpace(className) && className != "Táº¥t cáº£")
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

            // Äáº¿m sá»‘ lÆ°á»£ng há»c viÃªn cÃ³ Ä‘iá»ƒm cho tá»«ng Ä‘á»£t kiá»ƒm tra
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
                bool hasAnyComponents = await _context.SubjectAssessmentComponents.AnyAsync();
                if (!hasAnyComponents)
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
                        var compBySubj = allComps.GroupBy(c => c.CreditSubjectId).ToDictionary(g => g.Key, g => g.First());

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
                        .Include(s => s.Components)
                        .Where(s => s.SubjectGroup == group)
                        .OrderBy(s => s.Id)
                        .ToListAsync();

                    if (subjs.Count <= 1) continue;

                    var primary = subjs.FirstOrDefault(s => s.SubjectName.Equals(group, StringComparison.OrdinalIgnoreCase)) ?? subjs[0];

                    if (!primary.SubjectName.Equals(group, StringComparison.OrdinalIgnoreCase) && 
                        !subjs.Any(s => s.SubjectName.Equals(group, StringComparison.OrdinalIgnoreCase)))
                    {
                        primary.SubjectName = group;
                        changed = true;
                    }

                    primary.IsComponent = false;

                    var existingComps = await _context.SubjectAssessmentComponents
                        .Where(c => c.CreditSubjectId == primary.Id)
                        .ToListAsync();

                    // Nếu số component đã khớp với số môn con và các môn con khác đã là IsComponent
                    bool alreadyConsolidated = existingComps.Count == subjs.Count &&
                                              subjs.Where(s => s.Id != primary.Id).All(s => s.IsComponent);

                    if (alreadyConsolidated)
                    {
                        // Giữ nguyên tổng tín chỉ theo tổng tín chỉ các components, KHÔNG CỘNG DỒN
                        double compSum = Math.Round(existingComps.Sum(c => c.Credits), 2);
                        if (Math.Abs(primary.Credits - compSum) > 0.001 && compSum > 0)
                        {
                            primary.Credits = compSum;
                            changed = true;
                        }
                        continue;
                    }

                    // Tái cấu trúc nhưng TUYỆT ĐỐI KHÔNG xóa các component đã có điểm mà tái sử dụng / cập nhật
                    for (int i = 0; i < subjs.Count; i++)
                    {
                        var s = subjs[i];
                        var existingComp = existingComps.FirstOrDefault(c => 
                            c.ComponentName.Equals(s.SubjectName, StringComparison.OrdinalIgnoreCase) || 
                            c.OrderIndex == i + 1);

                        int compId;
                        if (existingComp != null)
                        {
                            existingComp.ComponentName = s.SubjectName;
                            if (s.Credits > 0) existingComp.Credits = s.Credits;
                            existingComp.OrderIndex = i + 1;
                            compId = existingComp.Id;
                        }
                        else
                        {
                            var newComp = new SubjectAssessmentComponent
                            {
                                CreditSubjectId = primary.Id,
                                ComponentName = s.SubjectName,
                                Credits = s.Credits > 0 ? s.Credits : 1.0,
                                OrderIndex = i + 1,
                                CreatedAt = DateTime.Now
                            };
                            _context.SubjectAssessmentComponents.Add(newComp);
                            await _context.SaveChangesAsync();
                            existingComps.Add(newComp);
                            compId = newComp.Id;
                        }

                        // Chuyển các điểm số trỏ vào môn thành phần hoặc chưa có ComponentId
                        var scores = await _context.CreditScoreRecords
                            .Where(sc => sc.CreditSubjectId == s.Id || (sc.CreditSubjectId == primary.Id && sc.ComponentId == null))
                            .ToListAsync();

                        foreach (var sc in scores)
                        {
                            sc.CreditSubjectId = primary.Id;
                            if (!sc.ComponentId.HasValue || sc.ComponentId.Value == 0)
                            {
                                sc.ComponentId = compId;
                            }
                        }

                        if (s.Id != primary.Id)
                        {
                            s.IsComponent = true;
                        }
                    }

                    // Tính tổng tín chỉ môn chính bằng tổng tín chỉ các thành phần con
                    primary.Credits = Math.Round(existingComps.Sum(c => c.Credits), 2);
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
                        await _context.SaveChangesAsync();

                        var scores = await _context.CreditScoreRecords
                            .Where(sc => sc.CreditSubjectId == ms.Id && sc.ComponentId == null)
                            .ToListAsync();
                        foreach (var sc in scores)
                        {
                            sc.ComponentId = comp.Id;
                        }
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

            // Láº¥y toÃ n bá»™ Ä‘iá»ƒm cá»§a há»c viÃªn nÃ y á»Ÿ mÃ´n há»c
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

            // TÃ¬m cÃ¡c component active (>10 há»c viÃªn khÃ¡c Ä‘Ã£ cÃ³ Ä‘iá»ƒm)
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
