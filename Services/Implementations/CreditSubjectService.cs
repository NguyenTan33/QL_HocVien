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
            var allScores = await _context.CreditScoreRecords
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

            // Tá»•ng tÃ­n chá»‰ toÃ n khÃ³a chuáº©n (tÃ­nh tá»« cÃ¡c Ä‘á»£t thi hoáº·c 62.90 theo file Excel chuáº©n)
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

                // Äiá»ƒm theo tá»«ng Ä‘á»£t kiá»ƒm tra cá»§a há»c viÃªn nÃ y
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

                // TÃ­nh Ä‘iá»ƒm mÃ´n lá»›n: Äiá»ƒm trung bÃ¬nh mÃ´n CHá»ˆ CÃ“ KHI 100% CÃC Cá»˜T Cá»¦A MÃ”N CHÃNH ÄÆ¯á»¢C NHáº¬P
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

                // Kiá»ƒm tra thiáº¿u Ä‘á»£t thi trong cÃ¡c Ä‘á»£t ÄÃƒ DIá»„N RA (>= 20 há»c viÃªn cÃ³ Ä‘iá»ƒm)
                // Náº¿u Ä‘á»£t kiá»ƒm tra chÆ°a cÃ³ ai thi (< 20 há»c sinh) thÃ¬ coi nhÆ° chÆ°a diá»…n ra vÃ  KHÃ”NG bá»‹ Ä‘Ã¡nh vÃ ng
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

                // TÃ­nh GPA tÃ­ch lÅ©y toÃ n khÃ³a trÃªn cÃ¡c Ä‘á»£t thi Ä‘Ã£ hoÃ n thÃ nh
                var scoredComponents = allComponents
                    .Where(c => cadetCompScores.TryGetValue(c.Id, out var sc) && sc.HasValue && sc.Value >= 0)
                    .Select(c => (score: cadetCompScores[c.Id]!.Value, credits: c.Credits));

                dto.Gpa = _calculator.CalculateCurriculumTbm(scoredComponents, curriculumCredits);
                dto.TotalCreditsEarned = Math.Round(scoredComponents.Sum(sc => sc.credits), 2);
                dto.TotalSubjectsCompleted = majorSubjects.Count(s => dto.SubjectScores.TryGetValue(s.Id, out var sc) && sc.HasValue);

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
                    var ws = wb.Worksheets.Add("Báº£ng Äiá»ƒm TBM Chuáº©n");

                    // Láº¥y danh sÃ¡ch toÃ n bá»™ cÃ¡c Ä‘á»£t thi / kiá»ƒm tra thÃ nh pháº§n sáº¯p xáº¿p theo mÃ´n chÃ­nh
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

                    // DÃ²ng 1: Sá»‘ tÃ­n chá»‰ cá»§a tá»«ng Ä‘á»£t kiá»ƒm tra / thi (Khá»›p file Äiá»ƒm TBM chuáº©n .xlsx)
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

                    // Ã” tá»•ng tÃ­n chá»‰ toÃ n khÃ³a táº¡i dÃ²ng 1 cá»§a cá»™t TBM (vÃ­ dá»¥ Ã´ BK1 = 62.90)
                    int tbmCol = col;
                    ws.Cell(1, tbmCol).Value = totalCurriculumCredits;
                    ws.Cell(1, tbmCol).Style.NumberFormat.Format = "0.00";
                    ws.Cell(1, tbmCol).Style.Font.Bold = true;
                    ws.Cell(1, tbmCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // TiÃªu Ä‘á» bÃ¡o cÃ¡o
                    ws.Cell(3, 1).Value = "Káº¾T QUáº¢ Há»ŒC Táº¬P TOÃ€N KHÃ“A VÃ€ ÄIá»‚M TRUNG BÃŒNH MÃ”N (TBM)";
                    ws.Range(3, 1, 3, tbmCol + 3).Merge();
                    ws.Cell(3, 1).Style.Font.Bold = true;
                    ws.Cell(3, 1).Style.Font.FontSize = 14;
                    ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
                    ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // DÃ²ng 4 & 5: TiÃªu Ä‘á» cá»™t
                    int hRow1 = 4;
                    int hRow2 = 5;

                    ws.Cell(hRow1, 1).Value = "TT";
                    ws.Cell(hRow1, 2).Value = "ÄÆ¡n vá»‹";
                    ws.Cell(hRow1, 3).Value = "Há» vÃ  tÃªn Ä‘á»‡m";
                    ws.Cell(hRow1, 4).Value = "TÃªn";
                    ws.Cell(hRow1, 5).Value = "Há» vÃ  tÃªn ghÃ©p";

                    col = startCol;
                    foreach (var comp in components)
                    {
                        // Xuáº¥t tÃªn tá»«ng Ä‘á»£t kiá»ƒm tra / thi (vd: CNTT1, CNTT2, CNTT, Thi CNTT...)
                        ws.Cell(hRow2, col).Value = comp.ComponentName;
                        col++;
                    }

                    ws.Cell(hRow2, tbmCol).Value = "TBM";
                    ws.Cell(hRow1, tbmCol + 1).Value = "Xáº¿p loáº¡i há»c táº­p";
                    ws.Cell(hRow1, tbmCol + 2).Value = "Sá»‘ mÃ´n <7";
                    ws.Cell(hRow1, tbmCol + 3).Value = "Sá»‘ mÃ´n Ä‘Ã£ há»c";

                    int lastCol = tbmCol + 3;

                    var headerRange = ws.Range(hRow1, 1, hRow2, lastCol);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    headerRange.Style.Alignment.WrapText = true;

                    // DÃ²ng dá»¯ liá»‡u há»c viÃªn (báº¯t Ä‘áº§u tá»« dÃ²ng 6)
                    int row = 6;
                    int stt = 1;

                    foreach (var item in summaries)
                    {
                        // Kiá»ƒm tra há»c viÃªn thiáº¿u mÃ´n: TÃ” TOÃ€N Bá»˜ DÃ’NG MÃ€U VÃ€NG (#FFFF00) NHÆ¯ FILE EXCEL Gá»C
                        if (item.HasMissingSubjects)
                        {
                            ws.Range(row, 1, row, lastCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF00");
                        }

                        ws.Cell(row, 1).Value = stt++;
                        ws.Cell(row, 2).Value = item.Unit; // PhÃ¢n Ä‘á»™i (b1, b2, b3...)

                        // TÃ¡ch há» Ä‘á»‡m vÃ  tÃªn
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
                            // Láº¥y Ä‘iá»ƒm theo tá»«ng Ä‘á»£t kiá»ƒm tra / thÃ nh pháº§n con
                            if (item.ComponentScores.TryGetValue(comp.Id, out var score) && score.HasValue && score.Value >= 0)
                            {
                                ws.Cell(row, col).Value = score.Value;
                                ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                completedCount++;
                                if (score.Value <= 6.99) belowSevenCount++;
                            }
                            else
                            {
                                // Náº¿u cÃ¡c Ä‘á»£t kiá»ƒm tra chÆ°a cÃ³ Ä‘iá»ƒm khi xuáº¥t excel Cá»¨ Äá»‚ TRá»NG Ã” HOÃ€N TOÃ€N
                                ws.Cell(row, col).Value = string.Empty;
                            }
                            col++;
                        }

                        // Cá»™t TBM (ROUNDDOWN 2 sá»‘ tháº­p phÃ¢n)
                        ws.Cell(row, tbmCol).Value = item.Gpa;
                        ws.Cell(row, tbmCol).Style.NumberFormat.Format = "0.00";
                        ws.Cell(row, tbmCol).Style.Font.Bold = true;
                        ws.Cell(row, tbmCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Cá»™t Xáº¿p loáº¡i
                        ws.Cell(row, tbmCol + 1).Value = item.AcademicRating;
                        ws.Cell(row, tbmCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, tbmCol + 1).Style.Font.Bold = true;

                        // Cá»™t Sá»‘ mÃ´n < 7
                        ws.Cell(row, tbmCol + 2).Value = belowSevenCount;
                        ws.Cell(row, tbmCol + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Cá»™t Sá»‘ mÃ´n Ä‘Ã£ há»c
                        ws.Cell(row, tbmCol + 3).Value = completedCount;
                        ws.Cell(row, tbmCol + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        row++;
                    }

                    // HÃ€NG CUá»I CÃ™NG Cá»¦A EXCEL: DÃ’NG TÃN CHá»ˆ Cá»¦A Tá»ªNG Äá»¢T KIá»‚M TRA HOáº¶C THI
                    int footerRow = row;
                    ws.Cell(footerRow, 1).Value = "TÃN CHá»ˆ Äá»¢T THI / KIá»‚M TRA";
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
                    return (true, $"ÄÃ£ xuáº¥t bÃ¡o cÃ¡o báº£ng Ä‘iá»ƒm chuáº©n TBM thÃ nh cÃ´ng ({summaries.Count} há»c viÃªn, cÃ¡c Ä‘á»£t thi chÆ°a cÃ³ Ä‘iá»ƒm Ä‘á»ƒ trá»‘ng, cÃ¡c dÃ²ng thiáº¿u mÃ´n Ä‘Ã£ tÃ´ mÃ u vÃ ng).");
                }
                catch (Exception ex)
                {
                    return (false, $"Lá»—i khi xuáº¥t file Excel: {ex.Message}");
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
                        return (false, "File khÃ´ng tá»“n táº¡i trÃªn há»‡ thá»‘ng.", 0, 0);

                    using var wb = new XLWorkbook(filePath);
                    var ws = wb.Worksheets.FirstOrDefault();
                    if (ws == null)
                        return (false, "File Excel khÃ´ng chá»©a báº¥t ká»³ sheet nÃ o.", 0, 0);

                    var subjectsInDb = await _context.CreditSubjects.ToListAsync();
                    var cadetsInDb = await _context.Cadets.ToListAsync();

                    int importedCadetsCount = 0;
                    int importedScoresCount = 0;

                    // 0. Dá»n dáº¹p cÃ¡c mÃ´n há»c máº«u ban Ä‘áº§u náº¿u cÃ³ (Ä‘á»ƒ dá»¯ liá»‡u Ä‘á»“ng bá»™ chuáº©n 100% vá»›i 57 mÃ´n cá»§a Excel)
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

                    // 1. Äá»c danh má»¥c mÃ´n há»c vÃ  sá»‘ tÃ­n chá»‰ (Cá»™t 6 Ä‘áº¿n khi gáº·p TBM, thÆ°á»ng lÃ  cá»™t 62)
                    var colSubjectMap = new Dictionary<int, CreditSubject>();

                    for (int c = 6; c <= 100; c++)
                    {
                        string rawName = ws.Cell(5, c).GetString();
                        string subjName = System.Text.RegularExpressions.Regex.Replace(rawName, @"\s+", " ").Trim();

                        if (string.IsNullOrWhiteSpace(subjName))
                        {
                            // Náº¿u Ã´ TBM thÃ¬ dá»«ng láº¡i
                            if (ws.Cell(4, c).GetString().Trim().Contains("TBM") || 
                                ws.Cell(5, c).GetString().Trim().Contains("TBM"))
                                break;
                            
                            // Kiá»ƒm tra náº¿u 2 cá»™t liÃªn tiáº¿p trá»‘ng thÃ¬ dá»«ng
                            if (string.IsNullOrWhiteSpace(ws.Cell(5, c + 1).GetString().Trim()))
                                break;

                            continue;
                        }

                        if (subjName.Equals("TBM", StringComparison.OrdinalIgnoreCase))
                            break;

                        // Xá»­ lÃ½ cÃ¡c cá»™t bá»‹ trÃ¹ng tÃªn trong file Excel thá»±c táº¿
                        if (c == 20) subjName = "ÄHQS 1";
                        else if (c == 60) subjName = "ÄHQS 2";
                        else if (c == 27) subjName = "KT Xe 1";
                        else if (c == 42) subjName = "KT Xe 2";
                        else if (c == 44) subjName = "VÃµ 1";
                        else if (c == 57) subjName = "VÃµ 2";

                        // Äá»c sá»‘ tÃ­n chá»‰ á»Ÿ dÃ²ng 1
                        double credits = 1.0;
                        string creditStr = ws.Cell(1, c).GetString().Trim().Replace(',', '.');
                        if (double.TryParse(creditStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedCredits) && parsedCredits > 0)
                        {
                            credits = parsedCredits;
                        }

                        string subjCode = $"TC{c - 5:D2}";

                        // Tá»± Ä‘á»™ng suy luáº­n nhÃ³m mÃ´n (SubjectGroup) cho cÃ¡c mÃ´n thÃ nh pháº§n
                        string group = string.Empty;
                        bool isComponent = false;

                        if (subjName.StartsWith("CNTT", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "CNTT";
                            isComponent = true;
                        }
                        else if (subjName.Contains("ÄLQLBÄ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "ÄLQLBÄ";
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
                        else if (subjName.Contains("LSÄ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "LSÄ";
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
                        else if (subjName.StartsWith("ÄHQS", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "ÄHQS";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("VÃµ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "VÃµ";
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
                        else if (subjName.StartsWith("LÄ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = "Lá»±u Ä‘áº¡n";
                            isComponent = true;
                        }
                        else if (subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase))
                        {
                            group = subjName.Substring(4).Trim();
                            isComponent = true;
                        }

                        // TÃ¬m hoáº·c táº¡o mÃ´n trong CSDL theo mÃ£ TCxx hoáº·c tÃªn
                        var existingSubj = subjectsInDb.FirstOrDefault(s => s.SubjectCode == subjCode || s.SubjectName.Equals(subjName, StringComparison.OrdinalIgnoreCase));
                        if (existingSubj == null)
                        {
                            var newSubj = new CreditSubject
                            {
                                SubjectCode = subjCode,
                                SubjectName = subjName,
                                Credits = credits,
                                AssessmentType = subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase) ? "Kiá»ƒm tra vÃ  thi" : "Kiá»ƒm tra thÆ°á»ng xuyÃªn",
                                SubjectGroup = group,
                                IsComponent = isComponent,
                                Description = $"Nháº­p tá»± Ä‘á»™ng tá»« file Excel ({credits} tÃ­n chá»‰)",
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

                    // 2. Äá»c danh sÃ¡ch há»c viÃªn vÃ  Ä‘iá»ƒm tá»«ng mÃ´n (tá»« dÃ²ng 6 trá»Ÿ Ä‘i)
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

                        // TÃ¬m hoáº·c táº¡o há»c viÃªn
                        var cadet = cadetsInDb.FirstOrDefault(cd => System.Text.RegularExpressions.Regex.Replace(cd.FullName, @"\s+", " ").Equals(fullName, StringComparison.OrdinalIgnoreCase));
                        if (cadet == null)
                        {
                            cadet = new Cadet
                            {
                                CadetCode = $"HV{DateTime.Now:yy}{cadetsInDb.Count + 1:D3}",
                                FullName = fullName,
                                Unit = unit,
                                Rank = "Binh nhÃ¬",
                                Position = "Há»c viÃªn",
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

                        // Äá»c Ä‘iá»ƒm cho tá»«ng mÃ´n
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
                                            ExamSession = "ToÃ n khÃ³a",
                                            ExamDate = DateTime.Today,
                                            Notes = "Nháº­p tá»± Ä‘á»™ng tá»« file chuáº©n TBM",
                                            CreatedAt = DateTime.Now
                                        });
                                    }

                                    importedScoresCount++;
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    return (true, $"ÄÃ£ nháº­p thÃ nh cÃ´ng tá»« file Excel: {colSubjectMap.Count} mÃ´n/thÃ nh pháº§n, {importedCadetsCount} há»c viÃªn má»›i, cáº­p nháº­t {importedScoresCount} Ä‘áº§u Ä‘iá»ƒm!", importedCadetsCount, importedScoresCount);
                }
                catch (Exception ex)
                {
                    return (false, $"Lá»—i khi nháº­p file Excel: {ex.Message}", 0, 0);
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
                        return (false, "File Excel khÃ´ng tá»“n táº¡i trÃªn há»‡ thá»‘ng.", 0, 0, 0);

                    // 1. XÃ³a sáº¡ch toÃ n bá»™ dá»¯ liá»‡u há»c viÃªn, mÃ´n há»c, thÃ nh pháº§n vÃ  Ä‘iá»ƒm sá»‘
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

                    // 2. Náº¡p má»›i toÃ n bá»™ tá»« file Excel
                    using var wb = new XLWorkbook(filePath);
                    var ws = wb.Worksheets.FirstOrDefault();
                    if (ws == null)
                        return (false, "File Excel khÃ´ng chá»©a báº¥t ká»³ sheet nÃ o.", 0, 0, 0);

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

                        if (c == 20) subjName = "ÄHQS 1";
                        else if (c == 60) subjName = "ÄHQS 2";
                        else if (c == 27) subjName = "KT Xe 1";
                        else if (c == 42) subjName = "KT Xe 2";
                        else if (c == 44) subjName = "VÃµ 1";
                        else if (c == 57) subjName = "VÃµ 2";

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
                        else if (subjName.Contains("ÄLQLBÄ", StringComparison.OrdinalIgnoreCase)) { group = "ÄLQLBÄ"; isComponent = true; }
                        else if (subjName.Contains("TH M-L", StringComparison.OrdinalIgnoreCase)) { group = "TH M-L"; isComponent = true; }
                        else if (subjName.Contains("KTCT", StringComparison.OrdinalIgnoreCase)) { group = "KTCT"; isComponent = true; }
                        else if (subjName.Contains("CNXH", StringComparison.OrdinalIgnoreCase)) { group = "CNXH"; isComponent = true; }
                        else if (subjName.Contains("LSÄ", StringComparison.OrdinalIgnoreCase)) { group = "LSÄ"; isComponent = true; }
                        else if (subjName.StartsWith("bBB", StringComparison.OrdinalIgnoreCase)) { group = "bBB"; isComponent = true; }
                        else if (subjName.Contains("VKHD", StringComparison.OrdinalIgnoreCase)) { group = "VKHD"; isComponent = true; }
                        else if (subjName.StartsWith("KT Xe", StringComparison.OrdinalIgnoreCase)) { group = "KT Xe"; isComponent = true; }
                        else if (subjName.StartsWith("ÄHQS", StringComparison.OrdinalIgnoreCase)) { group = "ÄHQS"; isComponent = true; }
                        else if (subjName.StartsWith("VÃµ", StringComparison.OrdinalIgnoreCase)) { group = "VÃµ"; isComponent = true; }
                        else if (subjName.StartsWith("HC", StringComparison.OrdinalIgnoreCase) && subjName.Contains("QS", StringComparison.OrdinalIgnoreCase)) { group = "HCQS"; isComponent = true; }
                        else if (subjName.StartsWith("TT HCM", StringComparison.OrdinalIgnoreCase)) { group = "TT HCM"; isComponent = true; }
                        else if (subjName.StartsWith("LÄ", StringComparison.OrdinalIgnoreCase)) { group = "Lá»±u Ä‘áº¡n"; isComponent = true; }
                        else if (subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase)) { group = subjName.Substring(4).Trim(); isComponent = true; }

                        var newSubj = new CreditSubject
                        {
                            SubjectCode = subjCode,
                            SubjectName = subjName,
                            Credits = credits,
                            AssessmentType = subjName.StartsWith("Thi ", StringComparison.OrdinalIgnoreCase) ? "Kiá»ƒm tra vÃ  thi" : "Kiá»ƒm tra thÆ°á»ng xuyÃªn",
                            SubjectGroup = group,
                            IsComponent = isComponent,
                            Description = $"Nháº­p tá»± Ä‘á»™ng tá»« file Excel ({credits} tÃ­n chá»‰)",
                            CreatedAt = DateTime.Now
                        };

                        _context.CreditSubjects.Add(newSubj);
                        colSubjectMap[c] = newSubj;
                    }

                    await _context.SaveChangesAsync();

                    // Äá»c danh sÃ¡ch há»c viÃªn
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
                            Rank = "Binh nhÃ¬",
                            Position = "Há»c viÃªn",
                            DateOfBirth = new DateTime(2002, 1, 1),
                            CreatedAt = DateTime.Now
                        };

                        _context.Cadets.Add(cadet);
                        addedCadets.Add(cadet);
                        importedCadetsCount++;
                    }

                    await _context.SaveChangesAsync();

                    // Äá»c Ä‘iá»ƒm
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
                                        ExamSession = "ToÃ n khÃ³a",
                                        ExamDate = DateTime.Today,
                                        Notes = "Nháº­p tá»± Ä‘á»™ng tá»« file chuáº©n TBM",
                                        CreatedAt = DateTime.Now
                                    });
                                    importedScoresCount++;
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    // 3. TÃ¡i cáº¥u trÃºc chuáº©n thÃ nh 41 mÃ´n lá»›n vÃ  cÃ¡c Ä‘á»£t kiá»ƒm tra / thi trá»±c thuá»™c
                    await ConsolidateMajorSubjectsAsync();

                    // 4. Äáº£m báº£o toÃ n bá»™ CreditScoreRecords Ä‘á»u cÃ³ ComponentId
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
                    return (true, $"LÃ m sáº¡ch vÃ  náº¡p láº¡i CSDL thÃ nh cÃ´ng: {importedCadetsCount} há»c viÃªn, {totalMajorSubjects} mÃ´n lá»›n, {importedScoresCount} Ä‘iá»ƒm sá»‘!", importedCadetsCount, totalMajorSubjects, importedScoresCount);
                }
                catch (Exception ex)
                {
                    return (false, $"Lá»—i khi lÃ m sáº¡ch vÃ  náº¡p láº¡i: {ex.Message}", 0, 0, 0);
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
                    row.MissingComponentsDisplay = $"ChÆ°a thi: {string.Join(", ", missingNames)}";
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
                        r.MissingComponentsDisplay = missing ? $"ChÆ°a thi: {string.Join(", ", mNames)}" : string.Empty;
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
                    return (false, "KhÃ´ng tÃ¬m tháº¥y mÃ´n há»c.");

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
                                    Notes = "Nháº­p tá»« báº£ng Ä‘iá»ƒm mÃ´n há»c"
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
                return (true, $"ÄÃ£ lÆ°u thÃ nh cÃ´ng {savedScoresCount} Ä‘áº§u Ä‘iá»ƒm cho mÃ´n '{subject.SubjectName}'!");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i lÆ°u báº£ng Ä‘iá»ƒm: {ex.Message}");
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

                    // XÃ³a cÃ¡c components cÅ© cá»§a mÃ´n chÃ­nh náº¿u cáº§n tÃ¡i cáº¥u trÃºc
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

                    // LiÃªn káº¿t láº¡i Ä‘iá»ƒm sá»‘ sang mÃ´n chÃ­nh vÃ  ComponentId tÆ°Æ¡ng á»©ng
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

                // Äá»‘i vá»›i cÃ¡c mÃ´n Ä‘á»™c láº­p (IsComponent == false), Ä‘áº£m báº£o cÃ³ Ã­t nháº¥t 1 component
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
                    return (false, "KhÃ´ng tÃ¬m tháº¥y mÃ´n há»c.");

                var cadet = await _context.Cadets.FindAsync(cadetId);
                if (cadet == null)
                    return (false, "KhÃ´ng tÃ¬m tháº¥y há»c viÃªn.");

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
                                Notes = "Nháº­p Ä‘iá»ƒm tá»« há»“ sÆ¡ há»c viÃªn"
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
                return (true, $"LÆ°u Ä‘iá»ƒm há»c viÃªn {cadet.FullName} thÃ nh cÃ´ng ({savedCount} cá»™t Ä‘iá»ƒm).");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i lÆ°u Ä‘iá»ƒm há»c viÃªn: {ex.Message}");
            }
        }
    }
}

