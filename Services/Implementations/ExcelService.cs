using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using System.Text.RegularExpressions;

namespace QL_HocVien.Services.Implementations
{
    public class ExcelService : IExcelService
    {
        private readonly AppDbContext _context;
        private readonly ICadetRepository _cadetRepository;
        private readonly IClassRepository _classRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly IPhysicalExamRepository _examRepository;
        private readonly IEvaluationService _evaluationService;
        private readonly IOfficerRepository _officerRepository;
        private readonly IRankRepository _rankRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IMajorRepository _majorRepository;
        private readonly IExcelSecurityValidator _excelValidator;

        public ExcelService(
            AppDbContext context,
            ICadetRepository cadetRepository,
            IClassRepository classRepository,
            ISubjectRepository subjectRepository,
            IPhysicalExamRepository examRepository,
            IEvaluationService evaluationService,
            IOfficerRepository officerRepository,
            IRankRepository rankRepository,
            IPositionRepository positionRepository,
            IUnitRepository unitRepository,
            IMajorRepository majorRepository,
            IExcelSecurityValidator? excelValidator = null)
        {
            _context = context;
            _cadetRepository = cadetRepository;
            _classRepository = classRepository;
            _subjectRepository = subjectRepository;
            _examRepository = examRepository;
            _evaluationService = evaluationService;
            _officerRepository = officerRepository;
            _rankRepository = rankRepository;
            _positionRepository = positionRepository;
            _unitRepository = unitRepository;
            _majorRepository = majorRepository;
            _excelValidator = excelValidator ?? new ExcelSecurityValidator();
        }

        private async Task<(bool IsValid, string Message)> ValidateExcelSecurityAsync(string filePath)
        {
            var result = await _excelValidator.ValidateExcelFileAsync(filePath);
            if (!result.IsValid)
            {
                return (false, $"[Báº¢O Máº¬T] Tá»« chá»‘i táº­p tin '{result.FileName}': {result.Message}");
            }
            return (true, string.Empty);
        }

        private static string CleanCellText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var sanitized = text.Replace("\r", " ").Replace("\n", " ");
            return Regex.Replace(sanitized, @"\s+", " ").Trim();
        }

        #region 1. XUáº¤T & NHáº¬P Há»ŒC VIÃŠN
        public async Task<(bool Success, string Message)> ExportCadetsToExcelAsync(IEnumerable<Cadet> cadets, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Danh sÃ¡ch há»c viÃªn");

                // TiÃªu Ä‘á»
                ws.Cell(1, 1).Value = "DANH SÃCH Há»ŒC VIÃŠN - Há»ŒC VIá»†N QUÃ‚N Sá»°";
                ws.Range(1, 1, 1, 12).Merge().Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.FromHtml("#0F766E"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(2, 1).Value = $"NgÃ y xuáº¥t: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range(2, 1, 2, 12).Merge().Style
                    .Font.SetItalic(true)
                    .Font.SetFontSize(10)
                    .Font.SetFontColor(XLColor.FromHtml("#64748B"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Header
                string[] headers = { "STT", "MÃ£ há»c viÃªn", "Há» vÃ  tÃªn", "Cáº¥p báº­c", "Chá»©c vá»¥", "ÄÆ¡n vá»‹", "Lá»›p", "Sá»‘ Ä‘iá»‡n thoáº¡i", "Email", "NgÃ y sinh", "Tuá»•i", "Giá»›i tÃ­nh" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style
                        .Font.SetBold(true)
                        .Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#0F766E"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetOutsideBorderColor(XLColor.FromHtml("#0D5E56"));
                }
                ws.Row(4).Height = 28;

                int row = 5;
                int stt = 1;
                foreach (var c in cadets)
                {
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = c.CadetCode;
                    ws.Cell(row, 3).Value = c.FullName;
                    ws.Cell(row, 4).Value = c.Rank;
                    ws.Cell(row, 5).Value = c.Position;
                    ws.Cell(row, 6).Value = c.Unit;
                    ws.Cell(row, 7).Value = c.ClassName ?? (c.MilitaryClass?.ClassName ?? "");
                    ws.Cell(row, 8).Value = c.PhoneNumber;
                    ws.Cell(row, 9).Value = c.Email;
                    ws.Cell(row, 10).Value = c.DateOfBirth.HasValue ? c.DateOfBirth.Value.ToString("dd/MM/yyyy") : "";
                    ws.Cell(row, 11).Value = c.Age;
                    ws.Cell(row, 12).Value = c.Gender;

                    var dataRow = ws.Range(row, 1, row, 12);
                    dataRow.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                  .Border.SetOutsideBorderColor(XLColor.FromHtml("#E2E8F0"))
                                  .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                                  .Border.SetInsideBorderColor(XLColor.FromHtml("#E2E8F0"));

                    if (row % 2 == 0)
                    {
                        dataRow.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
                    }

                    ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws.Cell(row, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws.Cell(row, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws.Cell(row, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws.Cell(row, 11).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws.Cell(row, 12).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
                return (true, $"ÄÃ£ xuáº¥t thÃ nh cÃ´ng {stt - 1} há»c viÃªn ra tá»‡p Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i xuáº¥t tá»‡p Excel: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<Cadet> Cadets)> ImportCadetsFromExcelAsync(string filePath)
        {
            var importedList = new List<Cadet>();
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tá»‡p Excel khÃ´ng tá»“n táº¡i.", importedList);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, importedList);

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("há»c viÃªn") || w.Name.Contains("Cadet"))
                         ?? workbook.Worksheets.FirstOrDefault();

                if (ws == null)
                    return (false, "KhÃ´ng tÃ¬m tháº¥y sheet chá»©a dá»¯ liá»‡u há»c viÃªn.", importedList);

                // TÃ¬m hÃ ng header (cÃ³ chá»©a 'MÃ£' hoáº·c 'Há» vÃ  tÃªn')
                int headerRow = 1;
                for (int r = 1; r <= 15; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => CleanCellText(c.GetString())));
                    if (textRow.Contains("Há» vÃ  tÃªn") || textRow.Contains("MÃ£ há»c viÃªn") || textRow.Contains("Há» tÃªn") || textRow.Contains("CadetCode"))
                    {
                        headerRow = r;
                        break;
                    }
                }

                // Dynamic Header-Based Mapping: Tá»± Ä‘á»™ng phÃ¡t hiá»‡n chá»‰ sá»‘ cá»™t theo tiÃªu Ä‘á» Ã´
                int colCode = -1, colFullName = -1, colRank = -1, colPosition = -1, colUnit = -1;
                int colClassName = -1, colPhone = -1, colEmail = -1, colDob = -1, colAge = -1, colGender = -1;

                foreach (var cell in ws.Row(headerRow).CellsUsed())
                {
                    var title = CleanCellText(cell.GetString()).ToLowerInvariant();
                    int c = cell.Address.ColumnNumber;

                    if (title.Contains("mÃ£") || title.Contains("cadetcode") || title.Contains("sá»‘ hiá»‡u") || title.Contains("shsv") || title.Contains("ms"))
                    {
                        if (colCode == -1) colCode = c;
                    }
                    else if (title.Contains("há»") || title.Contains("tÃªn") || title.Contains("fullname"))
                    {
                        if (colFullName == -1) colFullName = c;
                    }
                    else if (title.Contains("cáº¥p báº­c") || title.Contains("quÃ¢n hÃ m") || title.Equals("rank"))
                    {
                        if (colRank == -1) colRank = c;
                    }
                    else if (title.Contains("chá»©c vá»¥") || title.Contains("chá»©c danh") || title.Equals("position"))
                    {
                        if (colPosition == -1) colPosition = c;
                    }
                    else if (title.Contains("Ä‘Æ¡n vá»‹") || title.Contains("Ä‘áº¡i Ä‘á»™i") || title.Contains("trung Ä‘á»™i") || title.Contains("tiá»ƒu Ä‘oÃ n") || title.Equals("unit"))
                    {
                        if (colUnit == -1) colUnit = c;
                    }
                    else if (title.Contains("lá»›p") || title.Equals("class") || title.Contains("classname"))
                    {
                        if (colClassName == -1) colClassName = c;
                    }
                    else if (title.Contains("thoáº¡i") || title.Contains("sÄ‘t") || title.Contains("phone") || title.Contains("tel"))
                    {
                        if (colPhone == -1) colPhone = c;
                    }
                    else if (title.Contains("mail"))
                    {
                        if (colEmail == -1) colEmail = c;
                    }
                    else if (title.Contains("sinh") || title.Contains("dob") || title.Contains("birth"))
                    {
                        if (colDob == -1) colDob = c;
                    }
                    else if (title.Contains("tuá»•i") || title.Equals("age"))
                    {
                        if (colAge == -1) colAge = c;
                    }
                    else if (title.Contains("giá»›i tÃ­nh") || title.Contains("nam/ná»¯") || title.Equals("gender"))
                    {
                        if (colGender == -1) colGender = c;
                    }
                }

                // Fallback náº¿u khÃ´ng xÃ¡c Ä‘á»‹nh Ä‘Æ°á»£c vá»‹ trÃ­ há» tÃªn
                if (colFullName == -1)
                {
                    var firstTitle = CleanCellText(ws.Cell(headerRow, 1).GetString()).ToLowerInvariant();
                    bool firstIsStt = firstTitle.Contains("stt") || firstTitle.Contains("tt") || firstTitle.Contains("no");
                    colCode = firstIsStt ? 2 : 1;
                    colFullName = firstIsStt ? 3 : 2;
                    if (colRank == -1) colRank = firstIsStt ? 4 : 3;
                    if (colPosition == -1) colPosition = firstIsStt ? 5 : 4;
                    if (colUnit == -1) colUnit = firstIsStt ? 6 : 5;
                    if (colClassName == -1) colClassName = firstIsStt ? 7 : 6;
                    if (colPhone == -1) colPhone = firstIsStt ? 8 : 7;
                    if (colEmail == -1) colEmail = firstIsStt ? 9 : 8;
                    if (colDob == -1) colDob = firstIsStt ? 10 : 9;
                    if (colAge == -1) colAge = firstIsStt ? 11 : 10;
                    if (colGender == -1) colGender = firstIsStt ? 12 : 11;
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
                int addedCount = 0;
                int updatedCount = 0;
                var allClasses = (await _classRepository.GetAllAsync()).ToList();

                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    // Äá»c cÃ¡c cá»™t theo mapping Ä‘á»™ng vÃ  lÃ m sáº¡ch khoáº£ng tráº¯ng/xuá»‘ng dÃ²ng
                    string code = colCode > 0 ? CleanCellText(row.Cell(colCode).GetString()) : string.Empty;
                    string fullName = colFullName > 0 ? CleanCellText(row.Cell(colFullName).GetString()) : string.Empty;

                    // Náº¿u há» tÃªn rá»—ng thÃ¬ bá» qua
                    if (string.IsNullOrWhiteSpace(fullName)) continue;
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        code = $"HV-{DateTime.Today.Year}-{r:D3}";
                    }

                    string rank = colRank > 0 ? CleanCellText(row.Cell(colRank).GetString()) : string.Empty;
                    string pos = colPosition > 0 ? CleanCellText(row.Cell(colPosition).GetString()) : string.Empty;
                    string unit = colUnit > 0 ? CleanCellText(row.Cell(colUnit).GetString()) : string.Empty;
                    string className = colClassName > 0 ? CleanCellText(row.Cell(colClassName).GetString()) : string.Empty;
                    string phone = colPhone > 0 ? CleanCellText(row.Cell(colPhone).GetString()) : string.Empty;
                    string email = colEmail > 0 ? CleanCellText(row.Cell(colEmail).GetString()) : string.Empty;
                    string dobStr = colDob > 0 ? CleanCellText(row.Cell(colDob).GetString()) : string.Empty;
                    DateTime? dob = null;
                    if (DateTime.TryParse(dobStr, out var d)) dob = d;
                    
                    int age = 0;
                    if (colAge > 0)
                    {
                        int.TryParse(CleanCellText(row.Cell(colAge).GetString()), out age);
                    }
                    string gender = colGender > 0 ? CleanCellText(row.Cell(colGender).GetString()) : "Nam";
                    if (string.IsNullOrWhiteSpace(gender)) gender = "Nam";

                    var matchedClass = allClasses.FirstOrDefault(c => 
                        c.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase) || 
                        c.ClassCode.Equals(className, StringComparison.OrdinalIgnoreCase));

                    var existing = await _cadetRepository.GetByCodeAsync(code);
                    if (existing != null)
                    {
                        existing.FullName = fullName;
                        if (!string.IsNullOrWhiteSpace(rank)) existing.Rank = rank;
                        if (!string.IsNullOrWhiteSpace(pos)) existing.Position = pos;
                        if (!string.IsNullOrWhiteSpace(unit)) existing.Unit = unit;
                        if (matchedClass != null)
                        {
                            existing.ClassId = matchedClass.Id;
                            existing.ClassName = matchedClass.ClassName;
                        }
                        else if (!string.IsNullOrWhiteSpace(className))
                        {
                            existing.ClassName = className;
                        }
                        if (!string.IsNullOrWhiteSpace(phone)) existing.PhoneNumber = phone;
                        if (!string.IsNullOrWhiteSpace(email)) existing.Email = email;
                        if (dob.HasValue) existing.DateOfBirth = dob;
                        if (age > 0) existing.Age = age;
                        existing.Gender = gender;
                        _cadetRepository.Update(existing);
                        updatedCount++;
                        importedList.Add(existing);
                    }
                    else
                    {
                        var newCadet = new Cadet
                        {
                            CadetCode = code,
                            FullName = fullName,
                            Rank = !string.IsNullOrWhiteSpace(rank) ? rank : "Binh nhÃ¬",
                            Position = !string.IsNullOrWhiteSpace(pos) ? pos : "Há»c viÃªn",
                            Unit = !string.IsNullOrWhiteSpace(unit) ? unit : "Äáº¡i Ä‘á»™i 1",
                            ClassId = matchedClass?.Id,
                            ClassName = matchedClass?.ClassName ?? (!string.IsNullOrWhiteSpace(className) ? className : "K26A"),
                            PhoneNumber = !string.IsNullOrWhiteSpace(phone) ? phone : $"09{new Random().Next(10000000, 99999999)}",
                            Email = !string.IsNullOrWhiteSpace(email) ? email : $"{code.ToLower().Replace("-", "").Replace(" ", "")}@hocvien.edu.vn",
                            DateOfBirth = dob,
                            Age = age > 0 ? age : 21,
                            Gender = gender,
                            CreatedAt = DateTime.Now
                        };
                        await _cadetRepository.AddAsync(newCadet);
                        addedCount++;
                        importedList.Add(newCadet);
                    }
                }

                await _cadetRepository.SaveChangesAsync();
                return (true, $"Nháº­p dá»¯ liá»‡u thÃ nh cÃ´ng: ThÃªm má»›i {addedCount} há»c viÃªn, Cáº­p nháº­t {updatedCount} há»c viÃªn.", importedList);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i xá»­ lÃ½ tá»‡p Excel: {ex.Message}", importedList);
            }
        }
        #endregion

        #region 2. XUáº¤T & NHáº¬P MÃ”N Há»ŒC
        public async Task<(bool Success, string Message)> ExportSubjectsToExcelAsync(IEnumerable<Subject> subjects, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Trang mÃ´n há»c");

                ws.Cell("A1").Value = "DANH Má»¤C MÃ”N Há»ŒC & TIÃŠU CHUáº¨N RÃˆN LUYá»†N THá»‚ Lá»°C (TT32)";
                ws.Range("A1:J1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                string[] headers = { "STT", "MÃ£ mÃ´n", "TÃªn mÃ´n há»c", "NhÃ³m tá»‘ cháº¥t", "ÄÆ¡n vá»‹ tÃ­nh", "Chuáº©n Giá»i", "Chuáº©n KhÃ¡", "Chuáº©n Äáº¡t", "CÃ ng cao cÃ ng tá»‘t", "MÃ´ táº£" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(3, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                int row = 4;
                int stt = 1;
                foreach (var s in subjects)
                {
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = s.SubjectCode;
                    ws.Cell(row, 3).Value = s.SubjectName;
                    ws.Cell(row, 4).Value = s.Category;
                    ws.Cell(row, 5).Value = s.Unit;
                    ws.Cell(row, 6).Value = s.ExcellentThreshold;
                    ws.Cell(row, 7).Value = s.GoodThreshold;
                    ws.Cell(row, 8).Value = s.PassThreshold;
                    ws.Cell(row, 9).Value = s.IsHigherBetter ? "CÃ³" : "KhÃ´ng";
                    ws.Cell(row, 10).Value = s.Description;

                    ws.Range(row, 1, row, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                   .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);

                return (true, $"Xuáº¥t thÃ nh cÃ´ng {stt - 1} mÃ´n há»c ra Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i xuáº¥t mÃ´n há»c: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<Subject> Subjects)> ImportSubjectsFromExcelAsync(string filePath)
        {
            var list = new List<Subject>();
            try
            {
                if (!File.Exists(filePath)) return (false, "Tá»‡p khÃ´ng tá»“n táº¡i.", list);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, list);

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("mÃ´n") || w.Name.Contains("Subject"))
                         ?? workbook.Worksheets.FirstOrDefault();

                if (ws == null) return (false, "KhÃ´ng tÃ¬m tháº¥y sheet mÃ´n há»c.", list);

                int headerRow = 1;
                for (int r = 1; r <= 15; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => CleanCellText(c.GetString())));
                    if (textRow.Contains("MÃ£ mÃ´n") || textRow.Contains("TÃªn mÃ´n") || textRow.Contains("SubjectCode"))
                    {
                        headerRow = r;
                        break;
                    }
                }

                int colCode = -1, colName = -1, colCat = -1, colUnit = -1;
                int colExc = -1, colGood = -1, colPass = -1, colHigher = -1, colDesc = -1;

                foreach (var cell in ws.Row(headerRow).CellsUsed())
                {
                    var title = CleanCellText(cell.GetString()).ToLowerInvariant();
                    int c = cell.Address.ColumnNumber;

                    if (title.Contains("mÃ£ mÃ´n") || title.Contains("subjectcode"))
                    {
                        if (colCode == -1) colCode = c;
                    }
                    else if (title.Contains("tÃªn mÃ´n") || title.Contains("subjectname") || (title.Contains("mÃ´n") && !title.Contains("mÃ£")))
                    {
                        if (colName == -1) colName = c;
                    }
                    else if (title.Contains("loáº¡i") || title.Contains("nhÃ³m") || title.Contains("danh má»¥c") || title.Contains("category"))
                    {
                        if (colCat == -1) colCat = c;
                    }
                    else if (title.Contains("Ä‘Æ¡n vá»‹ tÃ­nh") || title.Contains("Ä‘vt") || title.Equals("Ä‘Æ¡n vá»‹") || title.Equals("unit"))
                    {
                        if (colUnit == -1) colUnit = c;
                    }
                    else if (title.Contains("giá»i") || title.Contains("excellent"))
                    {
                        if (colExc == -1) colExc = c;
                    }
                    else if (title.Contains("khÃ¡") || title.Contains("good"))
                    {
                        if (colGood == -1) colGood = c;
                    }
                    else if (title.Contains("Ä‘áº¡t") || title.Contains("pass"))
                    {
                        if (colPass == -1) colPass = c;
                    }
                    else if (title.Contains("cÃ ng cao") || title.Contains("higher"))
                    {
                        if (colHigher == -1) colHigher = c;
                    }
                    else if (title.Contains("mÃ´ táº£") || title.Contains("ghi chÃº") || title.Contains("desc"))
                    {
                        if (colDesc == -1) colDesc = c;
                    }
                }

                if (colCode == -1 && colName == -1)
                {
                    colCode = 2;
                    colName = 3;
                    colCat = 4;
                    colUnit = 5;
                    colExc = 6;
                    colGood = 7;
                    colPass = 8;
                    colHigher = 9;
                    colDesc = 10;
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
                int added = 0;
                int updated = 0;

                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string code = colCode > 0 ? CleanCellText(row.Cell(colCode).GetString()) : string.Empty;
                    string name = colName > 0 ? CleanCellText(row.Cell(colName).GetString()) : string.Empty;
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

                    string cat = colCat > 0 ? CleanCellText(row.Cell(colCat).GetString()) : "TiÃªu chuáº©n rÃ¨n luyá»‡n";
                    string unit = colUnit > 0 ? CleanCellText(row.Cell(colUnit).GetString()) : "Láº§n";
                    double exc = 0, good = 0, pass = 0;
                    if (colExc > 0) double.TryParse(CleanCellText(row.Cell(colExc).GetString()), out exc);
                    if (colGood > 0) double.TryParse(CleanCellText(row.Cell(colGood).GetString()), out good);
                    if (colPass > 0) double.TryParse(CleanCellText(row.Cell(colPass).GetString()), out pass);

                    string higher = colHigher > 0 ? CleanCellText(row.Cell(colHigher).GetString()).ToLowerInvariant() : "cÃ³";
                    bool isHigher = higher == "cÃ³" || higher == "yes" || higher == "true" || higher == "1";
                    string desc = colDesc > 0 ? CleanCellText(row.Cell(colDesc).GetString()) : string.Empty;

                    var existing = await _subjectRepository.GetByCodeAsync(code);
                    if (existing != null)
                    {
                        existing.SubjectName = name;
                        if (!string.IsNullOrWhiteSpace(cat)) existing.Category = cat;
                        if (!string.IsNullOrWhiteSpace(unit)) existing.Unit = unit;
                        existing.ExcellentThreshold = exc;
                        existing.GoodThreshold = good;
                        existing.PassThreshold = pass;
                        existing.IsHigherBetter = isHigher;
                        existing.Description = desc;
                        _subjectRepository.Update(existing);
                        updated++;
                        list.Add(existing);
                    }
                    else
                    {
                        var newSubject = new Subject
                        {
                            SubjectCode = code.ToUpper(),
                            SubjectName = name,
                            Category = !string.IsNullOrWhiteSpace(cat) ? cat : "Sá»©c máº¡nh",
                            Unit = !string.IsNullOrWhiteSpace(unit) ? unit : "láº§n",
                            ExcellentThreshold = exc,
                            GoodThreshold = good,
                            PassThreshold = pass,
                            IsHigherBetter = isHigher,
                            Description = desc
                        };
                        await _subjectRepository.AddAsync(newSubject);
                        added++;
                        list.Add(newSubject);
                    }
                }

                await _subjectRepository.SaveChangesAsync();
                return (true, $"Nháº­p mÃ´n há»c thÃ nh cÃ´ng: ThÃªm má»›i {added} mÃ´n, Cáº­p nháº­t {updated} mÃ´n.", list);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i nháº­p mÃ´n há»c: {ex.Message}", list);
            }
        }
        #endregion

        #region 3. XUáº¤T & NHáº¬P KIá»‚M TRA THá»‚ Lá»°C
        public async Task<(bool Success, string Message)> ExportExamRecordsToExcelAsync(IEnumerable<PhysicalExamRecord> records, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Kiá»ƒm tra thá»ƒ lá»±c");

                ws.Cell("A1").Value = "Báº¢NG Tá»”NG Há»¢P Káº¾T QUáº¢ KIá»‚M TRA THá»‚ Lá»°C QUÃ‚N Sá»°";
                ws.Range("A1:K1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                string[] headers = { "STT", "MÃ£ há»c viÃªn", "Há» vÃ  tÃªn", "ÄÆ¡n vá»‹", "Lá»›p", "MÃ£ mÃ´n", "TÃªn mÃ´n kiá»ƒm tra", "ThÃ nh tÃ­ch", "Xáº¿p loáº¡i", "Äá»£t kiá»ƒm tra", "NgÃ y kiá»ƒm tra" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(3, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#15803D"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                int row = 4;
                int stt = 1;
                foreach (var r in records)
                {
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = r.Cadet?.CadetCode ?? "";
                    ws.Cell(row, 3).Value = r.Cadet?.FullName ?? "";
                    ws.Cell(row, 4).Value = r.Cadet?.Unit ?? "";
                    ws.Cell(row, 5).Value = r.Cadet?.ClassName ?? "";
                    ws.Cell(row, 6).Value = r.Subject?.SubjectCode ?? "";
                    ws.Cell(row, 7).Value = r.Subject?.SubjectName ?? "";
                    ws.Cell(row, 8).Value = r.ScoreValue;
                    ws.Cell(row, 9).Value = r.Grade;
                    ws.Cell(row, 10).Value = r.ExamSession;
                    ws.Cell(row, 11).Value = r.ExamDate.ToString("dd/MM/yyyy");

                    ws.Range(row, 1, row, 11).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                   .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);

                return (true, $"Xuáº¥t thÃ nh cÃ´ng {stt - 1} lÆ°á»£t kiá»ƒm tra thá»ƒ lá»±c ra Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i xuáº¥t káº¿t quáº£ kiá»ƒm tra: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<PhysicalExamRecord> Records)> ImportExamRecordsFromExcelAsync(string filePath)
        {
            var list = new List<PhysicalExamRecord>();
            try
            {
                if (!File.Exists(filePath)) return (false, "Tá»‡p khÃ´ng tá»“n táº¡i.", list);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, list);

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("thá»ƒ lá»±c") || w.Name.Contains("Exam"))
                         ?? workbook.Worksheets.FirstOrDefault();

                if (ws == null) return (false, "KhÃ´ng tÃ¬m tháº¥y sheet kiá»ƒm tra thá»ƒ lá»±c.", list);

                int headerRow = 1;
                for (int r = 1; r <= 15; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => CleanCellText(c.GetString())));
                    if (textRow.Contains("MÃ£ há»c viÃªn") || textRow.Contains("MÃ£ mÃ´n") || textRow.Contains("ThÃ nh tÃ­ch") || textRow.Contains("CadetCode"))
                    {
                        headerRow = r;
                        break;
                    }
                }

                int colCadetCode = -1, colSubjectCode = -1, colScore = -1, colSession = -1, colDate = -1;

                foreach (var cell in ws.Row(headerRow).CellsUsed())
                {
                    var title = CleanCellText(cell.GetString()).ToLowerInvariant();
                    int c = cell.Address.ColumnNumber;

                    if (title.Contains("mÃ£ há»c viÃªn") || title.Contains("cadetcode") || title.Contains("mÃ£ hv") || (title.Contains("mÃ£") && !title.Contains("mÃ´n")))
                    {
                        if (colCadetCode == -1) colCadetCode = c;
                    }
                    else if (title.Contains("mÃ£ mÃ´n") || title.Contains("subjectcode") || (title.Contains("mÃ´n") && title.Contains("mÃ£")))
                    {
                        if (colSubjectCode == -1) colSubjectCode = c;
                    }
                    else if (title.Contains("thÃ nh tÃ­ch") || title.Contains("Ä‘iá»ƒm") || title.Contains("káº¿t quáº£") || title.Contains("score"))
                    {
                        if (colScore == -1) colScore = c;
                    }
                    else if (title.Contains("Ä‘á»£t") || title.Contains("session") || title.Contains("ká»³ kiá»ƒm tra"))
                    {
                        if (colSession == -1) colSession = c;
                    }
                    else if (title.Contains("ngÃ y") || title.Contains("date") || title.Contains("thá»i gian"))
                    {
                        if (colDate == -1) colDate = c;
                    }
                }

                if (colCadetCode == -1) colCadetCode = 2;
                if (colSubjectCode == -1) colSubjectCode = 6;
                if (colScore == -1) colScore = 8;
                if (colSession == -1) colSession = 10;
                if (colDate == -1) colDate = 11;

                var allCadets = (await _cadetRepository.GetAllAsync()).ToDictionary(c => CleanCellText(c.CadetCode).ToLowerInvariant());
                var allSubjects = (await _subjectRepository.GetAllAsync()).ToDictionary(s => CleanCellText(s.SubjectCode).ToLowerInvariant());

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
                int added = 0;

                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string cadetCode = colCadetCode > 0 ? CleanCellText(row.Cell(colCadetCode).GetString()).ToLowerInvariant() : string.Empty;
                    string subjectCode = colSubjectCode > 0 ? CleanCellText(row.Cell(colSubjectCode).GetString()).ToLowerInvariant() : string.Empty;

                    if (string.IsNullOrWhiteSpace(cadetCode) || string.IsNullOrWhiteSpace(subjectCode)) continue;
                    if (!allCadets.TryGetValue(cadetCode, out var cadet)) continue;
                    if (!allSubjects.TryGetValue(subjectCode, out var subject)) continue;

                    double score = 0;
                    if (colScore > 0)
                    {
                        double.TryParse(CleanCellText(row.Cell(colScore).GetString()), out score);
                    }
                    string session = colSession > 0 ? CleanCellText(row.Cell(colSession).GetString()) : "Kiá»ƒm tra Ä‘á»‹nh ká»³";
                    string dateStr = colDate > 0 ? CleanCellText(row.Cell(colDate).GetString()) : string.Empty;
                    DateTime examDate = DateTime.Today;
                    if (DateTime.TryParse(dateStr, out var d)) examDate = d;

                    string grade = _evaluationService.EvaluateGrade(subject, score);

                    var record = new PhysicalExamRecord
                    {
                        CadetId = cadet.Id,
                        SubjectId = subject.Id,
                        ScoreValue = score,
                        Grade = grade,
                        ExamSession = !string.IsNullOrWhiteSpace(session) ? session : "Kiá»ƒm tra Ä‘á»‹nh ká»³",
                        ExamDate = examDate,
                        CreatedAt = DateTime.Now
                    };

                    await _examRepository.AddAsync(record);
                    added++;
                    list.Add(record);
                }

                await _examRepository.SaveChangesAsync();
                return (true, $"Nháº­p thÃ nh cÃ´ng {added} lÆ°á»£t kiá»ƒm tra thá»ƒ lá»±c tá»« Excel.", list);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i nháº­p kiá»ƒm tra thá»ƒ lá»±c: {ex.Message}", list);
            }
        }
        #endregion

        #region 4. XUáº¤T & NHáº¬P Lá»šP Há»ŒC
        public async Task<(bool Success, string Message)> ExportClassesToExcelAsync(IEnumerable<MilitaryClass> classes, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Trang lá»›p há»c");

                ws.Cell("A1").Value = "DANH SÃCH Lá»šP Há»ŒC QUÃ‚N Äá»˜I";
                ws.Range("A1:I1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Thá»i Ä‘iá»ƒm xuáº¥t dá»¯ liá»‡u: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                ws.Range("A2:I2").Merge().Style.Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                string[] headers = { "STT", "MÃ£ lá»›p", "TÃªn lá»›p", "ÄÆ¡n vá»‹ quáº£n lÃ½", "ChuyÃªn ngÃ nh", "CÃ¡n bá»™ quáº£n lÃ½", "KhÃ³a há»c", "QuÃ¢n sá»‘", "MÃ´ táº£" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                int row = 5;
                int stt = 1;
                foreach (var c in classes)
                {
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = c.ClassCode;
                    ws.Cell(row, 3).Value = c.ClassName;
                    ws.Cell(row, 4).Value = c.Unit;
                    ws.Cell(row, 5).Value = c.Major;
                    ws.Cell(row, 6).Value = c.OfficerInCharge;
                    ws.Cell(row, 7).Value = c.AcademicYear;
                    ws.Cell(row, 8).Value = c.Cadets?.Count ?? 0;
                    ws.Cell(row, 9).Value = c.Description;

                    ws.Range(row, 1, row, 9).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                   .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);

                return (true, $"Xuáº¥t thÃ nh cÃ´ng {stt - 1} lá»›p há»c ra file Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xuáº¥t file Excel: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<MilitaryClass> Classes)> ImportClassesFromExcelAsync(string filePath)
        {
            var importedList = new List<MilitaryClass>();
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tá»‡p khÃ´ng tá»“n táº¡i.", importedList);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, importedList);

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheet("Trang lá»›p há»c") 
                    ?? workbook.Worksheets.FirstOrDefault(w => w.Name.ToLower().Contains("lá»›p") || w.Name.ToLower().Contains("class")) 
                    ?? workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                    return (false, "KhÃ´ng tÃ¬m tháº¥y trang tÃ­nh phÃ¹ há»£p trong tá»‡p Excel.", importedList);

                int headerRowIndex = 1;
                for (int r = 1; r <= 10; r++)
                {
                    for (int c = 1; c <= 10; c++)
                    {
                        var val = ws.Cell(r, c).GetString().Trim().ToLower();
                        if (val.Contains("mÃ£ lá»›p") || val == "classcode" || val == "mÃ£ lá»›p há»c")
                        {
                            headerRowIndex = r;
                            break;
                        }
                    }
                    if (headerRowIndex > 1) break;
                }

                int colCode = 2, colName = 3, colUnit = 4, colMajor = 5, colOfficer = 6, colYear = 7, colDesc = 9;
                for (int c = 1; c <= 15; c++)
                {
                    var title = ws.Cell(headerRowIndex, c).GetString().Trim().ToLower();
                    if (title.Contains("mÃ£ lá»›p")) colCode = c;
                    else if (title.Contains("tÃªn lá»›p")) colName = c;
                    else if (title.Contains("Ä‘Æ¡n vá»‹")) colUnit = c;
                    else if (title.Contains("chuyÃªn ngÃ nh")) colMajor = c;
                    else if (title.Contains("cÃ¡n bá»™") || title.Contains("quáº£n lÃ½") || title.Contains("chá»§ nhiá»‡m")) colOfficer = c;
                    else if (title.Contains("khÃ³a") || title.Contains("niÃªn khÃ³a")) colYear = c;
                    else if (title.Contains("mÃ´ táº£") || title.Contains("ghi chÃº")) colDesc = c;
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                int addedCount = 0;
                int updatedCount = 0;

                for (int r = headerRowIndex + 1; r <= lastRow; r++)
                {
                    var code = ws.Cell(r, colCode).GetString().Trim().ToUpper();
                    if (string.IsNullOrWhiteSpace(code)) continue;

                    var name = ws.Cell(r, colName).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = code;
                    }

                    var unit = ws.Cell(r, colUnit).GetString().Trim();
                    var major = ws.Cell(r, colMajor).GetString().Trim();
                    var officer = ws.Cell(r, colOfficer).GetString().Trim();
                    var year = ws.Cell(r, colYear).GetString().Trim();
                    var desc = ws.Cell(r, colDesc).GetString().Trim();

                    var existing = await _classRepository.GetByCodeAsync(code);
                    if (existing != null)
                    {
                        existing.ClassName = name;
                        if (!string.IsNullOrWhiteSpace(unit)) existing.Unit = unit;
                        if (!string.IsNullOrWhiteSpace(major)) existing.Major = major;
                        if (!string.IsNullOrWhiteSpace(officer)) existing.OfficerInCharge = officer;
                        if (!string.IsNullOrWhiteSpace(year)) existing.AcademicYear = year;
                        if (!string.IsNullOrWhiteSpace(desc)) existing.Description = desc;

                        _classRepository.Update(existing);
                        updatedCount++;
                        importedList.Add(existing);
                    }
                    else
                    {
                        var newClass = new MilitaryClass
                        {
                            ClassCode = code,
                            ClassName = name,
                            Unit = string.IsNullOrWhiteSpace(unit) ? "Äáº¡i Ä‘á»™i 1" : unit,
                            Major = string.IsNullOrWhiteSpace(major) ? "Chá»‰ huy Tham mÆ°u" : major,
                            OfficerInCharge = officer,
                            AcademicYear = string.IsNullOrWhiteSpace(year) ? "2023 - 2027" : year,
                            Description = desc,
                            CreatedAt = DateTime.Now
                        };

                        await _classRepository.AddAsync(newClass);
                        addedCount++;
                        importedList.Add(newClass);
                    }
                }

                await _classRepository.SaveChangesAsync();
                return (true, $"Nháº­p dá»¯ liá»‡u lá»›p há»c thÃ nh cÃ´ng: ThÃªm má»›i {addedCount} lá»›p, Cáº­p nháº­t {updatedCount} lá»›p.", importedList);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i xá»­ lÃ½ tá»‡p Excel lá»›p há»c: {ex.Message}", importedList);
            }
        }
        #endregion

        #region 5. XUáº¤T & NHáº¬P CÃN Bá»˜ QUáº¢N LÃ
        public async Task<(bool Success, string Message)> ExportOfficersToExcelAsync(IEnumerable<Officer> officers, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Trang cÃ¡n bá»™");

                ws.Cell("A1").Value = "DANH SÃCH CÃN Bá»˜ QUáº¢N LÃ QUÃ‚N Sá»°";
                ws.Range("A1:K1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Thá»i Ä‘iá»ƒm xuáº¥t dá»¯ liá»‡u: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                ws.Range("A2:K2").Merge().Style.Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                string[] headers = { "STT", "MÃ£ cÃ¡n bá»™", "Há» vÃ  tÃªn", "Cáº¥p báº­c", "Chá»©c vá»¥", "ÄÆ¡n vá»‹", "Sá»‘ Ä‘iá»‡n thoáº¡i", "Email", "ChuyÃªn mÃ´n", "NgÃ y sinh", "NgÃ y nháº­p ngÅ©" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                int row = 5;
                int stt = 1;
                foreach (var o in officers)
                {
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = o.OfficerCode;
                    ws.Cell(row, 3).Value = o.FullName;
                    ws.Cell(row, 4).Value = o.Rank;
                    ws.Cell(row, 5).Value = o.Position;
                    ws.Cell(row, 6).Value = o.Unit;
                    ws.Cell(row, 7).Value = o.PhoneNumber;
                    ws.Cell(row, 8).Value = o.Email;
                    ws.Cell(row, 9).Value = o.Specialty;
                    ws.Cell(row, 10).Value = o.DateOfBirth.HasValue ? o.DateOfBirth.Value.ToString("dd/MM/yyyy") : "";
                    ws.Cell(row, 11).Value = o.EnlistmentDate.HasValue ? o.EnlistmentDate.Value.ToString("dd/MM/yyyy") : "";

                    ws.Range(row, 1, row, 11).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                   .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);

                return (true, $"Xuáº¥t thÃ nh cÃ´ng {stt - 1} cÃ¡n bá»™ ra file Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xuáº¥t danh sÃ¡ch cÃ¡n bá»™: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<Officer> Officers)> ImportOfficersFromExcelAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tá»‡p khÃ´ng tá»“n táº¡i.", new List<Officer>());

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, new List<Officer>());

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("cÃ¡n bá»™", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Officer", StringComparison.OrdinalIgnoreCase)) ?? workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                    return (false, "KhÃ´ng tÃ¬m tháº¥y sheet dá»¯ liá»‡u cÃ¡n bá»™.", new List<Officer>());

                var imported = new List<Officer>();
                int headerRow = 1;
                for (int r = 1; r <= 15; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => CleanCellText(c.GetString())));
                    if (textRow.Contains("MÃ£ cÃ¡n bá»™") || textRow.Contains("OfficerCode") || textRow.Contains("Há» vÃ  tÃªn") || textRow.Contains("Há» tÃªn"))
                    {
                        headerRow = r;
                        break;
                    }
                }

                int colCode = -1, colName = -1, colRank = -1, colPos = -1, colUnit = -1;
                int colPhone = -1, colEmail = -1, colSpec = -1, colDob = -1, colEnlist = -1;

                foreach (var cell in ws.Row(headerRow).CellsUsed())
                {
                    var title = CleanCellText(cell.GetString()).ToLowerInvariant();
                    int c = cell.Address.ColumnNumber;

                    if (title.Contains("mÃ£") || title.Contains("officer") || title.Contains("shcb"))
                    {
                        if (colCode == -1) colCode = c;
                    }
                    else if (title.Contains("há»") || title.Contains("tÃªn") || title.Contains("fullname"))
                    {
                        if (colName == -1) colName = c;
                    }
                    else if (title.Contains("cáº¥p báº­c") || title.Contains("quÃ¢n hÃ m") || title.Equals("rank"))
                    {
                        if (colRank == -1) colRank = c;
                    }
                    else if (title.Contains("chá»©c vá»¥") || title.Contains("chá»©c danh") || title.Equals("position"))
                    {
                        if (colPos == -1) colPos = c;
                    }
                    else if (title.Contains("Ä‘Æ¡n vá»‹") || title.Contains("Ä‘áº¡i Ä‘á»™i") || title.Contains("tiá»ƒu Ä‘oÃ n") || title.Equals("unit"))
                    {
                        if (colUnit == -1) colUnit = c;
                    }
                    else if (title.Contains("thoáº¡i") || title.Contains("sÄ‘t") || title.Contains("phone"))
                    {
                        if (colPhone == -1) colPhone = c;
                    }
                    else if (title.Contains("mail"))
                    {
                        if (colEmail == -1) colEmail = c;
                    }
                    else if (title.Contains("chuyÃªn ngÃ nh") || title.Contains("chuyÃªn mÃ´n") || title.Contains("specialty"))
                    {
                        if (colSpec == -1) colSpec = c;
                    }
                    else if (title.Contains("ngÃ y sinh") || title.Contains("sinh") || title.Contains("dob"))
                    {
                        if (colDob == -1) colDob = c;
                    }
                    else if (title.Contains("nháº­p ngÅ©") || title.Contains("enlist"))
                    {
                        if (colEnlist == -1) colEnlist = c;
                    }
                }

                if (colName == -1)
                {
                    var firstTitle = CleanCellText(ws.Cell(headerRow, 1).GetString()).ToLowerInvariant();
                    bool firstIsStt = firstTitle.Contains("stt") || firstTitle.Contains("tt") || firstTitle.Contains("no");
                    colCode = firstIsStt ? 2 : 1;
                    colName = firstIsStt ? 3 : 2;
                    if (colRank == -1) colRank = firstIsStt ? 4 : 3;
                    if (colPos == -1) colPos = firstIsStt ? 5 : 4;
                    if (colUnit == -1) colUnit = firstIsStt ? 6 : 5;
                    if (colPhone == -1) colPhone = firstIsStt ? 7 : 6;
                    if (colEmail == -1) colEmail = firstIsStt ? 8 : 7;
                    if (colSpec == -1) colSpec = firstIsStt ? 9 : 8;
                    if (colDob == -1) colDob = firstIsStt ? 10 : 9;
                    if (colEnlist == -1) colEnlist = firstIsStt ? 11 : 10;
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                for (int row = headerRow + 1; row <= lastRow; row++)
                {
                    string code = colCode > 0 ? CleanCellText(ws.Cell(row, colCode).GetString()) : string.Empty;
                    string name = colName > 0 ? CleanCellText(ws.Cell(row, colName).GetString()) : string.Empty;
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        code = $"CB-{DateTime.Today.Year}-{row:D3}";
                    }

                    string rank = colRank > 0 ? CleanCellText(ws.Cell(row, colRank).GetString()) : "Thiáº¿u Ãºy";
                    string pos = colPos > 0 ? CleanCellText(ws.Cell(row, colPos).GetString()) : "CÃ¡n bá»™";
                    string unit = colUnit > 0 ? CleanCellText(ws.Cell(row, colUnit).GetString()) : "Äáº¡i Ä‘á»™i 1";
                    string phone = colPhone > 0 ? CleanCellText(ws.Cell(row, colPhone).GetString()) : string.Empty;
                    string email = colEmail > 0 ? CleanCellText(ws.Cell(row, colEmail).GetString()) : string.Empty;
                    string specialty = colSpec > 0 ? CleanCellText(ws.Cell(row, colSpec).GetString()) : "Chá»‰ huy tham mÆ°u";
                    string dobStr = colDob > 0 ? CleanCellText(ws.Cell(row, colDob).GetString()) : string.Empty;
                    string enlistStr = colEnlist > 0 ? CleanCellText(ws.Cell(row, colEnlist).GetString()) : string.Empty;

                    DateTime? dob = null;
                    if (DateTime.TryParse(dobStr, out var d)) dob = d;
                    DateTime? enlist = null;
                    if (DateTime.TryParse(enlistStr, out var e)) enlist = e;

                    var existing = await _officerRepository.GetByCodeAsync(code);
                    if (existing != null)
                    {
                        existing.FullName = name;
                        existing.Rank = rank;
                        existing.Position = pos;
                        existing.Unit = unit;
                        existing.PhoneNumber = phone;
                        existing.Email = email;
                        existing.Specialty = specialty;
                        existing.DateOfBirth = dob;
                        existing.EnlistmentDate = enlist;
                        _officerRepository.Update(existing);
                        imported.Add(existing);
                    }
                    else
                    {
                        var newOff = new Officer
                        {
                            OfficerCode = code,
                            FullName = name,
                            Rank = rank,
                            Position = pos,
                            Unit = unit,
                            PhoneNumber = phone,
                            Email = email,
                            Specialty = specialty,
                            DateOfBirth = dob,
                            EnlistmentDate = enlist
                        };
                        await _officerRepository.AddAsync(newOff);
                        imported.Add(newOff);
                    }
                }

                await _officerRepository.SaveChangesAsync();
                return (true, $"Nháº­p thÃ nh cÃ´ng {imported.Count} cÃ¡n bá»™ tá»« Excel.", imported);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i nháº­p danh sÃ¡ch cÃ¡n bá»™: {ex.Message}", new List<Officer>());
            }
        }
        #endregion

        #region 6. XUáº¤T & NHáº¬P DANH Má»¤C Tá»” CHá»¨C
        public async Task<(bool Success, string Message)> ExportCatalogsToExcelAsync(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ranks = (await _rankRepository.GetAllAsync()).OrderBy(r => r.DisplayOrder).ToList();
                var positions = (await _positionRepository.GetAllAsync()).OrderBy(p => p.DisplayOrder).ToList();
                var units = (await _unitRepository.GetAllAsync()).ToList();
                var majors = (await _majorRepository.GetAllAsync()).ToList();

                // 1. Cáº¥p báº­c
                var wsRank = workbook.Worksheets.Add("Cáº¥p báº­c");
                wsRank.Cell("A1").Value = "DANH Má»¤C Cáº¤P Báº¬C QUÃ‚N HÃ€M";
                wsRank.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] rankHeaders = { "STT", "MÃ£ cáº¥p báº­c", "TÃªn cáº¥p báº­c", "NhÃ³m cáº¥p báº­c", "Thá»© tá»± hiá»ƒn thá»‹" };
                for (int i = 0; i < rankHeaders.Length; i++)
                {
                    wsRank.Cell(3, i + 1).Value = rankHeaders[i];
                    wsRank.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < ranks.Count; i++)
                {
                    wsRank.Cell(i + 4, 1).Value = i + 1;
                    wsRank.Cell(i + 4, 2).Value = ranks[i].RankCode;
                    wsRank.Cell(i + 4, 3).Value = ranks[i].RankName;
                    wsRank.Cell(i + 4, 4).Value = ranks[i].RankGroup;
                    wsRank.Cell(i + 4, 5).Value = ranks[i].DisplayOrder;
                }
                wsRank.Columns().AdjustToContents();

                // 2. Chá»©c vá»¥
                var wsPos = workbook.Worksheets.Add("Chá»©c vá»¥");
                wsPos.Cell("A1").Value = "DANH Má»¤C CHá»¨C Vá»¤ QUÃ‚N Sá»°";
                wsPos.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] posHeaders = { "STT", "MÃ£ chá»©c vá»¥", "TÃªn chá»©c vá»¥", "NhÃ³m chá»©c vá»¥", "Thá»© tá»± hiá»ƒn thá»‹" };
                for (int i = 0; i < posHeaders.Length; i++)
                {
                    wsPos.Cell(3, i + 1).Value = posHeaders[i];
                    wsPos.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < positions.Count; i++)
                {
                    wsPos.Cell(i + 4, 1).Value = i + 1;
                    wsPos.Cell(i + 4, 2).Value = positions[i].PositionCode;
                    wsPos.Cell(i + 4, 3).Value = positions[i].PositionName;
                    wsPos.Cell(i + 4, 4).Value = positions[i].PositionGroup;
                    wsPos.Cell(i + 4, 5).Value = positions[i].DisplayOrder;
                }
                wsPos.Columns().AdjustToContents();

                // 3. ÄÆ¡n vá»‹
                var wsUnit = workbook.Worksheets.Add("ÄÆ¡n vá»‹");
                wsUnit.Cell("A1").Value = "DANH Má»¤C ÄÆ N Vá»Š QUáº¢N LÃ";
                wsUnit.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] unitHeaders = { "STT", "MÃ£ Ä‘Æ¡n vá»‹", "TÃªn Ä‘Æ¡n vá»‹", "ÄÆ¡n vá»‹ cáº¥p trÃªn", "NgÆ°á»i chá»‰ huy", "Sá»‘ Ä‘iá»‡n thoáº¡i" };
                for (int i = 0; i < unitHeaders.Length; i++)
                {
                    wsUnit.Cell(3, i + 1).Value = unitHeaders[i];
                    wsUnit.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < units.Count; i++)
                {
                    wsUnit.Cell(i + 4, 1).Value = i + 1;
                    wsUnit.Cell(i + 4, 2).Value = units[i].UnitCode;
                    wsUnit.Cell(i + 4, 3).Value = units[i].UnitName;
                    wsUnit.Cell(i + 4, 4).Value = units[i].ParentUnit;
                    wsUnit.Cell(i + 4, 5).Value = units[i].CommanderName;
                    wsUnit.Cell(i + 4, 6).Value = units[i].ContactPhone;
                }
                wsUnit.Columns().AdjustToContents();

                // 4. ChuyÃªn ngÃ nh
                var wsMajor = workbook.Worksheets.Add("ChuyÃªn ngÃ nh");
                wsMajor.Cell("A1").Value = "DANH Má»¤C CHUYÃŠN NGÃ€NH ÄÃ€O Táº O";
                wsMajor.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] majorHeaders = { "STT", "MÃ£ chuyÃªn ngÃ nh", "TÃªn chuyÃªn ngÃ nh", "Thá»i gian Ä‘Ã o táº¡o", "Khoa phá»¥ trÃ¡ch" };
                for (int i = 0; i < majorHeaders.Length; i++)
                {
                    wsMajor.Cell(3, i + 1).Value = majorHeaders[i];
                    wsMajor.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < majors.Count; i++)
                {
                    wsMajor.Cell(i + 4, 1).Value = i + 1;
                    wsMajor.Cell(i + 4, 2).Value = majors[i].MajorCode;
                    wsMajor.Cell(i + 4, 3).Value = majors[i].MajorName;
                    wsMajor.Cell(i + 4, 4).Value = majors[i].TrainingDuration;
                    wsMajor.Cell(i + 4, 5).Value = majors[i].Department;
                }
                wsMajor.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
                return (true, $"Xuáº¥t thÃ nh cÃ´ng danh má»¥c tá»• chá»©c ({ranks.Count} cáº¥p báº­c, {positions.Count} chá»©c vá»¥, {units.Count} Ä‘Æ¡n vá»‹, {majors.Count} chuyÃªn ngÃ nh) ra file Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xuáº¥t danh má»¥c tá»• chá»©c: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int RanksCount, int PositionsCount, int UnitsCount, int MajorsCount)> ImportCatalogsFromExcelAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tá»‡p khÃ´ng tá»“n táº¡i.", 0, 0, 0, 0);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, 0, 0, 0, 0);

                using var workbook = new XLWorkbook(filePath);
                int rCount = 0, pCount = 0, uCount = 0, mCount = 0;

                // 1. Cáº¥p báº­c
                var wsRank = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Cáº¥p báº­c", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Rank", StringComparison.OrdinalIgnoreCase));
                if (wsRank != null)
                {
                    int lastRow = wsRank.LastRowUsed()?.RowNumber() ?? 0;
                    for (int row = 4; row <= lastRow; row++)
                    {
                        string code = wsRank.Cell(row, 2).GetString().Trim();
                        string name = wsRank.Cell(row, 3).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;
                        string group = wsRank.Cell(row, 4).GetString().Trim();
                        int order = wsRank.Cell(row, 5).TryGetValue<int>(out var o) ? o : 0;

                        var existing = await _rankRepository.GetByCodeAsync(code);
                        if (existing != null)
                        {
                            existing.RankName = name;
                            existing.RankGroup = group;
                            existing.DisplayOrder = order;
                            _rankRepository.Update(existing);
                        }
                        else
                        {
                            await _rankRepository.AddAsync(new MilitaryRank { RankCode = code, RankName = name, RankGroup = group, DisplayOrder = order });
                        }
                        rCount++;
                    }
                    await _rankRepository.SaveChangesAsync();
                }

                // 2. Chá»©c vá»¥
                var wsPos = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Chá»©c vá»¥", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Position", StringComparison.OrdinalIgnoreCase));
                if (wsPos != null)
                {
                    int lastRow = wsPos.LastRowUsed()?.RowNumber() ?? 0;
                    for (int row = 4; row <= lastRow; row++)
                    {
                        string code = wsPos.Cell(row, 2).GetString().Trim();
                        string name = wsPos.Cell(row, 3).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;
                        string group = wsPos.Cell(row, 4).GetString().Trim();
                        int order = wsPos.Cell(row, 5).TryGetValue<int>(out var o) ? o : 0;

                        var existing = await _positionRepository.GetByCodeAsync(code);
                        if (existing != null)
                        {
                            existing.PositionName = name;
                            existing.PositionGroup = group;
                            existing.DisplayOrder = order;
                            _positionRepository.Update(existing);
                        }
                        else
                        {
                            await _positionRepository.AddAsync(new MilitaryPosition { PositionCode = code, PositionName = name, PositionGroup = group, DisplayOrder = order });
                        }
                        pCount++;
                    }
                    await _positionRepository.SaveChangesAsync();
                }

                // 3. ÄÆ¡n vá»‹
                var wsUnit = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("ÄÆ¡n vá»‹", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Unit", StringComparison.OrdinalIgnoreCase));
                if (wsUnit != null)
                {
                    int lastRow = wsUnit.LastRowUsed()?.RowNumber() ?? 0;
                    for (int row = 4; row <= lastRow; row++)
                    {
                        string code = wsUnit.Cell(row, 2).GetString().Trim();
                        string name = wsUnit.Cell(row, 3).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;
                        string parent = wsUnit.Cell(row, 4).GetString().Trim();
                        string cmdr = wsUnit.Cell(row, 5).GetString().Trim();
                        string phone = wsUnit.Cell(row, 6).GetString().Trim();

                        var existing = await _unitRepository.GetByCodeAsync(code);
                        if (existing != null)
                        {
                            existing.UnitName = name;
                            existing.ParentUnit = parent;
                            existing.CommanderName = cmdr;
                            existing.ContactPhone = phone;
                            _unitRepository.Update(existing);
                        }
                        else
                        {
                            await _unitRepository.AddAsync(new MilitaryUnit { UnitCode = code, UnitName = name, ParentUnit = parent, CommanderName = cmdr, ContactPhone = phone });
                        }
                        uCount++;
                    }
                    await _unitRepository.SaveChangesAsync();
                }

                // 4. ChuyÃªn ngÃ nh
                var wsMajor = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("ChuyÃªn ngÃ nh", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Major", StringComparison.OrdinalIgnoreCase));
                if (wsMajor != null)
                {
                    int lastRow = wsMajor.LastRowUsed()?.RowNumber() ?? 0;
                    for (int row = 4; row <= lastRow; row++)
                    {
                        string code = wsMajor.Cell(row, 2).GetString().Trim();
                        string name = wsMajor.Cell(row, 3).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;
                        string duration = wsMajor.Cell(row, 4).GetString().Trim();
                        string dept = wsMajor.Cell(row, 5).GetString().Trim();

                        var existing = await _majorRepository.GetByCodeAsync(code);
                        if (existing != null)
                        {
                            existing.MajorName = name;
                            existing.TrainingDuration = duration;
                            existing.Department = dept;
                            _majorRepository.Update(existing);
                        }
                        else
                        {
                            await _majorRepository.AddAsync(new MilitaryMajor { MajorCode = code, MajorName = name, TrainingDuration = duration, Department = dept });
                        }
                        mCount++;
                    }
                    await _majorRepository.SaveChangesAsync();
                }

                return (true, $"Nháº­p thÃ nh cÃ´ng danh má»¥c: {rCount} cáº¥p báº­c, {pCount} chá»©c vá»¥, {uCount} Ä‘Æ¡n vá»‹, {mCount} chuyÃªn ngÃ nh.", rCount, pCount, uCount, mCount);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i nháº­p danh má»¥c: {ex.Message}", 0, 0, 0, 0);
            }
        }
        #endregion

        #region 7. XUáº¤T & NHáº¬P TOÃ€N Bá»˜ Dá»® LIá»†U Há»† THá»NG (FULL BACKUP / RESTORE)
        public async Task<(bool Success, string Message)> ExportAllDataToExcelAsync(string filePath)
        {
            try
            {
                var classes = (await _classRepository.GetAllWithCadetsAsync()).ToList();
                var cadets = (await _cadetRepository.GetAllAsync()).ToList();
                var subjects = (await _subjectRepository.GetAllAsync()).ToList();
                var records = (await _examRepository.GetAllWithDetailsAsync()).ToList();
                var officers = (await _officerRepository.GetAllAsync()).ToList();
                var ranks = (await _rankRepository.GetAllAsync()).OrderBy(r => r.DisplayOrder).ToList();
                var positions = (await _positionRepository.GetAllAsync()).OrderBy(p => p.DisplayOrder).ToList();
                var units = (await _unitRepository.GetAllAsync()).ToList();
                var majors = (await _majorRepository.GetAllAsync()).ToList();
                var failedRecords = records.Where(r => r.Grade == "KhÃ´ng Ä‘áº¡t").ToList();

                using var workbook = new XLWorkbook();

                // 1. Sheet Tá»•ng quan (KPI Dashboard)
                var wsDash = workbook.Worksheets.Add("Trang tá»•ng quan");
                wsDash.Cell("A1").Value = "BÃO CÃO Tá»”NG QUAN QUáº¢N LÃ Há»ŒC VIÃŠN & CÃN Bá»˜ QUÃ‚N Äá»˜I";
                wsDash.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(16)
                    .Font.SetFontColor(XLColor.FromHtml("#1E3A8A")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                wsDash.Cell("A3").Value = "CHá»ˆ Sá» Tá»”NG Há»¢P TOÃ€N ÄÆ N Vá»Š";
                wsDash.Range("A3:C3").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));

                wsDash.Cell("A4").Value = "Tá»•ng sá»‘ lá»›p há»c quÃ¢n sá»±:";
                wsDash.Cell("B4").Value = classes.Count;
                wsDash.Cell("A5").Value = "Tá»•ng quÃ¢n sá»‘ há»c viÃªn:";
                wsDash.Cell("B5").Value = cadets.Count;
                wsDash.Cell("A6").Value = "Tá»•ng sá»‘ cÃ¡n bá»™ quáº£n lÃ½:";
                wsDash.Cell("B6").Value = officers.Count;
                wsDash.Cell("A7").Value = "Tá»•ng sá»‘ mÃ´n rÃ¨n luyá»‡n:";
                wsDash.Cell("B7").Value = subjects.Count;
                wsDash.Cell("A8").Value = "Tá»•ng sá»‘ lÆ°á»£t kiá»ƒm tra:";
                wsDash.Cell("B8").Value = records.Count;

                double passRate = records.Count > 0 
                    ? Math.Round((double)(records.Count - failedRecords.Count) / records.Count * 100, 1) 
                    : 100.0;
                wsDash.Cell("A9").Value = "Tá»· lá»‡ Ä‘áº¡t chuáº©n quÃ¢n sá»±:";
                wsDash.Cell("B9").Value = $"{passRate}%";

                wsDash.Cell("A11").Value = "PHÃ‚N LOáº I Xáº¾P LOáº I CHI TIáº¾T";
                wsDash.Range("A11:C11").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));

                wsDash.Cell("A12").Value = "Xuáº¥t sáº¯c:";
                wsDash.Cell("B12").Value = records.Count(r => r.Grade == "Xuáº¥t sáº¯c");
                wsDash.Cell("A13").Value = "Giá»i:";
                wsDash.Cell("B13").Value = records.Count(r => r.Grade == "Giá»i");
                wsDash.Cell("A14").Value = "KhÃ¡:";
                wsDash.Cell("B14").Value = records.Count(r => r.Grade == "KhÃ¡");
                wsDash.Cell("A15").Value = "Äáº¡t:";
                wsDash.Cell("B15").Value = records.Count(r => r.Grade == "Äáº¡t");
                wsDash.Cell("A16").Value = "KhÃ´ng Ä‘áº¡t (Cáº§n rÃ¨n luyá»‡n láº¡i):";
                wsDash.Cell("B16").Value = failedRecords.Count;
                wsDash.Cell("B16").Style.Font.SetFontColor(XLColor.Red);
                wsDash.Columns().AdjustToContents();

                // 2. Sheet CÃ¡n bá»™
                var wsOff = workbook.Worksheets.Add("Trang cÃ¡n bá»™");
                wsOff.Cell("A1").Value = "DANH SÃCH CÃN Bá»˜ QUáº¢N LÃ QUÃ‚N Sá»°";
                wsOff.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] oHeaders = { "STT", "MÃ£ cÃ¡n bá»™", "Há» vÃ  tÃªn", "Cáº¥p báº­c", "Chá»©c vá»¥", "ÄÆ¡n vá»‹", "Sá»‘ Ä‘iá»‡n thoáº¡i", "Email", "ChuyÃªn mÃ´n", "NgÃ y sinh", "NgÃ y nháº­p ngÅ©" };
                for (int i = 0; i < oHeaders.Length; i++)
                {
                    wsOff.Cell(3, i + 1).Value = oHeaders[i];
                    wsOff.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < officers.Count; i++)
                {
                    var o = officers[i];
                    wsOff.Cell(i + 4, 1).Value = i + 1;
                    wsOff.Cell(i + 4, 2).Value = o.OfficerCode;
                    wsOff.Cell(i + 4, 3).Value = o.FullName;
                    wsOff.Cell(i + 4, 4).Value = o.Rank;
                    wsOff.Cell(i + 4, 5).Value = o.Position;
                    wsOff.Cell(i + 4, 6).Value = o.Unit;
                    wsOff.Cell(i + 4, 7).Value = o.PhoneNumber;
                    wsOff.Cell(i + 4, 8).Value = o.Email;
                    wsOff.Cell(i + 4, 9).Value = o.Specialty;
                    wsOff.Cell(i + 4, 10).Value = o.DateOfBirth.HasValue ? o.DateOfBirth.Value.ToString("dd/MM/yyyy") : "";
                    wsOff.Cell(i + 4, 11).Value = o.EnlistmentDate.HasValue ? o.EnlistmentDate.Value.ToString("dd/MM/yyyy") : "";
                }
                wsOff.Columns().AdjustToContents();

                // 3. Sheet Lá»›p há»c
                var wsClass = workbook.Worksheets.Add("Trang lá»›p há»c");
                wsClass.Cell("A1").Value = "DANH Má»¤C Lá»šP Há»ŒC QUÃ‚N Äá»˜I";
                wsClass.Range("A1:H1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] cHeaders = { "STT", "MÃ£ lá»›p", "TÃªn lá»›p", "ÄÆ¡n vá»‹ quáº£n lÃ½", "ChuyÃªn ngÃ nh", "CÃ¡n bá»™ quáº£n lÃ½", "KhÃ³a há»c", "QuÃ¢n sá»‘" };
                for (int i = 0; i < cHeaders.Length; i++)
                {
                    wsClass.Cell(3, i + 1).Value = cHeaders[i];
                    wsClass.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < classes.Count; i++)
                {
                    var cl = classes[i];
                    wsClass.Cell(i + 4, 1).Value = i + 1;
                    wsClass.Cell(i + 4, 2).Value = cl.ClassCode;
                    wsClass.Cell(i + 4, 3).Value = cl.ClassName;
                    wsClass.Cell(i + 4, 4).Value = cl.Unit;
                    wsClass.Cell(i + 4, 5).Value = cl.Major;
                    wsClass.Cell(i + 4, 6).Value = cl.OfficerInCharge;
                    wsClass.Cell(i + 4, 7).Value = cl.AcademicYear;
                    wsClass.Cell(i + 4, 8).Value = cl.Cadets?.Count ?? 0;
                }
                wsClass.Columns().AdjustToContents();

                // 4. Sheet Há»c viÃªn
                var wsCadet = workbook.Worksheets.Add("Trang há»c viÃªn");
                wsCadet.Cell("A1").Value = "DANH SÃCH Há»ŒC VIÃŠN QUÃ‚N Äá»˜I";
                wsCadet.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] cdHeaders = { "STT", "MÃ£ há»c viÃªn", "Há» vÃ  tÃªn", "Cáº¥p báº­c", "Chá»©c vá»¥", "ÄÆ¡n vá»‹", "Lá»›p", "Sá»‘ Ä‘iá»‡n thoáº¡i", "Email", "Tuá»•i", "Giá»›i tÃ­nh" };
                for (int i = 0; i < cdHeaders.Length; i++)
                {
                    wsCadet.Cell(3, i + 1).Value = cdHeaders[i];
                    wsCadet.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < cadets.Count; i++)
                {
                    var c = cadets[i];
                    wsCadet.Cell(i + 4, 1).Value = i + 1;
                    wsCadet.Cell(i + 4, 2).Value = c.CadetCode;
                    wsCadet.Cell(i + 4, 3).Value = c.FullName;
                    wsCadet.Cell(i + 4, 4).Value = c.Rank;
                    wsCadet.Cell(i + 4, 5).Value = c.Position;
                    wsCadet.Cell(i + 4, 6).Value = c.Unit;
                    wsCadet.Cell(i + 4, 7).Value = c.ClassName;
                    wsCadet.Cell(i + 4, 8).Value = c.PhoneNumber;
                    wsCadet.Cell(i + 4, 9).Value = c.Email;
                    wsCadet.Cell(i + 4, 10).Value = c.Age ?? 0;
                    wsCadet.Cell(i + 4, 11).Value = c.Gender;
                }
                wsCadet.Columns().AdjustToContents();

                // 5. Sheet MÃ´n há»c
                var wsSub = workbook.Worksheets.Add("Trang mÃ´n há»c");
                wsSub.Cell("A1").Value = "DANH Má»¤C TIÃŠU CHUáº¨N RÃˆN LUYá»†N THá»‚ Lá»°C";
                wsSub.Range("A1:I1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] sHeaders = { "STT", "MÃ£ mÃ´n", "TÃªn mÃ´n", "NhÃ³m tá»‘ cháº¥t", "ÄÆ¡n vá»‹ tÃ­nh", "Chuáº©n Giá»i", "Chuáº©n KhÃ¡", "Chuáº©n Äáº¡t", "CÃ ng cao cÃ ng tá»‘t" };
                for (int i = 0; i < sHeaders.Length; i++)
                {
                    wsSub.Cell(3, i + 1).Value = sHeaders[i];
                    wsSub.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < subjects.Count; i++)
                {
                    var s = subjects[i];
                    wsSub.Cell(i + 4, 1).Value = i + 1;
                    wsSub.Cell(i + 4, 2).Value = s.SubjectCode;
                    wsSub.Cell(i + 4, 3).Value = s.SubjectName;
                    wsSub.Cell(i + 4, 4).Value = s.Category;
                    wsSub.Cell(i + 4, 5).Value = s.Unit;
                    wsSub.Cell(i + 4, 6).Value = s.ExcellentThreshold;
                    wsSub.Cell(i + 4, 7).Value = s.GoodThreshold;
                    wsSub.Cell(i + 4, 8).Value = s.PassThreshold;
                    wsSub.Cell(i + 4, 9).Value = s.IsHigherBetter ? "CÃ³" : "KhÃ´ng";
                }
                wsSub.Columns().AdjustToContents();

                // 6. Sheet Kiá»ƒm tra thá»ƒ lá»±c
                var wsExam = workbook.Worksheets.Add("Kiá»ƒm tra thá»ƒ lá»±c");
                wsExam.Cell("A1").Value = "Báº¢NG Káº¾T QUáº¢ KIá»‚M TRA Äá»ŠNH Ká»²";
                wsExam.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#15803D"));
                string[] eHeaders = { "STT", "MÃ£ há»c viÃªn", "Há» vÃ  tÃªn", "ÄÆ¡n vá»‹", "Lá»›p", "MÃ£ mÃ´n", "TÃªn mÃ´n", "ThÃ nh tÃ­ch", "Xáº¿p loáº¡i", "Äá»£t kiá»ƒm tra", "NgÃ y kiá»ƒm tra" };
                for (int i = 0; i < eHeaders.Length; i++)
                {
                    wsExam.Cell(3, i + 1).Value = eHeaders[i];
                    wsExam.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#15803D"));
                }
                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];
                    wsExam.Cell(i + 4, 1).Value = i + 1;
                    wsExam.Cell(i + 4, 2).Value = r.Cadet?.CadetCode ?? "";
                    wsExam.Cell(i + 4, 3).Value = r.Cadet?.FullName ?? "";
                    wsExam.Cell(i + 4, 4).Value = r.Cadet?.Unit ?? "";
                    wsExam.Cell(i + 4, 5).Value = r.Cadet?.ClassName ?? "";
                    wsExam.Cell(i + 4, 6).Value = r.Subject?.SubjectCode ?? "";
                    wsExam.Cell(i + 4, 7).Value = r.Subject?.SubjectName ?? "";
                    wsExam.Cell(i + 4, 8).Value = r.ScoreValue;
                    wsExam.Cell(i + 4, 9).Value = r.Grade;
                    wsExam.Cell(i + 4, 10).Value = r.ExamSession;
                    wsExam.Cell(i + 4, 11).Value = r.ExamDate.ToString("dd/MM/yyyy");
                }
                wsExam.Columns().AdjustToContents();

                // 7. Sheet Há»c viÃªn chÆ°a Ä‘áº¡t (RÃ¨n luyá»‡n bá»• sung)
                var wsFail = workbook.Worksheets.Add("RÃ¨n luyá»‡n bá»• sung");
                wsFail.Cell("A1").Value = "DANH SÃCH Há»ŒC VIÃŠN CHÆ¯A Äáº T Cáº¦N RÃˆN LUYá»†N Bá»” SUNG";
                wsFail.Range("A1:H1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#DC2626"));
                string[] fHeaders = { "STT", "MÃ£ há»c viÃªn", "Há» vÃ  tÃªn", "ÄÆ¡n vá»‹", "Lá»›p", "Ná»™i dung chÆ°a Ä‘áº¡t", "ThÃ nh tÃ­ch", "NgÃ y kiá»ƒm tra" };
                for (int i = 0; i < fHeaders.Length; i++)
                {
                    wsFail.Cell(3, i + 1).Value = fHeaders[i];
                    wsFail.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#DC2626"));
                }
                for (int i = 0; i < failedRecords.Count; i++)
                {
                    var f = failedRecords[i];
                    wsFail.Cell(i + 4, 1).Value = i + 1;
                    wsFail.Cell(i + 4, 2).Value = f.Cadet?.CadetCode ?? "";
                    wsFail.Cell(i + 4, 3).Value = f.Cadet?.FullName ?? "";
                    wsFail.Cell(i + 4, 4).Value = f.Cadet?.Unit ?? "";
                    wsFail.Cell(i + 4, 5).Value = f.Cadet?.ClassName ?? "";
                    wsFail.Cell(i + 4, 6).Value = f.Subject?.SubjectName ?? "";
                    wsFail.Cell(i + 4, 7).Value = f.ScoreValue;
                    wsFail.Cell(i + 4, 8).Value = f.ExamDate.ToString("dd/MM/yyyy");
                }
                wsFail.Columns().AdjustToContents();

                // 8. Sheet Danh má»¥c tá»• chá»©c: Cáº¥p báº­c
                var wsRank = workbook.Worksheets.Add("Cáº¥p báº­c");
                wsRank.Cell("A1").Value = "DANH Má»¤C Cáº¤P Báº¬C QUÃ‚N HÃ€M";
                wsRank.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] rHeaders = { "STT", "MÃ£ cáº¥p báº­c", "TÃªn cáº¥p báº­c", "NhÃ³m cáº¥p báº­c", "Thá»© tá»± hiá»ƒn thá»‹" };
                for (int i = 0; i < rHeaders.Length; i++)
                {
                    wsRank.Cell(3, i + 1).Value = rHeaders[i];
                    wsRank.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < ranks.Count; i++)
                {
                    wsRank.Cell(i + 4, 1).Value = i + 1;
                    wsRank.Cell(i + 4, 2).Value = ranks[i].RankCode;
                    wsRank.Cell(i + 4, 3).Value = ranks[i].RankName;
                    wsRank.Cell(i + 4, 4).Value = ranks[i].RankGroup;
                    wsRank.Cell(i + 4, 5).Value = ranks[i].DisplayOrder;
                }
                wsRank.Columns().AdjustToContents();

                // 9. Sheet Chá»©c vá»¥
                var wsPos = workbook.Worksheets.Add("Chá»©c vá»¥");
                wsPos.Cell("A1").Value = "DANH Má»¤C CHá»¨C Vá»¤ QUÃ‚N Sá»°";
                wsPos.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] pHeaders = { "STT", "MÃ£ chá»©c vá»¥", "TÃªn chá»©c vá»¥", "NhÃ³m chá»©c vá»¥", "Thá»© tá»± hiá»ƒn thá»‹" };
                for (int i = 0; i < pHeaders.Length; i++)
                {
                    wsPos.Cell(3, i + 1).Value = pHeaders[i];
                    wsPos.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < positions.Count; i++)
                {
                    wsPos.Cell(i + 4, 1).Value = i + 1;
                    wsPos.Cell(i + 4, 2).Value = positions[i].PositionCode;
                    wsPos.Cell(i + 4, 3).Value = positions[i].PositionName;
                    wsPos.Cell(i + 4, 4).Value = positions[i].PositionGroup;
                    wsPos.Cell(i + 4, 5).Value = positions[i].DisplayOrder;
                }
                wsPos.Columns().AdjustToContents();

                // 10. Sheet ÄÆ¡n vá»‹
                var wsUnit = workbook.Worksheets.Add("ÄÆ¡n vá»‹");
                wsUnit.Cell("A1").Value = "DANH Má»¤C ÄÆ N Vá»Š QUáº¢N LÃ";
                wsUnit.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] uHeaders = { "STT", "MÃ£ Ä‘Æ¡n vá»‹", "TÃªn Ä‘Æ¡n vá»‹", "ÄÆ¡n vá»‹ cáº¥p trÃªn", "NgÆ°á»i chá»‰ huy", "Sá»‘ Ä‘iá»‡n thoáº¡i" };
                for (int i = 0; i < uHeaders.Length; i++)
                {
                    wsUnit.Cell(3, i + 1).Value = uHeaders[i];
                    wsUnit.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < units.Count; i++)
                {
                    wsUnit.Cell(i + 4, 1).Value = i + 1;
                    wsUnit.Cell(i + 4, 2).Value = units[i].UnitCode;
                    wsUnit.Cell(i + 4, 3).Value = units[i].UnitName;
                    wsUnit.Cell(i + 4, 4).Value = units[i].ParentUnit;
                    wsUnit.Cell(i + 4, 5).Value = units[i].CommanderName;
                    wsUnit.Cell(i + 4, 6).Value = units[i].ContactPhone;
                }
                wsUnit.Columns().AdjustToContents();

                // 11. Sheet ChuyÃªn ngÃ nh
                var wsMajor = workbook.Worksheets.Add("ChuyÃªn ngÃ nh");
                wsMajor.Cell("A1").Value = "DANH Má»¤C CHUYÃŠN NGÃ€NH ÄÃ€O Táº O";
                wsMajor.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] mHeaders = { "STT", "MÃ£ chuyÃªn ngÃ nh", "TÃªn chuyÃªn ngÃ nh", "Thá»i gian Ä‘Ã o táº¡o", "Khoa phá»¥ trÃ¡ch" };
                for (int i = 0; i < mHeaders.Length; i++)
                {
                    wsMajor.Cell(3, i + 1).Value = mHeaders[i];
                    wsMajor.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < majors.Count; i++)
                {
                    wsMajor.Cell(i + 4, 1).Value = i + 1;
                    wsMajor.Cell(i + 4, 2).Value = majors[i].MajorCode;
                    wsMajor.Cell(i + 4, 3).Value = majors[i].MajorName;
                    wsMajor.Cell(i + 4, 4).Value = majors[i].TrainingDuration;
                    wsMajor.Cell(i + 4, 5).Value = majors[i].Department;
                }
                wsMajor.Columns().AdjustToContents();

                // 12. Sheet MÃ´n há»c tÃ­n chá»‰
                var creditSubjects = await _context.CreditSubjects.AsNoTracking().ToListAsync();
                var wsCreditSub = workbook.Worksheets.Add("MÃ´n há»c tÃ­n chá»‰");
                wsCreditSub.Cell("A1").Value = "DANH Má»¤C MÃ”N Há»ŒC TÃN CHá»ˆ QUÃ‚N Sá»°";
                wsCreditSub.Range("A1:H1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] csHeaders = { "STT", "MÃ£ mÃ´n há»c", "TÃªn mÃ´n há»c", "Sá»‘ tÃ­n chá»‰", "HÃ¬nh thá»©c Ä‘Ã¡nh giÃ¡", "NhÃ³m mÃ´n há»c", "LÃ  mÃ´n thÃ nh pháº§n", "Ghi chÃº" };
                for (int i = 0; i < csHeaders.Length; i++)
                {
                    wsCreditSub.Cell(3, i + 1).Value = csHeaders[i];
                    wsCreditSub.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < creditSubjects.Count; i++)
                {
                    var cs = creditSubjects[i];
                    wsCreditSub.Cell(i + 4, 1).Value = i + 1;
                    wsCreditSub.Cell(i + 4, 2).Value = cs.SubjectCode;
                    wsCreditSub.Cell(i + 4, 3).Value = cs.SubjectName;
                    wsCreditSub.Cell(i + 4, 4).Value = cs.Credits;
                    wsCreditSub.Cell(i + 4, 5).Value = cs.AssessmentType;
                    wsCreditSub.Cell(i + 4, 6).Value = cs.SubjectGroup ?? "";
                    wsCreditSub.Cell(i + 4, 7).Value = cs.IsComponent ? "CÃ³" : "KhÃ´ng";
                    wsCreditSub.Cell(i + 4, 8).Value = cs.Description ?? "";
                }
                wsCreditSub.Columns().AdjustToContents();

                // 13. Sheet Äá»£t kiá»ƒm tra & Thi
                var components = await _context.SubjectAssessmentComponents
                    .Include(c => c.CreditSubject)
                    .AsNoTracking()
                    .OrderBy(c => c.CreditSubjectId)
                    .ThenBy(c => c.OrderIndex)
                    .ToListAsync();

                var wsComp = workbook.Worksheets.Add("Äá»£t kiá»ƒm tra & Thi");
                wsComp.Cell("A1").Value = "DANH Má»¤C Äá»¢T KIá»‚M TRA & THI Cá»¦A MÃ”N TÃN CHá»ˆ";
                wsComp.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] compHeaders = { "STT", "MÃ£ mÃ´n há»c", "TÃªn mÃ´n há»c", "TÃªn Ä‘á»£t kiá»ƒm tra / thi", "Sá»‘ tÃ­n chá»‰ Ä‘á»£t", "Thá»© tá»±" };
                for (int i = 0; i < compHeaders.Length; i++)
                {
                    wsComp.Cell(3, i + 1).Value = compHeaders[i];
                    wsComp.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < components.Count; i++)
                {
                    var cp = components[i];
                    wsComp.Cell(i + 4, 1).Value = i + 1;
                    wsComp.Cell(i + 4, 2).Value = cp.CreditSubject?.SubjectCode ?? "";
                    wsComp.Cell(i + 4, 3).Value = cp.CreditSubject?.SubjectName ?? "";
                    wsComp.Cell(i + 4, 4).Value = cp.ComponentName;
                    wsComp.Cell(i + 4, 5).Value = cp.Credits;
                    wsComp.Cell(i + 4, 6).Value = cp.OrderIndex;
                }
                wsComp.Columns().AdjustToContents();

                // 14. Sheet Báº£ng Ä‘iá»ƒm chi tiáº¿t
                var creditScores = await _context.CreditScoreRecords
                    .Include(s => s.Cadet)
                    .Include(s => s.CreditSubject)
                    .Include(s => s.Component)
                    .AsNoTracking()
                    .ToListAsync();

                var wsScores = workbook.Worksheets.Add("Báº£ng Ä‘iá»ƒm tÃ­n chá»‰");
                wsScores.Cell("A1").Value = "CHI TIáº¾T Äáº¦U ÄIá»‚M Há»ŒC Táº¬P TÃN CHá»ˆ Há»ŒC VIÃŠN";
                wsScores.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] scHeaders = { "STT", "MÃ£ há»c viÃªn", "Há» vÃ  tÃªn", "ÄÆ¡n vá»‹", "Lá»›p", "MÃ£ mÃ´n há»c", "TÃªn mÃ´n há»c", "TÃªn Ä‘á»£t kiá»ƒm tra / thi", "Äiá»ƒm sá»‘", "NgÃ y kiá»ƒm tra", "Ghi chÃº" };
                for (int i = 0; i < scHeaders.Length; i++)
                {
                    wsScores.Cell(3, i + 1).Value = scHeaders[i];
                    wsScores.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                for (int i = 0; i < creditScores.Count; i++)
                {
                    var sc = creditScores[i];
                    wsScores.Cell(i + 4, 1).Value = i + 1;
                    wsScores.Cell(i + 4, 2).Value = sc.Cadet?.CadetCode ?? "";
                    wsScores.Cell(i + 4, 3).Value = sc.Cadet?.FullName ?? "";
                    wsScores.Cell(i + 4, 4).Value = sc.Cadet?.Unit ?? "";
                    wsScores.Cell(i + 4, 5).Value = sc.Cadet?.ClassName ?? "";
                    wsScores.Cell(i + 4, 6).Value = sc.CreditSubject?.SubjectCode ?? "";
                    wsScores.Cell(i + 4, 7).Value = sc.CreditSubject?.SubjectName ?? "";
                    wsScores.Cell(i + 4, 8).Value = sc.Component?.ComponentName ?? (sc.ExamSession ?? "");
                    wsScores.Cell(i + 4, 9).Value = sc.FinalScore;
                    wsScores.Cell(i + 4, 10).Value = sc.ExamDate.ToString("dd/MM/yyyy");
                    wsScores.Cell(i + 4, 11).Value = sc.Notes ?? "";
                }
                wsScores.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
                return (true, $"Xuáº¥t toÃ n bá»™ dá»¯ liá»‡u thÃ nh cÃ´ng ra file Excel ({classes.Count} lá»›p há»c, {cadets.Count} há»c viÃªn, {officers.Count} cÃ¡n bá»™, {creditSubjects.Count} mÃ´n tÃ­n chá»‰, {components.Count} Ä‘á»£t kiá»ƒm tra, {creditScores.Count} Ä‘áº§u Ä‘iá»ƒm trÃªn 14 sheets).");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i xuáº¥t toÃ n bá»™ dá»¯ liá»‡u: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int ClassesCount, int CadetsCount, int SubjectsCount, int ExamsCount, int OfficersCount, int CreditSubjectsCount, int CreditScoresCount)> ImportAllDataFromExcelAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tá»‡p khÃ´ng tá»“n táº¡i.", 0, 0, 0, 0, 0, 0, 0);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, 0, 0, 0, 0, 0, 0, 0);

                // 1. Nháº­p danh má»¥c tá»• chá»©c trÆ°á»›c
                var catResult = await ImportCatalogsFromExcelAsync(filePath);
                // 2. Nháº­p cÃ¡n bá»™
                var offResult = await ImportOfficersFromExcelAsync(filePath);
                // 3. Nháº­p lá»›p há»c
                var classResult = await ImportClassesFromExcelAsync(filePath);
                // 4. Nháº­p mÃ´n há»c thá»ƒ lá»±c
                var subResult = await ImportSubjectsFromExcelAsync(filePath);
                // 5. Nháº­p há»c viÃªn
                var cadetResult = await ImportCadetsFromExcelAsync(filePath);
                // 6. Nháº­p káº¿t quáº£ kiá»ƒm tra thá»ƒ lá»±c
                var examResult = await ImportExamRecordsFromExcelAsync(filePath);
                // 7. Nháº­p mÃ´n há»c tÃ­n chá»‰, Ä‘á»£t kiá»ƒm tra vÃ  báº£ng Ä‘iá»ƒm tÃ­n chá»‰
                var (csCount, scoreCount) = await ImportCreditDataFromExcelAsync(filePath);

                int clCount = classResult.Classes.Count;
                int cCount = cadetResult.Cadets.Count;
                int sCount = subResult.Subjects.Count;
                int eCount = examResult.Records.Count;
                int offCount = offResult.Officers.Count;

                return (true, $"KhÃ´i phá»¥c toÃ n bá»™ dá»¯ liá»‡u thÃ nh cÃ´ng: {clCount} lá»›p há»c, {cCount} há»c viÃªn, {offCount} cÃ¡n bá»™, {csCount} mÃ´n tÃ­n chá»‰, {scoreCount} Ä‘áº§u Ä‘iá»ƒm, {sCount} mÃ´n thá»ƒ lá»±c, {eCount} lÆ°á»£t kiá»ƒm tra thá»ƒ lá»±c.", clCount, cCount, sCount, eCount, offCount, csCount, scoreCount);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i nháº­p toÃ n bá»™ dá»¯ liá»‡u: {ex.Message}", 0, 0, 0, 0, 0, 0, 0);
            }
        }

        private async Task<(int SubjectsCount, int ScoresCount)> ImportCreditDataFromExcelAsync(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            int importedSubjects = 0;
            int importedScores = 0;

            var wsCreditSub = workbook.Worksheets.FirstOrDefault(w => w.Name == "MÃ´n há»c tÃ­n chá»‰");
            var wsComp = workbook.Worksheets.FirstOrDefault(w => w.Name == "Äá»£t kiá»ƒm tra & Thi");
            var wsScores = workbook.Worksheets.FirstOrDefault(w => w.Name == "Báº£ng Ä‘iá»ƒm tÃ­n chá»‰");

            if (wsCreditSub != null)
            {
                var existingSubjects = await _context.CreditSubjects.ToListAsync();
                int lastRow = wsCreditSub.LastRowUsed()?.RowNumber() ?? 3;
                for (int r = 4; r <= lastRow; r++)
                {
                    string code = CleanCellText(wsCreditSub.Cell(r, 2).GetString());
                    string name = CleanCellText(wsCreditSub.Cell(r, 3).GetString());
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

                    double credits = 1.0;
                    var cVal = wsCreditSub.Cell(r, 4).Value;
                    if (cVal.IsNumber) credits = cVal.GetNumber();
                    else if (double.TryParse(CleanCellText(cVal.ToString()), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cv))
                        credits = cv;

                    string assess = CleanCellText(wsCreditSub.Cell(r, 5).GetString());
                    if (string.IsNullOrWhiteSpace(assess)) assess = "Kiá»ƒm tra vÃ  thi";
                    string group = CleanCellText(wsCreditSub.Cell(r, 6).GetString());
                    string isCompStr = CleanCellText(wsCreditSub.Cell(r, 7).GetString());
                    bool isComp = isCompStr.Equals("CÃ³", StringComparison.OrdinalIgnoreCase);
                    string desc = CleanCellText(wsCreditSub.Cell(r, 8).GetString());

                    var subj = existingSubjects.FirstOrDefault(s => s.SubjectCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                    if (subj == null)
                    {
                        subj = new CreditSubject
                        {
                            SubjectCode = code,
                            SubjectName = name,
                            Credits = credits,
                            AssessmentType = assess,
                            SubjectGroup = group,
                            IsComponent = isComp,
                            Description = desc,
                            CreatedAt = DateTime.Now
                        };
                        _context.CreditSubjects.Add(subj);
                        existingSubjects.Add(subj);
                        importedSubjects++;
                    }
                    else
                    {
                        subj.SubjectName = name;
                        subj.Credits = credits;
                        subj.AssessmentType = assess;
                        subj.SubjectGroup = group;
                        subj.IsComponent = isComp;
                        subj.Description = desc;
                    }
                }
                await _context.SaveChangesAsync();

                // Nháº­p Äá»£t kiá»ƒm tra & Thi
                if (wsComp != null)
                {
                    var existingComps = await _context.SubjectAssessmentComponents.ToListAsync();
                    int cLastRow = wsComp.LastRowUsed()?.RowNumber() ?? 3;
                    for (int r = 4; r <= cLastRow; r++)
                    {
                        string sCode = CleanCellText(wsComp.Cell(r, 2).GetString());
                        string cName = CleanCellText(wsComp.Cell(r, 4).GetString());
                        if (string.IsNullOrWhiteSpace(sCode) || string.IsNullOrWhiteSpace(cName)) continue;

                        double cCredits = 1.0;
                        var val = wsComp.Cell(r, 5).Value;
                        if (val.IsNumber) cCredits = val.GetNumber();
                        else if (double.TryParse(CleanCellText(val.ToString()), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cv))
                            cCredits = cv;

                        int order = r - 3;
                        var oVal = wsComp.Cell(r, 6).Value;
                        if (oVal.IsNumber) order = (int)oVal.GetNumber();

                        var targetSubj = existingSubjects.FirstOrDefault(s => s.SubjectCode.Equals(sCode, StringComparison.OrdinalIgnoreCase));
                        if (targetSubj != null)
                        {
                            var comp = existingComps.FirstOrDefault(c => c.CreditSubjectId == targetSubj.Id && c.ComponentName.Equals(cName, StringComparison.OrdinalIgnoreCase));
                            if (comp == null)
                            {
                                comp = new SubjectAssessmentComponent
                                {
                                    CreditSubjectId = targetSubj.Id,
                                    ComponentName = cName,
                                    Credits = cCredits,
                                    OrderIndex = order,
                                    CreatedAt = DateTime.Now
                                };
                                _context.SubjectAssessmentComponents.Add(comp);
                                existingComps.Add(comp);
                            }
                            else
                            {
                                comp.Credits = cCredits;
                                comp.OrderIndex = order;
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Nháº­p Báº£ng Ä‘iá»ƒm chi tiáº¿t
                if (wsScores != null)
                {
                    var allCadets = await _context.Cadets.ToListAsync();
                    var allComps = await _context.SubjectAssessmentComponents.ToListAsync();
                    var existingScores = await _context.CreditScoreRecords.ToListAsync();

                    int sLastRow = wsScores.LastRowUsed()?.RowNumber() ?? 3;
                    for (int r = 4; r <= sLastRow; r++)
                    {
                        string cadetCode = CleanCellText(wsScores.Cell(r, 2).GetString());
                        string cadetName = CleanCellText(wsScores.Cell(r, 3).GetString());
                        string sCode = CleanCellText(wsScores.Cell(r, 6).GetString());
                        string cName = CleanCellText(wsScores.Cell(r, 8).GetString());

                        double score = 0.0;
                        var sVal = wsScores.Cell(r, 9).Value;
                        if (sVal.IsNumber) score = sVal.GetNumber();
                        else if (double.TryParse(CleanCellText(sVal.ToString()).Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sv))
                            score = sv;

                        DateTime examDate = DateTime.Now;
                        var dVal = wsScores.Cell(r, 10).Value;
                        if (dVal.IsDateTime) examDate = dVal.GetDateTime();
                        else if (DateTime.TryParse(CleanCellText(dVal.ToString()), out var dt))
                            examDate = dt;

                        string note = CleanCellText(wsScores.Cell(r, 11).GetString());

                        var cadet = allCadets.FirstOrDefault(c => (!string.IsNullOrEmpty(cadetCode) && c.CadetCode.Equals(cadetCode, StringComparison.OrdinalIgnoreCase)) || c.FullName.Equals(cadetName, StringComparison.OrdinalIgnoreCase));
                        var subj = existingSubjects.FirstOrDefault(s => s.SubjectCode.Equals(sCode, StringComparison.OrdinalIgnoreCase));
                        if (cadet == null || subj == null) continue;

                        var comp = allComps.FirstOrDefault(c => c.CreditSubjectId == subj.Id && c.ComponentName.Equals(cName, StringComparison.OrdinalIgnoreCase));

                        var scoreRecord = existingScores.FirstOrDefault(s => s.CadetId == cadet.Id && s.CreditSubjectId == subj.Id && ((comp != null && s.ComponentId == comp.Id) || (comp == null && (s.ExamSession == cName || s.ExamSession == null))));

                        if (scoreRecord == null)
                        {
                            scoreRecord = new CreditScoreRecord
                            {
                                CadetId = cadet.Id,
                                CreditSubjectId = subj.Id,
                                ComponentId = comp?.Id,
                                ExamSession = cName,
                                FinalScore = score,
                                RegularScore = score,
                                ExamScore = score,
                                ExamDate = examDate,
                                Notes = note,
                                CreatedAt = DateTime.Now
                            };
                            _context.CreditScoreRecords.Add(scoreRecord);
                            existingScores.Add(scoreRecord);
                            importedScores++;
                        }
                        else
                        {
                            scoreRecord.FinalScore = score;
                            scoreRecord.RegularScore = score;
                            scoreRecord.ExamScore = score;
                            scoreRecord.ExamDate = examDate;
                            scoreRecord.Notes = note;
                            importedScores++;
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return (importedSubjects, importedScores);
        }
        #endregion

        #region 8. BÃ¡o CÃ¡o Äá»‘i SoÃ¡t & So SÃ¡nh Äá»£t Thi
        public async Task<(bool Success, string Message)> ExportComparisonToExcelAsync(ExamComparisonResultDto comparison, string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook();

                    // ================= Sheet 1: Tá»•ng quan Cáº¥p Äáº¡i Ä‘á»™i =================
                    var wsUnit = workbook.Worksheets.Add("1. Tá»•ng Quan & Äáº¡i Äá»™i");
                    wsUnit.ShowGridLines = true;

                    // TiÃªu Ä‘á»
                    wsUnit.Cell(1, 1).Value = "BÃO CÃO PHÃ‚N TÃCH SO SÃNH Káº¾T QUáº¢ RÃˆN LUYá»†N THá»‚ Lá»°C";
                    wsUnit.Range(1, 1, 1, 10).Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    wsUnit.Cell(2, 1).Value = $"Äá»£t gá»‘c: {comparison.BaselineSession}   |   Äá»£t so sÃ¡nh: {comparison.CompareSession}   |   NgÃ y xuáº¥t: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    wsUnit.Range(2, 1, 2, 10).Merge().Style.Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    // Thá»‘ng kÃª toÃ n Ä‘Æ¡n vá»‹
                    wsUnit.Cell(4, 1).Value = "THá»NG KÃŠ BIáº¾N Äá»˜NG TOÃ€N QUÃ‚N Sá»";
                    wsUnit.Range(4, 1, 4, 10).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#1E293B")).Font.SetFontColor(XLColor.White);

                    wsUnit.Cell(5, 1).Value = "Tá»•ng quÃ¢n sá»‘ Ä‘Ã¡nh giÃ¡:";
                    wsUnit.Cell(5, 2).Value = comparison.TotalEvaluatedCadets;
                    wsUnit.Cell(5, 3).Value = "TÄƒng trÆ°á»Ÿng (â–²):";
                    wsUnit.Cell(5, 4).Value = $"{comparison.OverallGrowthCount} ({comparison.OverallGrowthPercentage:F1}%)";
                    wsUnit.Cell(5, 4).Style.Font.SetFontColor(XLColor.FromHtml("#16A34A")).Font.SetBold();

                    wsUnit.Cell(5, 5).Value = "Giá»¯ nguyÃªn (â€”):";
                    wsUnit.Cell(5, 6).Value = $"{comparison.OverallUnchangedCount} ({comparison.OverallUnchangedPercentage:F1}%)";
                    wsUnit.Cell(5, 6).Style.Font.SetFontColor(XLColor.FromHtml("#D97706")).Font.SetBold();

                    wsUnit.Cell(5, 7).Value = "Thá»¥t lÃ¹i (â–¼):";
                    wsUnit.Cell(5, 8).Value = $"{comparison.OverallRegressionCount} ({comparison.OverallRegressionPercentage:F1}%)";
                    wsUnit.Cell(5, 8).Style.Font.SetFontColor(XLColor.FromHtml("#DC2626")).Font.SetBold();

                    wsUnit.Cell(5, 9).Value = "Delta % Äáº¡t:";
                    wsUnit.Cell(5, 10).Value = $"{(comparison.PassRateDelta >= 0 ? "+" : "")}{comparison.PassRateDelta:F1}%";
                    wsUnit.Cell(5, 10).Style.Font.SetBold();

                    // Báº£ng chi tiáº¿t theo tá»«ng Äáº¡i Ä‘á»™i
                    wsUnit.Cell(7, 1).Value = "Báº¢NG SO SÃNH THEO Tá»ªNG Äáº I Äá»˜I & ÄÆ N Vá»Š";
                    wsUnit.Range(7, 1, 7, 10).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#334155")).Font.SetFontColor(XLColor.White);

                    string[] unitHeaders = { "ÄÆ¡n vá»‹", "QuÃ¢n sá»‘", "% Äáº¡t Äá»£t 1", "% Äáº¡t Äá»£t 2", "ChÃªnh lá»‡ch (Delta)", "% Giá»i/KhÃ¡ Äá»£t 1", "% Giá»i/KhÃ¡ Äá»£t 2", "TÄƒng trÆ°á»Ÿng (â–²)", "Giá»¯ nguyÃªn (â€”)", "Thá»¥t lÃ¹i (â–¼)" };
                    for (int i = 0; i < unitHeaders.Length; i++)
                    {
                        var cell = wsUnit.Cell(8, i + 1);
                        cell.Value = unitHeaders[i];
                        cell.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#0F766E")).Font.SetFontColor(XLColor.White).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int uRow = 9;
                    foreach (var u in comparison.UnitComparisons)
                    {
                        wsUnit.Cell(uRow, 1).Value = u.UnitName;
                        wsUnit.Cell(uRow, 2).Value = u.TotalCadets;
                        wsUnit.Cell(uRow, 3).Value = $"{u.BaselinePassRate:F1}%";
                        wsUnit.Cell(uRow, 4).Value = $"{u.ComparePassRate:F1}%";

                        var deltaCell = wsUnit.Cell(uRow, 5);
                        deltaCell.Value = $"{(u.PassRateDelta >= 0 ? "+" : "")}{u.PassRateDelta:F1}%";
                        deltaCell.Style.Font.SetBold().Font.SetFontColor(u.PassRateDelta > 0 ? XLColor.FromHtml("#16A34A") : (u.PassRateDelta < 0 ? XLColor.FromHtml("#DC2626") : XLColor.FromHtml("#64748B")));

                        wsUnit.Cell(uRow, 6).Value = $"{u.BaselineExcellentRate:F1}%";
                        wsUnit.Cell(uRow, 7).Value = $"{u.CompareExcellentRate:F1}%";
                        wsUnit.Cell(uRow, 8).Value = u.GrowthCadetsCount;
                        wsUnit.Cell(uRow, 9).Value = u.UnchangedCadetsCount;
                        wsUnit.Cell(uRow, 10).Value = u.RegressionCadetsCount;

                        if (uRow % 2 == 0)
                        {
                            wsUnit.Range(uRow, 1, uRow, 10).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
                        }
                        uRow++;
                    }

                    wsUnit.Columns().AdjustToContents();

                    // ================= Sheet 2: Cáº¥p Lá»›p & PhÃ¢n Äá»™i =================
                    var wsClass = workbook.Worksheets.Add("2. Cáº¥p Lá»›p & PhÃ¢n Äá»™i");
                    wsClass.ShowGridLines = true;

                    wsClass.Cell(1, 1).Value = "Báº¢NG SO SÃNH THÃ€NH TÃCH THEO Lá»šP & TIá»‚U Äá»˜I";
                    wsClass.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] classHeaders = { "Lá»›p / Tiá»ƒu Ä‘á»™i", "Äáº¡i Ä‘á»™i", "Thá»© háº¡ng", "QuÃ¢n sá»‘", "% Äáº¡t Äá»£t 1", "% Äáº¡t Äá»£t 2", "ChÃªnh lá»‡ch (Delta)", "TÄƒng (â–²)", "Giáº£m (â–¼)" };
                    for (int i = 0; i < classHeaders.Length; i++)
                    {
                        var cell = wsClass.Cell(3, i + 1);
                        cell.Value = classHeaders[i];
                        cell.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A")).Font.SetFontColor(XLColor.White).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int clRow = 4;
                    foreach (var c in comparison.ClassComparisons)
                    {
                        wsClass.Cell(clRow, 1).Value = c.ClassName;
                        wsClass.Cell(clRow, 2).Value = c.Unit;
                        wsClass.Cell(clRow, 3).Value = $"Háº¡ng {c.RankInUnit}";
                        wsClass.Cell(clRow, 4).Value = c.TotalCadets;
                        wsClass.Cell(clRow, 5).Value = $"{c.BaselinePassRate:F1}%";
                        wsClass.Cell(clRow, 6).Value = $"{c.ComparePassRate:F1}%";
                        
                        var dCell = wsClass.Cell(clRow, 7);
                        dCell.Value = $"{(c.PassRateDelta >= 0 ? "+" : "")}{c.PassRateDelta:F1}%";
                        dCell.Style.Font.SetBold().Font.SetFontColor(c.PassRateDelta > 0 ? XLColor.FromHtml("#16A34A") : (c.PassRateDelta < 0 ? XLColor.FromHtml("#DC2626") : XLColor.FromHtml("#64748B")));

                        wsClass.Cell(clRow, 8).Value = c.GrowthCadetsCount;
                        wsClass.Cell(clRow, 9).Value = c.RegressionCadetsCount;

                        if (clRow % 2 == 1)
                        {
                            wsClass.Range(clRow, 1, clRow, 9).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
                        }
                        clRow++;
                    }

                    wsClass.Columns().AdjustToContents();

                    // ================= Sheet 3: Chi Tiáº¿t CÃ¡ NhÃ¢n Há»c ViÃªn =================
                    var wsCadet = workbook.Worksheets.Add("3. Chi Tiáº¿t CÃ¡ NhÃ¢n");
                    wsCadet.ShowGridLines = true;

                    wsCadet.Cell(1, 1).Value = "Báº¢NG CHI TIáº¾T BIáº¾N Äá»˜NG THÃ€NH TÃCH Tá»ªNG CÃ NHÃ‚N Há»ŒC VIÃŠN";
                    wsCadet.Range(1, 1, 1, 12).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] cadetHeaders = { "STT", "MÃ£ HV", "Há» vÃ  tÃªn", "Cáº¥p báº­c", "ÄÆ¡n vá»‹", "Lá»›p", "MÃ´n kiá»ƒm tra", "Äiá»ƒm Äá»£t 1", "Äiá»ƒm Äá»£t 2", "ChÃªnh lá»‡ch (Delta)", "Xáº¿p loáº¡i Äá»£t 1", "Xáº¿p loáº¡i Äá»£t 2", "Xu hÆ°á»›ng" };
                    for (int i = 0; i < cadetHeaders.Length; i++)
                    {
                        var cell = wsCadet.Cell(3, i + 1);
                        cell.Value = cadetHeaders[i];
                        cell.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#15803D")).Font.SetFontColor(XLColor.White).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int cadRow = 4;
                    int stt = 1;
                    foreach (var cadet in comparison.CadetTrends)
                    {
                        if (!cadet.SubjectTrends.Any())
                        {
                            wsCadet.Cell(cadRow, 1).Value = stt++;
                            wsCadet.Cell(cadRow, 2).Value = cadet.CadetCode;
                            wsCadet.Cell(cadRow, 3).Value = cadet.FullName;
                            wsCadet.Cell(cadRow, 4).Value = cadet.Rank;
                            wsCadet.Cell(cadRow, 5).Value = cadet.Unit;
                            wsCadet.Cell(cadRow, 6).Value = cadet.ClassName;
                            wsCadet.Cell(cadRow, 7).Value = "ChÆ°a cÃ³ mÃ´n so sÃ¡nh";
                            wsCadet.Cell(cadRow, 11).Value = cadet.OverallBaselineGrade;
                            wsCadet.Cell(cadRow, 12).Value = cadet.OverallCompareGrade;
                            wsCadet.Cell(cadRow, 13).Value = cadet.OverallTrendText;
                            cadRow++;
                            continue;
                        }

                        foreach (var sub in cadet.SubjectTrends)
                        {
                            wsCadet.Cell(cadRow, 1).Value = stt++;
                            wsCadet.Cell(cadRow, 2).Value = cadet.CadetCode;
                            wsCadet.Cell(cadRow, 3).Value = cadet.FullName;
                            wsCadet.Cell(cadRow, 4).Value = cadet.Rank;
                            wsCadet.Cell(cadRow, 5).Value = cadet.Unit;
                            wsCadet.Cell(cadRow, 6).Value = cadet.ClassName;
                            wsCadet.Cell(cadRow, 7).Value = sub.SubjectName;
                            wsCadet.Cell(cadRow, 8).Value = $"{sub.BaselineScore} {sub.Unit}";
                            wsCadet.Cell(cadRow, 9).Value = $"{sub.CompareScore} {sub.Unit}";
                            
                            var cdCell = wsCadet.Cell(cadRow, 10);
                            cdCell.Value = $"{(sub.ScoreDelta >= 0 ? "+" : "")}{sub.ScoreDelta} {sub.Unit}";
                            cdCell.Style.Font.SetBold();

                            wsCadet.Cell(cadRow, 11).Value = sub.BaselineGrade;
                            wsCadet.Cell(cadRow, 12).Value = sub.CompareGrade;

                            var trendCell = wsCadet.Cell(cadRow, 13);
                            trendCell.Value = $"{sub.TrendSymbol} {sub.TrendText}";
                            trendCell.Style.Font.SetBold().Font.SetFontColor(sub.Trend == TrendDirection.Growth ? XLColor.FromHtml("#16A34A") : (sub.Trend == TrendDirection.Regression ? XLColor.FromHtml("#DC2626") : XLColor.FromHtml("#D97706")));

                            if (cadRow % 2 == 1)
                            {
                                wsCadet.Range(cadRow, 1, cadRow, 13).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
                            }
                            cadRow++;
                        }
                    }

                    wsCadet.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                    return (true, $"Xuáº¥t bÃ¡o cÃ¡o phÃ¢n tÃ­ch Ä‘á»‘i soÃ¡t Ä‘á»£t thi thÃ nh cÃ´ng ra file: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    return (false, $"Lá»—i khi xuáº¥t file Excel: {ex.Message}");
                }
            });
        }
        #endregion

        #region 9. BÃO CÃO Tá»”NG QUAN & Äá»€ XUáº¤T HUáº¤N LUYá»†N AI
        public async Task<(bool Success, string Message)> ExportDashboardExecutiveReportAsync(
            string filePath,
            QL_HocVien.Models.DTOs.DashboardSummaryDto summary,
            IEnumerable<QL_HocVien.Models.DTOs.UnitLeaderboardDto> units,
            IEnumerable<QL_HocVien.Models.DTOs.SubjectPerformanceDto> subjects,
            QL_HocVien.Models.DTOs.TrainingRecommendationSummaryDto aiRecommendations,
            IEnumerable<PhysicalExamRecord> failedRecords,
            IEnumerable<QL_HocVien.Models.DTOs.CadetHonorDto> honoredCadets)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook();

                    // ==========================================
                    // SHEET 1: Tá»”NG QUAN & CHá»ˆ TIÃŠU ÄÆ N Vá»Š
                    // ==========================================
                    var ws1 = workbook.Worksheets.Add("Tá»•ng Quan & Thi Äua");
                    ws1.Cell("A1").Value = "BÃO CÃO Tá»”NG QUAN CHá»ˆ Äáº O HUáº¤N LUYá»†N & RÃˆN LUYá»†N THá»‚ Lá»°C";
                    ws1.Range("A1:G1").Merge().Style
                        .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#0F172A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    ws1.Cell("A2").Value = $"NgÃ y xuáº¥t bÃ¡o cÃ¡o: {DateTime.Now:dd/MM/yyyy HH:mm} - Há»‡ Thá»‘ng Quáº£n LÃ½ Há»c ViÃªn QuÃ¢n Äá»™i";
                    ws1.Range("A2:G2").Merge().Style
                        .Font.SetItalic().Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#64748B"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    // Khá»‘i KPI Tá»•ng thá»ƒ
                    ws1.Cell("A4").Value = "I. CHá»ˆ Sá» RÃˆN LUYá»†N Tá»”NG THá»‚";
                    ws1.Range("A4:G4").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));

                    string[] kpiHeaders = { "Tá»•ng QuÃ¢n Sá»‘", "LÆ°á»£t Kiá»ƒm Tra", "Tá»· Lá»‡ Äáº¡t Chuáº©n", "Tá»· Lá»‡ Giá»i/XS", "ChÆ°a Äáº¡t Chuáº©n", "ÄÃ¡nh GiÃ¡ ToÃ n Viá»‡n" };
                    for (int i = 0; i < kpiHeaders.Length; i++)
                    {
                        var cell = ws1.Cell(5, i + 1);
                        cell.Value = kpiHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    ws1.Cell(6, 1).Value = $"{summary.TotalCadets} Ä‘/c";
                    ws1.Cell(6, 2).Value = $"{summary.TotalExamRecords} lÆ°á»£t";
                    ws1.Cell(6, 3).Value = $"{summary.OverallPassRate:F1}%";
                    ws1.Cell(6, 4).Value = $"{summary.EliteRate:F1}%";
                    ws1.Cell(6, 5).Value = $"{summary.FailCount} lÆ°á»£t ({summary.FailRate:F1}%)";
                    ws1.Cell(6, 6).Value = summary.OverallRatingLabel;

                    ws1.Range("A6:F6").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                                            .Font.SetBold().Font.SetFontSize(11)
                                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                            .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    // Báº£ng Xáº¿p háº¡ng Äáº¡i Ä‘á»™i
                    ws1.Cell("A8").Value = "II. Báº¢NG Xáº¾P Háº NG THI ÄUA RÃˆN LUYá»†N CÃC Äáº I Äá»˜I / ÄÆ N Vá»Š";
                    ws1.Range("A8:G8").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#15803D"));

                    string[] unitHeaders = { "Háº¡ng", "ÄÆ¡n Vá»‹ / Äáº¡i Äá»™i", "QuÃ¢n Sá»‘", "LÆ°á»£t Kiá»ƒm Tra", "Tá»· Lá»‡ Äáº¡t (%)", "Tá»· Lá»‡ Giá»i (%)", "Xáº¿p Loáº¡i ÄÆ¡n Vá»‹" };
                    for (int i = 0; i < unitHeaders.Length; i++)
                    {
                        var cell = ws1.Cell(9, i + 1);
                        cell.Value = unitHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#15803D"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int uRow = 10;
                    foreach (var u in units)
                    {
                        ws1.Cell(uRow, 1).Value = u.RankMedal;
                        ws1.Cell(uRow, 2).Value = u.UnitName;
                        ws1.Cell(uRow, 3).Value = u.TotalCadets;
                        ws1.Cell(uRow, 4).Value = u.TotalExamRecords;
                        ws1.Cell(uRow, 5).Value = $"{u.PassRate:F1}%";
                        ws1.Cell(uRow, 6).Value = $"{u.EliteRate:F1}%";
                        ws1.Cell(uRow, 7).Value = u.EvaluationStatus;

                        ws1.Range(uRow, 1, uRow, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                        if (uRow % 2 == 1) ws1.Range(uRow, 1, uRow, 7).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
                        ws1.Cell(uRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws1.Cell(uRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws1.Cell(uRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws1.Cell(uRow, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws1.Cell(uRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        uRow++;
                    }

                    // Báº£ng Äá»™ khÃ³ MÃ´n thá»ƒ lá»±c
                    int sHeaderRow = uRow + 2;
                    ws1.Cell(sHeaderRow, 1).Value = "III. PHÃ‚N TÃCH Tá»¶ Lá»† Äáº T & Äá»˜ KHÃ“ CÃC MÃ”N KIá»‚M TRA";
                    ws1.Range(sHeaderRow, 1, sHeaderRow, 6).Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#B45309"));

                    string[] subHeaders = { "MÃ£ MÃ´n", "TÃªn MÃ´n Kiá»ƒm Tra", "Tá»•ng LÆ°á»£t Thi", "Tá»· Lá»‡ Äáº¡t (%)", "Tá»· Lá»‡ ChÆ°a Äáº¡t (%)", "Má»©c Äá»™ Rá»§i Ro / ÄÃ¡nh GiÃ¡" };
                    for (int i = 0; i < subHeaders.Length; i++)
                    {
                        var cell = ws1.Cell(sHeaderRow + 1, i + 1);
                        cell.Value = subHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#D97706"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int sRow = sHeaderRow + 2;
                    foreach (var s in subjects)
                    {
                        ws1.Cell(sRow, 1).Value = s.SubjectCode;
                        ws1.Cell(sRow, 2).Value = s.SubjectName;
                        ws1.Cell(sRow, 3).Value = s.TotalTested;
                        ws1.Cell(sRow, 4).Value = $"{s.PassRate:F1}%";
                        ws1.Cell(sRow, 5).Value = $"{s.FailRate:F1}%";
                        ws1.Cell(sRow, 6).Value = s.DifficultyLevel;

                        ws1.Range(sRow, 1, sRow, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                        if (sRow % 2 == 1) ws1.Range(sRow, 1, sRow, 6).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
                        ws1.Cell(sRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws1.Cell(sRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws1.Cell(sRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws1.Cell(sRow, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        sRow++;
                    }

                    ws1.Columns().AdjustToContents();

                    // ==========================================
                    // SHEET 2: Äá»€ XUáº¤T HUáº¤N LUYá»†N AI THÃ”NG MINH
                    // ==========================================
                    var ws2 = workbook.Worksheets.Add("Äá» Xuáº¥t Huáº¥n Luyá»‡n AI");
                    ws2.Cell("A1").Value = "CHá»ˆ Äáº O & PHÃC Äá»’ HUáº¤N LUYá»†N THá»‚ Lá»°C QUÃ‚N Äá»˜I THÃ”NG MINH";
                    ws2.Range("A1:G1").Merge().Style
                        .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#4338CA"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    ws2.Cell("A3").Value = "1. ÄÃNH GIÃ CHIáº¾N LÆ¯á»¢C & Äá»€ XUáº¤T PHÃ‚N Bá»” THá»œI GIAN:";
                    ws2.Range("A3:G3").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));

                    ws2.Cell("A4").Value = aiRecommendations.StrategicDirective.ExecutiveSummary;
                    ws2.Range("A4:G4").Merge().Style.Font.SetItalic().Font.SetFontSize(11);

                    ws2.Cell("A5").Value = $"â€¢ PhÃ¢n bá»• quá»¹ thá»i gian: {aiRecommendations.StrategicDirective.TimeAllocationDirective}";
                    ws2.Range("A5:G5").Merge().Style.Font.SetFontSize(11);

                    ws2.Cell("A6").Value = $"â€¢ Phá»¥c há»“i & dinh dÆ°á»¡ng: {aiRecommendations.StrategicDirective.RecoveryAndNutritionAdvice}";
                    ws2.Range("A6:G6").Merge().Style.Font.SetFontSize(11);

                    ws2.Cell("A8").Value = "2. PHÃC Äá»’ HUáº¤N LUYá»†N CHUYÃŠN SÃ‚U THEO Tá»ªNG NHÃ“M Tá» CHáº¤T THá»‚ Lá»°C QUÃ‚N Sá»°:";
                    ws2.Range("A8:G8").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#4338CA"));

                    string[] aiHeaders = { "NhÃ³m Tá»‘ Cháº¥t Thá»ƒ Lá»±c", "Ná»™i Dung MÃ´n", "Tá»· Lá»‡ ChÆ°a Äáº¡t", "Má»©c Äá»™ Æ¯u TiÃªn", "PhÃ¢n TÃ­ch Äiá»ƒm Ngháº½n Ká»¹ Thuáº­t", "PhÃ¡c Äá»“ BÃ i Táº­p Khoa Há»c", "Lá»‹ch Huáº¥n Luyá»‡n Tuáº§n" };
                    for (int i = 0; i < aiHeaders.Length; i++)
                    {
                        var cell = ws2.Cell(9, i + 1);
                        cell.Value = aiHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#4338CA"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int aiRow = 10;
                    foreach (var c in aiRecommendations.ComponentPrescriptions)
                    {
                        ws2.Cell(aiRow, 1).Value = c.ComponentName;
                        ws2.Cell(aiRow, 2).Value = c.TargetSubjects;
                        ws2.Cell(aiRow, 3).Value = $"{c.FailRate:F1}% ({c.AffectedCadetsCount} Ä‘/c)";
                        ws2.Cell(aiRow, 4).Value = c.UrgencyLevel;
                        ws2.Cell(aiRow, 5).Value = c.CoreWeaknessAnalysis;
                        ws2.Cell(aiRow, 6).Value = c.ScientificTrainingProtocol;
                        ws2.Cell(aiRow, 7).Value = c.WeeklyScheduleRecommendation;

                        ws2.Range(aiRow, 1, aiRow, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                           .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                        ws2.Cell(aiRow, 1).Style.Font.SetBold();
                        ws2.Cell(aiRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(aiRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold();

                        if (aiRow % 2 == 1) ws2.Range(aiRow, 1, aiRow, 7).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F5F3FF"));
                        aiRow++;
                    }

                    ws2.Columns().AdjustToContents();

                    // ==========================================
                    // SHEET 3: DANH SÃCH Cáº¦N Bá»’I DÆ¯á» NG THá»‚ Lá»°C
                    // ==========================================
                    var ws3 = workbook.Worksheets.Add("DS Cáº§n Bá»“i DÆ°á»¡ng Thá»ƒ Lá»±c");
                    ws3.Cell("A1").Value = "DANH SÃCH Há»ŒC VIÃŠN CHÆ¯A Äáº T CHUáº¨N - ÄÆ¯A VÃ€O Káº¾ HOáº CH Bá»’I DÆ¯á» NG Cáº¤P Tá»C";
                    ws3.Range("A1:H1").Merge().Style
                        .Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#B91C1C"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] failHeaders = { "STT", "MÃ£ HV", "Há» vÃ  TÃªn", "ÄÆ¡n Vá»‹", "Lá»›p", "Ná»™i Dung ChÆ°a Äáº¡t", "ThÃ nh TÃ­ch", "PhÃ¡c Äá»“ Bá»“i DÆ°á»¡ng & Thá»i Háº¡n" };
                    for (int i = 0; i < failHeaders.Length; i++)
                    {
                        var cell = ws3.Cell(3, i + 1);
                        cell.Value = failHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#DC2626"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int fRow = 4;
                    int fStt = 1;
                    foreach (var fr in failedRecords)
                    {
                        ws3.Cell(fRow, 1).Value = fStt++;
                        ws3.Cell(fRow, 2).Value = fr.Cadet?.CadetCode ?? "";
                        ws3.Cell(fRow, 3).Value = fr.Cadet?.FullName ?? "";
                        ws3.Cell(fRow, 4).Value = fr.Cadet?.Unit ?? "";
                        ws3.Cell(fRow, 5).Value = fr.Cadet?.ClassName ?? (fr.Cadet?.MilitaryClass?.ClassName ?? "");
                        ws3.Cell(fRow, 6).Value = fr.Subject?.SubjectName ?? "";
                        ws3.Cell(fRow, 7).Value = fr.ScoreValue;
                        ws3.Cell(fRow, 8).Value = "Táº­p bá»• trá»£ thá»ƒ lá»±c chuyÃªn biá»‡t; kiá»ƒm tra sÃ¡t háº¡ch láº¡i sau 30 ngÃ y";

                        ws3.Range(fRow, 1, fRow, 8).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                         .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                        if (fRow % 2 == 1) ws3.Range(fRow, 1, fRow, 8).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEF2F2"));
                        ws3.Cell(fRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws3.Cell(fRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws3.Cell(fRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        fRow++;
                    }

                    ws3.Columns().AdjustToContents();

                    // ==========================================
                    // SHEET 4: BIá»‚U DÆ¯Æ NG Há»ŒC VIÃŠN XUáº¤T Sáº®C
                    // ==========================================
                    var ws4 = workbook.Worksheets.Add("DS Biá»ƒu DÆ°Æ¡ng Khen ThÆ°á»Ÿng");
                    ws4.Cell("A1").Value = "Báº¢NG VÃ€NG BIá»‚U DÆ¯Æ NG Há»ŒC VIÃŠN RÃˆN LUYá»†N THá»‚ Lá»°C XUáº¤T Sáº®C & KIá»†N TÆ¯á»šNG";
                    ws4.Range("A1:H1").Merge().Style
                        .Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#15803D"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] honorHeaders = { "STT", "MÃ£ HV", "Há» vÃ  TÃªn", "Cáº¥p Báº­c", "ÄÆ¡n Vá»‹", "Lá»›p", "Danh Hiá»‡u Biá»ƒu DÆ°Æ¡ng", "Ná»™i Dung TiÃªu Biá»ƒu" };
                    for (int i = 0; i < honorHeaders.Length; i++)
                    {
                        var cell = ws4.Cell(3, i + 1);
                        cell.Value = honorHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#16A34A"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int hRow = 4;
                    int hStt = 1;
                    foreach (var h in honoredCadets)
                    {
                        ws4.Cell(hRow, 1).Value = hStt++;
                        ws4.Cell(hRow, 2).Value = h.CadetCode;
                        ws4.Cell(hRow, 3).Value = h.FullName;
                        ws4.Cell(hRow, 4).Value = h.Rank;
                        ws4.Cell(hRow, 5).Value = h.Unit;
                        ws4.Cell(hRow, 6).Value = h.ClassName;
                        ws4.Cell(hRow, 7).Value = h.HonorTitle;
                        ws4.Cell(hRow, 8).Value = $"{h.BestSubject} ({h.BestScore})";

                        ws4.Range(hRow, 1, hRow, 8).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                         .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                        if (hRow % 2 == 1) ws4.Range(hRow, 1, hRow, 8).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F0FDF4"));
                        ws4.Cell(hRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws4.Cell(hRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws4.Cell(hRow, 7).Style.Font.SetBold().Font.SetFontColor(XLColor.FromHtml("#15803D"));
                        hRow++;
                    }

                    ws4.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                    return (true, $"Xuáº¥t bÃ¡o cÃ¡o tá»•ng quan & Ä‘á» xuáº¥t huáº¥n luyá»‡n AI thÃ nh cÃ´ng: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    return (false, $"Lá»—i xuáº¥t bÃ¡o cÃ¡o tá»•ng quan: {ex.Message}");
                }
            });
        }
        #endregion
    }
}

