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
                return (false, $"[BẢO MẬT] Từ chối tập tin '{result.FileName}': {result.Message}");
            }
            return (true, string.Empty);
        }

        private static string CleanCellText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var sanitized = text.Replace("\r", " ").Replace("\n", " ");
            return Regex.Replace(sanitized, @"\s+", " ").Trim();
        }

        private static string ExtractFallbackUnit(string? sheetName, string? filePath)
        {
            try
            {
                // 1. Phân tích tên Sheet nếu chỉ rõ đơn vị
                if (!string.IsNullOrWhiteSpace(sheetName))
                {
                    var s = sheetName.Trim();
                    var sLower = s.ToLowerInvariant();
                    if (!sLower.StartsWith("sheet") && !sLower.Contains("danh sách") && !sLower.Contains("học viên") && !sLower.Contains("cadet") && !sLower.Contains("trang"))
                    {
                        return s;
                    }
                    if (sLower.Contains("tiểu đoàn") || sLower.Contains("đại đội") || sLower.Contains("trung đội") ||
                        Regex.IsMatch(sLower, @"\b[dcbn]\d+\b", RegexOptions.IgnoreCase))
                    {
                        return s;
                    }
                }

                // 2. Phân tích tên file nếu có chứa mã đơn vị (VD: HocVien_d1_c2_b1_n1.xlsx hoặc DanhSach_c2.xlsx)
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var match = Regex.Match(fileName, @"(d\d+[_/.-]c\d+([_/.-]b\d+)?([_/.-]n\d+)?|tiểu\s*đoàn\s*\d+|đại\s*đội\s*\d+|trung\s*đội\s*\d+|\b[dcbn]\d+\b)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return match.Value.Replace('_', '/').Replace('-', '/');
                    }
                }
            }
            catch { }

            return "Đại đội 1";
        }

        #region 1. XUẤT & NHẬP HỌC VIÊN
        public async Task<(bool Success, string Message)> ExportCadetsToExcelAsync(IEnumerable<Cadet> cadets, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Danh sách học viên");

                // Tiêu đề
                ws.Cell(1, 1).Value = "DANH SÁCH HỌC VIÊN - HỌC VIỆN QUÂN SỰ";
                ws.Range(1, 1, 1, 12).Merge().Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(14)
                    .Font.SetFontColor(XLColor.FromHtml("#0F766E"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range(2, 1, 2, 12).Merge().Style
                    .Font.SetItalic(true)
                    .Font.SetFontSize(10)
                    .Font.SetFontColor(XLColor.FromHtml("#64748B"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Header
                string[] headers = { "STT", "Mã học viên", "Họ và tên", "Cấp bậc", "Chức vụ", "Đơn vị", "Lớp", "Số điện thoại", "Email", "Ngày sinh", "Tuổi", "Giới tính" };
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
                return (true, $"Đã xuất thành công {stt - 1} học viên ra tệp Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xuất tệp Excel: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<Cadet> Cadets)> ImportCadetsFromExcelAsync(string filePath)
        {
            var importedList = new List<Cadet>();
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tệp Excel không tồn tại.", importedList);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, importedList);

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("học viên") || w.Name.Contains("Cadet"))
                         ?? workbook.Worksheets.FirstOrDefault();

                if (ws == null)
                    return (false, "Không tìm thấy sheet chứa dữ liệu học viên.", importedList);

                // Tìm hàng header (có chứa 'Mã' hoặc 'Họ và tên')
                int headerRow = 1;
                for (int r = 1; r <= 15; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => CleanCellText(c.GetString())));
                    if (textRow.Contains("Họ và tên", StringComparison.OrdinalIgnoreCase) || 
                        textRow.Contains("Mã học viên", StringComparison.OrdinalIgnoreCase) || 
                        textRow.Contains("Họ tên", StringComparison.OrdinalIgnoreCase) || 
                        textRow.Contains("CadetCode", StringComparison.OrdinalIgnoreCase) ||
                        textRow.Contains("Họ và Tên", StringComparison.OrdinalIgnoreCase) ||
                        textRow.Contains("Họ và chữ lót", StringComparison.OrdinalIgnoreCase))
                    {
                        headerRow = r;
                        break;
                    }
                }

                string fallbackUnit = ExtractFallbackUnit(ws.Name, filePath);

                // Dynamic Header-Based Mapping: Tự động phát hiện chỉ số cột theo tiêu đề ô
                int colCode = -1, colFullName = -1, colRank = -1, colPosition = -1, colUnit = -1;
                int colClassName = -1, colPhone = -1, colEmail = -1, colDob = -1, colAge = -1, colGender = -1;

                foreach (var cell in ws.Row(headerRow).CellsUsed())
                {
                    var title = CleanCellText(cell.GetString()).ToLowerInvariant().Trim();
                    int c = cell.Address.ColumnNumber;

                    if (title.Contains("mã") || title.Contains("cadetcode") || title.Contains("số hiệu") || 
                        title.Contains("shsv") || title.Contains("mshv") || title.Contains("mahocvien") || 
                        title.Contains("ma hv") || title.Equals("ms") || title.Equals("id"))
                    {
                        if (colCode == -1) colCode = c;
                    }
                    else if (title.Contains("họ") || title.Contains("tên") || title.Contains("fullname") || title.Contains("hoten"))
                    {
                        if (colFullName == -1) colFullName = c;
                    }
                    else if (title.Contains("cấp bậc") || title.Contains("quân hàm") || title.Contains("cap bac") || title.Contains("quan ham") || title.Equals("rank") || title.Contains("bậc"))
                    {
                        if (colRank == -1) colRank = c;
                    }
                    else if (title.Contains("chức vụ") || title.Contains("chức danh") || title.Contains("chuc vu") || title.Contains("nhiệm vụ") || title.Equals("position"))
                    {
                        if (colPosition == -1) colPosition = c;
                    }
                    else if (title.Contains("đơn vị") || title.Contains("đại đội") || title.Contains("trung đội") || 
                             title.Contains("tiểu đoàn") || title.Contains("nhóm") || title.Contains("tổ") || 
                             title.Contains("phân đội") || title.Contains("don vi") || title.Equals("unit") || title.Contains("bộ phận"))
                    {
                        if (colUnit == -1) colUnit = c;
                    }
                    else if (title.Contains("lớp") || title.Contains("lop") || title.Equals("class") || title.Contains("classname"))
                    {
                        if (colClassName == -1) colClassName = c;
                    }
                    else if (title.Contains("thoại") || title.Contains("sđt") || title.Contains("sdt") || 
                             title.Contains("phone") || title.Contains("tel") || title.Contains("mobile") || title.Contains("liên hệ") || title.Contains("dienthoai"))
                    {
                        if (colPhone == -1) colPhone = c;
                    }
                    else if (title.Contains("mail"))
                    {
                        if (colEmail == -1) colEmail = c;
                    }
                    else if (title.Contains("năm sinh") || title.Contains("ngày sinh") || title.Contains("sinh") || 
                             title.Contains("dob") || title.Contains("birth") || title.Contains("nam sinh") || title.Contains("ngay sinh") || title.Equals("ns"))
                    {
                        if (colDob == -1) colDob = c;
                    }
                    else if (title.Contains("tuổi") || title.Contains("tuoi") || title.Equals("age"))
                    {
                        if (colAge == -1) colAge = c;
                    }
                    else if (title.Contains("giới tính") || title.Contains("nam/nữ") || title.Contains("gioi tinh") || 
                             title.Contains("phái") || title.Equals("gender") || title.Equals("sex"))
                    {
                        if (colGender == -1) colGender = c;
                    }
                }

                // Fallback nếu không xác định được vị trí họ tên
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
                    // Đọc các cột theo mapping động và làm sạch khoảng trắng/xuống dòng
                    string code = colCode > 0 ? CleanCellText(row.Cell(colCode).GetString()) : string.Empty;
                    string fullName = colFullName > 0 ? CleanCellText(row.Cell(colFullName).GetString()) : string.Empty;

                    // Nếu họ tên rỗng thì bỏ qua
                    if (string.IsNullOrWhiteSpace(fullName)) continue;
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        code = $"HV-{DateTime.Today.Year}-{r:D3}";
                    }

                    string rank = colRank > 0 ? CleanCellText(row.Cell(colRank).GetString()) : string.Empty;
                    string pos = colPosition > 0 ? CleanCellText(row.Cell(colPosition).GetString()) : string.Empty;
                    string unit = colUnit > 0 ? CleanCellText(row.Cell(colUnit).GetString()) : string.Empty;
                    if (string.IsNullOrWhiteSpace(unit))
                    {
                        unit = fallbackUnit;
                    }

                    string className = colClassName > 0 ? CleanCellText(row.Cell(colClassName).GetString()) : string.Empty;
                    string phone = colPhone > 0 ? CleanCellText(row.Cell(colPhone).GetString()) : string.Empty;
                    string email = colEmail > 0 ? CleanCellText(row.Cell(colEmail).GetString()) : string.Empty;

                    // Xử lý Ngày/Năm sinh & Tuổi thông minh
                    DateTime? dob = null;
                    int age = 0;
                    if (colAge > 0)
                    {
                        int.TryParse(CleanCellText(row.Cell(colAge).GetString()), out age);
                    }

                    if (colDob > 0)
                    {
                        var cellDob = row.Cell(colDob);
                        if (cellDob.DataType == XLDataType.DateTime)
                        {
                            try { dob = cellDob.GetDateTime(); } catch { }
                        }
                        else if (cellDob.DataType == XLDataType.Number)
                        {
                            double val = cellDob.GetDouble();
                            if (val >= 1940 && val <= DateTime.Today.Year + 5)
                            {
                                int y = (int)val;
                                dob = new DateTime(y, 1, 1);
                                if (age <= 0) age = DateTime.Today.Year - y;
                            }
                            else if (val > 20000 && val < 70000)
                            {
                                try { dob = DateTime.FromOADate(val); } catch { }
                            }
                        }

                        if (!dob.HasValue)
                        {
                            string dobStr = CleanCellText(cellDob.GetString());
                            if (!string.IsNullOrWhiteSpace(dobStr))
                            {
                                if (int.TryParse(dobStr, out int y) && y >= 1940 && y <= DateTime.Today.Year + 5)
                                {
                                    dob = new DateTime(y, 1, 1);
                                    if (age <= 0) age = DateTime.Today.Year - y;
                                }
                                else
                                {
                                    string[] dateFormats = { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "dd.MM.yyyy" };
                                    if (DateTime.TryParseExact(dobStr, dateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedExact))
                                    {
                                        dob = parsedExact;
                                    }
                                    else if (DateTime.TryParse(dobStr, new System.Globalization.CultureInfo("vi-VN"), System.Globalization.DateTimeStyles.None, out var parsedVi))
                                    {
                                        dob = parsedVi;
                                    }
                                    else if (DateTime.TryParse(dobStr, out var parsedGen))
                                    {
                                        dob = parsedGen;
                                    }
                                }
                            }
                        }
                    }

                    if (dob.HasValue && age <= 0)
                    {
                        age = DateTime.Today.Year - dob.Value.Year;
                        if (DateTime.Today < dob.Value.AddYears(age))
                        {
                            age--;
                        }
                    }
                    if (age <= 0) age = 21;

                    // Chuẩn hóa Giới tính
                    string rawGender = colGender > 0 ? CleanCellText(row.Cell(colGender).GetString()) : "";
                    string gender = "Nam";
                    if (!string.IsNullOrWhiteSpace(rawGender))
                    {
                        var g = rawGender.ToLowerInvariant().Trim();
                        if (g.Contains("nữ") || g.Contains("nu") || g == "f" || g == "female" || g == "gái")
                        {
                            gender = "Nữ";
                        }
                        else
                        {
                            gender = "Nam";
                        }
                    }

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
                            Rank = !string.IsNullOrWhiteSpace(rank) ? rank : "Binh nhì",
                            Position = !string.IsNullOrWhiteSpace(pos) ? pos : "Học viên",
                            Unit = !string.IsNullOrWhiteSpace(unit) ? unit : fallbackUnit,
                            ClassId = matchedClass?.Id,
                            ClassName = matchedClass?.ClassName ?? (!string.IsNullOrWhiteSpace(className) ? className : "K26A"),
                            PhoneNumber = !string.IsNullOrWhiteSpace(phone) ? phone : $"09{new Random().Next(10000000, 99999999)}",
                            Email = !string.IsNullOrWhiteSpace(email) ? email : $"{code.ToLower().Replace("-", "").Replace(" ", "")}@hocvien.edu.vn",
                            DateOfBirth = dob,
                            Age = age,
                            Gender = gender,
                            CreatedAt = DateTime.Now
                        };
                        await _cadetRepository.AddAsync(newCadet);
                        addedCount++;
                        importedList.Add(newCadet);
                    }
                }

                await _cadetRepository.SaveChangesAsync();
                return (true, $"Nhập dữ liệu thành công: Thêm mới {addedCount} học viên, Cập nhật {updatedCount} học viên.", importedList);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xử lý tệp Excel: {ex.Message}", importedList);
            }
        }
        #endregion

        #region 2. XUẤT & NHẬP MÔN HỌC
        public async Task<(bool Success, string Message)> ExportSubjectsToExcelAsync(IEnumerable<Subject> subjects, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Trang môn học");

                ws.Cell("A1").Value = "DANH MỤC MÔN HỌC & TIÊU CHUẨN RÈN LUYỆN THỂ LỰC (TT32)";
                ws.Range("A1:J1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                string[] headers = { "STT", "Mã môn", "Tên môn học", "Nhóm tố chất", "Đơn vị tính", "Chuẩn Giỏi", "Chuẩn Khá", "Chuẩn Đạt", "Càng cao càng tốt", "Mô tả" };
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
                    ws.Cell(row, 9).Value = s.IsHigherBetter ? "Có" : "Không";
                    ws.Cell(row, 10).Value = s.Description;

                    ws.Range(row, 1, row, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                   .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);

                return (true, $"Xuất thành công {stt - 1} môn học ra Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xuất môn học: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<Subject> Subjects)> ImportSubjectsFromExcelAsync(string filePath)
        {
            var list = new List<Subject>();
            try
            {
                if (!File.Exists(filePath)) return (false, "Tệp không tồn tại.", list);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, list);

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("môn") || w.Name.Contains("Subject"))
                         ?? workbook.Worksheets.FirstOrDefault();

                if (ws == null) return (false, "Không tìm thấy sheet môn học.", list);

                int headerRow = 1;
                for (int r = 1; r <= 15; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => CleanCellText(c.GetString())));
                    if (textRow.Contains("Mã môn") || textRow.Contains("Tên môn") || textRow.Contains("SubjectCode"))
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

                    if (title.Contains("mã môn") || title.Contains("subjectcode"))
                    {
                        if (colCode == -1) colCode = c;
                    }
                    else if (title.Contains("tên môn") || title.Contains("subjectname") || (title.Contains("môn") && !title.Contains("mã")))
                    {
                        if (colName == -1) colName = c;
                    }
                    else if (title.Contains("loại") || title.Contains("nhóm") || title.Contains("danh mục") || title.Contains("category"))
                    {
                        if (colCat == -1) colCat = c;
                    }
                    else if (title.Contains("đơn vị tính") || title.Contains("đvt") || title.Equals("đơn vị") || title.Equals("unit"))
                    {
                        if (colUnit == -1) colUnit = c;
                    }
                    else if (title.Contains("giỏi") || title.Contains("excellent"))
                    {
                        if (colExc == -1) colExc = c;
                    }
                    else if (title.Contains("khá") || title.Contains("good"))
                    {
                        if (colGood == -1) colGood = c;
                    }
                    else if (title.Contains("đạt") || title.Contains("pass"))
                    {
                        if (colPass == -1) colPass = c;
                    }
                    else if (title.Contains("càng cao") || title.Contains("higher"))
                    {
                        if (colHigher == -1) colHigher = c;
                    }
                    else if (title.Contains("mô tả") || title.Contains("ghi chú") || title.Contains("desc"))
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

                    string cat = colCat > 0 ? CleanCellText(row.Cell(colCat).GetString()) : "Tiêu chuẩn rèn luyện";
                    string unit = colUnit > 0 ? CleanCellText(row.Cell(colUnit).GetString()) : "Lần";
                    double exc = 0, good = 0, pass = 0;
                    if (colExc > 0) double.TryParse(CleanCellText(row.Cell(colExc).GetString()), out exc);
                    if (colGood > 0) double.TryParse(CleanCellText(row.Cell(colGood).GetString()), out good);
                    if (colPass > 0) double.TryParse(CleanCellText(row.Cell(colPass).GetString()), out pass);

                    string higher = colHigher > 0 ? CleanCellText(row.Cell(colHigher).GetString()).ToLowerInvariant() : "có";
                    bool isHigher = higher == "có" || higher == "yes" || higher == "true" || higher == "1";
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
                            Category = !string.IsNullOrWhiteSpace(cat) ? cat : "Sức mạnh",
                            Unit = !string.IsNullOrWhiteSpace(unit) ? unit : "lần",
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
                return (true, $"Nhập môn học thành công: Thêm mới {added} môn, Cập nhật {updated} môn.", list);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi nhập môn học: {ex.Message}", list);
            }
        }
        #endregion

        #region 3. XUẤT & NHẬP KIỂM TRA THỂ LỰC
        public async Task<(bool Success, string Message)> ExportExamRecordsToExcelAsync(IEnumerable<PhysicalExamRecord> records, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Kiểm tra thể lực");

                ws.Cell("A1").Value = "BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA THỂ LỰC QUÂN SỰ";
                ws.Range("A1:K1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                string[] headers = { "STT", "Mã học viên", "Họ và tên", "Đơn vị", "Lớp", "Mã môn", "Tên môn kiểm tra", "Thành tích", "Xếp loại", "Đợt kiểm tra", "Ngày kiểm tra" };
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

                return (true, $"Xuất thành công {stt - 1} lượt kiểm tra thể lực ra Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xuất kết quả kiểm tra: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<PhysicalExamRecord> Records)> ImportExamRecordsFromExcelAsync(string filePath)
        {
            var list = new List<PhysicalExamRecord>();
            try
            {
                if (!File.Exists(filePath)) return (false, "Tệp không tồn tại.", list);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, list);

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("thể lực") || w.Name.Contains("Exam"))
                         ?? workbook.Worksheets.FirstOrDefault();

                if (ws == null) return (false, "Không tìm thấy sheet kiểm tra thể lực.", list);

                int headerRow = 1;
                for (int r = 1; r <= 15; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => CleanCellText(c.GetString())));
                    if (textRow.Contains("Mã học viên") || textRow.Contains("Mã môn") || textRow.Contains("Thành tích") || textRow.Contains("CadetCode"))
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

                    if (title.Contains("mã học viên") || title.Contains("cadetcode") || title.Contains("mã hv") || (title.Contains("mã") && !title.Contains("môn")))
                    {
                        if (colCadetCode == -1) colCadetCode = c;
                    }
                    else if (title.Contains("mã môn") || title.Contains("subjectcode") || (title.Contains("môn") && title.Contains("mã")))
                    {
                        if (colSubjectCode == -1) colSubjectCode = c;
                    }
                    else if (title.Contains("thành tích") || title.Contains("điểm") || title.Contains("kết quả") || title.Contains("score"))
                    {
                        if (colScore == -1) colScore = c;
                    }
                    else if (title.Contains("đợt") || title.Contains("session") || title.Contains("kỳ kiểm tra"))
                    {
                        if (colSession == -1) colSession = c;
                    }
                    else if (title.Contains("ngày") || title.Contains("date") || title.Contains("thời gian"))
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
                    string session = colSession > 0 ? CleanCellText(row.Cell(colSession).GetString()) : "Kiểm tra định kỳ";
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
                        ExamSession = !string.IsNullOrWhiteSpace(session) ? session : "Kiểm tra định kỳ",
                        ExamDate = examDate,
                        CreatedAt = DateTime.Now
                    };

                    await _examRepository.AddAsync(record);
                    added++;
                    list.Add(record);
                }

                await _examRepository.SaveChangesAsync();
                return (true, $"Nhập thành công {added} lượt kiểm tra thể lực từ Excel.", list);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi nhập kiểm tra thể lực: {ex.Message}", list);
            }
        }
        #endregion

        #region 4. XUẤT & NHẬP LỚP HỌC
        public async Task<(bool Success, string Message)> ExportClassesToExcelAsync(IEnumerable<MilitaryClass> classes, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Trang lớp học");

                ws.Cell("A1").Value = "DANH SÁCH LỚP HỌC QUÂN ĐỘI";
                ws.Range("A1:I1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Thời điểm xuất dữ liệu: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                ws.Range("A2:I2").Merge().Style.Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                string[] headers = { "STT", "Mã lớp", "Tên lớp", "Đơn vị quản lý", "Chuyên ngành", "Cán bộ quản lý", "Khóa học", "Quân số", "Mô tả" };
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

                return (true, $"Xuất thành công {stt - 1} lớp học ra file Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xuất file Excel: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<MilitaryClass> Classes)> ImportClassesFromExcelAsync(string filePath)
        {
            var importedList = new List<MilitaryClass>();
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tệp không tồn tại.", importedList);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, importedList);

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheet("Trang lớp học") 
                    ?? workbook.Worksheets.FirstOrDefault(w => w.Name.ToLower().Contains("lớp") || w.Name.ToLower().Contains("class")) 
                    ?? workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                    return (false, "Không tìm thấy trang tính phù hợp trong tệp Excel.", importedList);

                int headerRowIndex = 1;
                for (int r = 1; r <= 10; r++)
                {
                    for (int c = 1; c <= 10; c++)
                    {
                        var val = ws.Cell(r, c).GetString().Trim().ToLower();
                        if (val.Contains("mã lớp") || val == "classcode" || val == "mã lớp học")
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
                    if (title.Contains("mã lớp")) colCode = c;
                    else if (title.Contains("tên lớp")) colName = c;
                    else if (title.Contains("đơn vị")) colUnit = c;
                    else if (title.Contains("chuyên ngành")) colMajor = c;
                    else if (title.Contains("cán bộ") || title.Contains("quản lý") || title.Contains("chủ nhiệm")) colOfficer = c;
                    else if (title.Contains("khóa") || title.Contains("niên khóa")) colYear = c;
                    else if (title.Contains("mô tả") || title.Contains("ghi chú")) colDesc = c;
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
                            Unit = string.IsNullOrWhiteSpace(unit) ? "Đại đội 1" : unit,
                            Major = string.IsNullOrWhiteSpace(major) ? "Chỉ huy Tham mưu" : major,
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
                return (true, $"Nhập dữ liệu lớp học thành công: Thêm mới {addedCount} lớp, Cập nhật {updatedCount} lớp.", importedList);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xử lý tệp Excel lớp học: {ex.Message}", importedList);
            }
        }
        #endregion

        #region 5. XUẤT & NHẬP CÁN BỘ QUẢN LÝ
        public async Task<(bool Success, string Message)> ExportOfficersToExcelAsync(IEnumerable<Officer> officers, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Trang cán bộ");

                ws.Cell("A1").Value = "DANH SÁCH CÁN BỘ QUẢN LÝ QUÂN SỰ";
                ws.Range("A1:K1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Thời điểm xuất dữ liệu: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                ws.Range("A2:K2").Merge().Style.Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                string[] headers = { "STT", "Mã cán bộ", "Họ và tên", "Cấp bậc", "Chức vụ", "Đơn vị", "Số điện thoại", "Email", "Chuyên môn", "Ngày sinh", "Ngày nhập ngũ" };
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

                return (true, $"Xuất thành công {stt - 1} cán bộ ra file Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xuất danh sách cán bộ: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, List<Officer> Officers)> ImportOfficersFromExcelAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tệp không tồn tại.", new List<Officer>());

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, new List<Officer>());

                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("cán bộ", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Officer", StringComparison.OrdinalIgnoreCase)) ?? workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                    return (false, "Không tìm thấy sheet dữ liệu cán bộ.", new List<Officer>());

                var imported = new List<Officer>();
                int headerRow = 1;
                for (int r = 1; r <= 15; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => CleanCellText(c.GetString())));
                    if (textRow.Contains("Mã cán bộ") || textRow.Contains("OfficerCode") || textRow.Contains("Họ và tên") || textRow.Contains("Họ tên"))
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

                    if (title.Contains("mã") || title.Contains("officer") || title.Contains("shcb"))
                    {
                        if (colCode == -1) colCode = c;
                    }
                    else if (title.Contains("họ") || title.Contains("tên") || title.Contains("fullname"))
                    {
                        if (colName == -1) colName = c;
                    }
                    else if (title.Contains("cấp bậc") || title.Contains("quân hàm") || title.Equals("rank"))
                    {
                        if (colRank == -1) colRank = c;
                    }
                    else if (title.Contains("chức vụ") || title.Contains("chức danh") || title.Equals("position"))
                    {
                        if (colPos == -1) colPos = c;
                    }
                    else if (title.Contains("đơn vị") || title.Contains("đại đội") || title.Contains("tiểu đoàn") || title.Equals("unit"))
                    {
                        if (colUnit == -1) colUnit = c;
                    }
                    else if (title.Contains("thoại") || title.Contains("sđt") || title.Contains("phone"))
                    {
                        if (colPhone == -1) colPhone = c;
                    }
                    else if (title.Contains("mail"))
                    {
                        if (colEmail == -1) colEmail = c;
                    }
                    else if (title.Contains("chuyên ngành") || title.Contains("chuyên môn") || title.Contains("specialty"))
                    {
                        if (colSpec == -1) colSpec = c;
                    }
                    else if (title.Contains("ngày sinh") || title.Contains("sinh") || title.Contains("dob"))
                    {
                        if (colDob == -1) colDob = c;
                    }
                    else if (title.Contains("nhập ngũ") || title.Contains("enlist"))
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

                    string rank = colRank > 0 ? CleanCellText(ws.Cell(row, colRank).GetString()) : "Thiếu úy";
                    string pos = colPos > 0 ? CleanCellText(ws.Cell(row, colPos).GetString()) : "Cán bộ";
                    string unit = colUnit > 0 ? CleanCellText(ws.Cell(row, colUnit).GetString()) : "Đại đội 1";
                    string phone = colPhone > 0 ? CleanCellText(ws.Cell(row, colPhone).GetString()) : string.Empty;
                    string email = colEmail > 0 ? CleanCellText(ws.Cell(row, colEmail).GetString()) : string.Empty;
                    string specialty = colSpec > 0 ? CleanCellText(ws.Cell(row, colSpec).GetString()) : "Chỉ huy tham mưu";
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
                return (true, $"Nhập thành công {imported.Count} cán bộ từ Excel.", imported);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi nhập danh sách cán bộ: {ex.Message}", new List<Officer>());
            }
        }
        #endregion

        #region 6. XUẤT & NHẬP DANH MỤC TỔ CHỨC
        public async Task<(bool Success, string Message)> ExportCatalogsToExcelAsync(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ranks = (await _rankRepository.GetAllAsync()).OrderBy(r => r.DisplayOrder).ToList();
                var positions = (await _positionRepository.GetAllAsync()).OrderBy(p => p.DisplayOrder).ToList();
                var units = (await _unitRepository.GetAllAsync()).ToList();
                var majors = (await _majorRepository.GetAllAsync()).ToList();

                // 1. Cấp bậc
                var wsRank = workbook.Worksheets.Add("Cấp bậc");
                wsRank.Cell("A1").Value = "DANH MỤC CẤP BẬC QUÂN HÀM";
                wsRank.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] rankHeaders = { "STT", "Mã cấp bậc", "Tên cấp bậc", "Nhóm cấp bậc", "Thứ tự hiển thị" };
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

                // 2. Chức vụ
                var wsPos = workbook.Worksheets.Add("Chức vụ");
                wsPos.Cell("A1").Value = "DANH MỤC CHỨC VỤ QUÂN SỰ";
                wsPos.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] posHeaders = { "STT", "Mã chức vụ", "Tên chức vụ", "Nhóm chức vụ", "Thứ tự hiển thị" };
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

                // 3. Đơn vị
                var wsUnit = workbook.Worksheets.Add("Đơn vị");
                wsUnit.Cell("A1").Value = "DANH MỤC ĐƠN VỊ QUẢN LÝ";
                wsUnit.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] unitHeaders = { "STT", "Mã đơn vị", "Tên đơn vị", "Đơn vị cấp trên", "Người chỉ huy", "Số điện thoại" };
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

                // 4. Chuyên ngành
                var wsMajor = workbook.Worksheets.Add("Chuyên ngành");
                wsMajor.Cell("A1").Value = "DANH MỤC CHUYÊN NGÀNH ĐÀO TẠO";
                wsMajor.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] majorHeaders = { "STT", "Mã chuyên ngành", "Tên chuyên ngành", "Thời gian đào tạo", "Khoa phụ trách" };
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
                return (true, $"Xuất thành công danh mục tổ chức ({ranks.Count} cấp bậc, {positions.Count} chức vụ, {units.Count} đơn vị, {majors.Count} chuyên ngành) ra file Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xuất danh mục tổ chức: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int RanksCount, int PositionsCount, int UnitsCount, int MajorsCount)> ImportCatalogsFromExcelAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tệp không tồn tại.", 0, 0, 0, 0);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, 0, 0, 0, 0);

                using var workbook = new XLWorkbook(filePath);
                int rCount = 0, pCount = 0, uCount = 0, mCount = 0;

                // 1. Cấp bậc
                var wsRank = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Cấp bậc", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Rank", StringComparison.OrdinalIgnoreCase));
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

                // 2. Chức vụ
                var wsPos = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Chức vụ", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Position", StringComparison.OrdinalIgnoreCase));
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

                // 3. Đơn vị
                var wsUnit = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Đơn vị", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Unit", StringComparison.OrdinalIgnoreCase));
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

                // 4. Chuyên ngành
                var wsMajor = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Chuyên ngành", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Major", StringComparison.OrdinalIgnoreCase));
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

                return (true, $"Nhập thành công danh mục: {rCount} cấp bậc, {pCount} chức vụ, {uCount} đơn vị, {mCount} chuyên ngành.", rCount, pCount, uCount, mCount);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi nhập danh mục: {ex.Message}", 0, 0, 0, 0);
            }
        }
        #endregion

        #region 7. XUẤT & NHẬP TOÀN BỘ DỮ LIỆU HỆ THỐNG (FULL BACKUP / RESTORE)
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
                var failedRecords = records.Where(r => r.Grade == "Không đạt").ToList();

                using var workbook = new XLWorkbook();

                // 1. Sheet Tổng quan (KPI Dashboard)
                var wsDash = workbook.Worksheets.Add("Trang tổng quan");
                wsDash.Cell("A1").Value = "BÁO CÁO TỔNG QUAN QUẢN LÝ HỌC VIÊN & CÁN BỘ QUÂN ĐỘI";
                wsDash.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(16)
                    .Font.SetFontColor(XLColor.FromHtml("#1E3A8A")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                wsDash.Cell("A3").Value = "CHỈ SỐ TỔNG HỢP TOÀN ĐƠN VỊ";
                wsDash.Range("A3:C3").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));

                wsDash.Cell("A4").Value = "Tổng số lớp học quân sự:";
                wsDash.Cell("B4").Value = classes.Count;
                wsDash.Cell("A5").Value = "Tổng quân số học viên:";
                wsDash.Cell("B5").Value = cadets.Count;
                wsDash.Cell("A6").Value = "Tổng số cán bộ quản lý:";
                wsDash.Cell("B6").Value = officers.Count;
                wsDash.Cell("A7").Value = "Tổng số môn rèn luyện:";
                wsDash.Cell("B7").Value = subjects.Count;
                wsDash.Cell("A8").Value = "Tổng số lượt kiểm tra:";
                wsDash.Cell("B8").Value = records.Count;

                double passRate = records.Count > 0 
                    ? Math.Round((double)(records.Count - failedRecords.Count) / records.Count * 100, 1) 
                    : 100.0;
                wsDash.Cell("A9").Value = "Tỷ lệ đạt chuẩn quân sự:";
                wsDash.Cell("B9").Value = $"{passRate}%";

                wsDash.Cell("A11").Value = "PHÂN LOẠI XẾP LOẠI CHI TIẾT";
                wsDash.Range("A11:C11").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));

                wsDash.Cell("A12").Value = "Xuất sắc:";
                wsDash.Cell("B12").Value = records.Count(r => r.Grade == "Xuất sắc");
                wsDash.Cell("A13").Value = "Giỏi:";
                wsDash.Cell("B13").Value = records.Count(r => r.Grade == "Giỏi");
                wsDash.Cell("A14").Value = "Khá:";
                wsDash.Cell("B14").Value = records.Count(r => r.Grade == "Khá");
                wsDash.Cell("A15").Value = "Đạt:";
                wsDash.Cell("B15").Value = records.Count(r => r.Grade == "Đạt");
                wsDash.Cell("A16").Value = "Không đạt (Cần rèn luyện lại):";
                wsDash.Cell("B16").Value = failedRecords.Count;
                wsDash.Cell("B16").Style.Font.SetFontColor(XLColor.Red);
                wsDash.Columns().AdjustToContents();

                // 2. Sheet Cán bộ
                var wsOff = workbook.Worksheets.Add("Trang cán bộ");
                wsOff.Cell("A1").Value = "DANH SÁCH CÁN BỘ QUẢN LÝ QUÂN SỰ";
                wsOff.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] oHeaders = { "STT", "Mã cán bộ", "Họ và tên", "Cấp bậc", "Chức vụ", "Đơn vị", "Số điện thoại", "Email", "Chuyên môn", "Ngày sinh", "Ngày nhập ngũ" };
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

                // 3. Sheet Lớp học
                var wsClass = workbook.Worksheets.Add("Trang lớp học");
                wsClass.Cell("A1").Value = "DANH MỤC LỚP HỌC QUÂN ĐỘI";
                wsClass.Range("A1:H1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] cHeaders = { "STT", "Mã lớp", "Tên lớp", "Đơn vị quản lý", "Chuyên ngành", "Cán bộ quản lý", "Khóa học", "Quân số" };
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

                // 4. Sheet Học viên
                var wsCadet = workbook.Worksheets.Add("Trang học viên");
                wsCadet.Cell("A1").Value = "DANH SÁCH HỌC VIÊN QUÂN ĐỘI";
                wsCadet.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] cdHeaders = { "STT", "Mã học viên", "Họ và tên", "Cấp bậc", "Chức vụ", "Đơn vị", "Lớp", "Số điện thoại", "Email", "Tuổi", "Giới tính" };
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

                // 5. Sheet Môn học
                var wsSub = workbook.Worksheets.Add("Trang môn học");
                wsSub.Cell("A1").Value = "DANH MỤC TIÊU CHUẨN RÈN LUYỆN THỂ LỰC";
                wsSub.Range("A1:I1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] sHeaders = { "STT", "Mã môn", "Tên môn", "Nhóm tố chất", "Đơn vị tính", "Chuẩn Giỏi", "Chuẩn Khá", "Chuẩn Đạt", "Càng cao càng tốt" };
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
                    wsSub.Cell(i + 4, 9).Value = s.IsHigherBetter ? "Có" : "Không";
                }
                wsSub.Columns().AdjustToContents();

                // 6. Sheet Kiểm tra thể lực
                var wsExam = workbook.Worksheets.Add("Kiểm tra thể lực");
                wsExam.Cell("A1").Value = "BẢNG KẾT QUẢ KIỂM TRA ĐỊNH KỲ";
                wsExam.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#15803D"));
                string[] eHeaders = { "STT", "Mã học viên", "Họ và tên", "Đơn vị", "Lớp", "Mã môn", "Tên môn", "Thành tích", "Xếp loại", "Đợt kiểm tra", "Ngày kiểm tra" };
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

                // 7. Sheet Học viên chưa đạt (Rèn luyện bổ sung)
                var wsFail = workbook.Worksheets.Add("Rèn luyện bổ sung");
                wsFail.Cell("A1").Value = "DANH SÁCH HỌC VIÊN CHƯA ĐẠT CẦN RÈN LUYỆN BỔ SUNG";
                wsFail.Range("A1:H1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#DC2626"));
                string[] fHeaders = { "STT", "Mã học viên", "Họ và tên", "Đơn vị", "Lớp", "Nội dung chưa đạt", "Thành tích", "Ngày kiểm tra" };
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

                // 8. Sheet Danh mục tổ chức: Cấp bậc
                var wsRank = workbook.Worksheets.Add("Cấp bậc");
                wsRank.Cell("A1").Value = "DANH MỤC CẤP BẬC QUÂN HÀM";
                wsRank.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] rHeaders = { "STT", "Mã cấp bậc", "Tên cấp bậc", "Nhóm cấp bậc", "Thứ tự hiển thị" };
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

                // 9. Sheet Chức vụ
                var wsPos = workbook.Worksheets.Add("Chức vụ");
                wsPos.Cell("A1").Value = "DANH MỤC CHỨC VỤ QUÂN SỰ";
                wsPos.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] pHeaders = { "STT", "Mã chức vụ", "Tên chức vụ", "Nhóm chức vụ", "Thứ tự hiển thị" };
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

                // 10. Sheet Đơn vị
                var wsUnit = workbook.Worksheets.Add("Đơn vị");
                wsUnit.Cell("A1").Value = "DANH MỤC ĐƠN VỊ QUẢN LÝ";
                wsUnit.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] uHeaders = { "STT", "Mã đơn vị", "Tên đơn vị", "Đơn vị cấp trên", "Người chỉ huy", "Số điện thoại" };
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

                // 11. Sheet Chuyên ngành
                var wsMajor = workbook.Worksheets.Add("Chuyên ngành");
                wsMajor.Cell("A1").Value = "DANH MỤC CHUYÊN NGÀNH ĐÀO TẠO";
                wsMajor.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] mHeaders = { "STT", "Mã chuyên ngành", "Tên chuyên ngành", "Thời gian đào tạo", "Khoa phụ trách" };
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

                // 12. Sheet Môn học tín chỉ
                var creditSubjects = await _context.CreditSubjects.AsNoTracking().ToListAsync();
                var wsCreditSub = workbook.Worksheets.Add("Môn học tín chỉ");
                wsCreditSub.Cell("A1").Value = "DANH MỤC MÔN HỌC TÍN CHỈ QUÂN SỰ";
                wsCreditSub.Range("A1:H1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] csHeaders = { "STT", "Mã môn học", "Tên môn học", "Số tín chỉ", "Hình thức đánh giá", "Nhóm môn học", "Là môn thành phần", "Ghi chú" };
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
                    wsCreditSub.Cell(i + 4, 7).Value = cs.IsComponent ? "Có" : "Không";
                    wsCreditSub.Cell(i + 4, 8).Value = cs.Description ?? "";
                }
                wsCreditSub.Columns().AdjustToContents();

                // 13. Sheet Đợt kiểm tra & Thi
                var components = await _context.SubjectAssessmentComponents
                    .Include(c => c.CreditSubject)
                    .AsNoTracking()
                    .OrderBy(c => c.CreditSubjectId)
                    .ThenBy(c => c.OrderIndex)
                    .ToListAsync();

                var wsComp = workbook.Worksheets.Add("Đợt kiểm tra & Thi");
                wsComp.Cell("A1").Value = "DANH MỤC ĐỢT KIỂM TRA & THI CỦA MÔN TÍN CHỈ";
                wsComp.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] compHeaders = { "STT", "Mã môn học", "Tên môn học", "Tên đợt kiểm tra / thi", "Số tín chỉ đợt", "Thứ tự" };
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

                // 14. Sheet Bảng điểm chi tiết
                var creditScores = await _context.CreditScoreRecords
                    .Include(s => s.Cadet)
                    .Include(s => s.CreditSubject)
                    .Include(s => s.Component)
                    .AsNoTracking()
                    .ToListAsync();

                var wsScores = workbook.Worksheets.Add("Bảng điểm tín chỉ");
                wsScores.Cell("A1").Value = "CHI TIẾT ĐẦU ĐIỂM HỌC TẬP TÍN CHỈ HỌC VIÊN";
                wsScores.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] scHeaders = { "STT", "Mã học viên", "Họ và tên", "Đơn vị", "Lớp", "Mã môn học", "Tên môn học", "Tên đợt kiểm tra / thi", "Điểm số", "Ngày kiểm tra", "Ghi chú" };
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
                return (true, $"Xuất toàn bộ dữ liệu thành công ra file Excel ({classes.Count} lớp học, {cadets.Count} học viên, {officers.Count} cán bộ, {creditSubjects.Count} môn tín chỉ, {components.Count} đợt kiểm tra, {creditScores.Count} đầu điểm trên 14 sheets).");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xuất toàn bộ dữ liệu: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int ClassesCount, int CadetsCount, int SubjectsCount, int ExamsCount, int OfficersCount, int CreditSubjectsCount, int CreditScoresCount)> ImportAllDataFromExcelAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tệp không tồn tại.", 0, 0, 0, 0, 0, 0, 0);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, 0, 0, 0, 0, 0, 0, 0);

                // 1. Nhập danh mục tổ chức trước
                var catResult = await ImportCatalogsFromExcelAsync(filePath);
                // 2. Nhập cán bộ
                var offResult = await ImportOfficersFromExcelAsync(filePath);
                // 3. Nhập lớp học
                var classResult = await ImportClassesFromExcelAsync(filePath);
                // 4. Nhập môn học thể lực
                var subResult = await ImportSubjectsFromExcelAsync(filePath);
                // 5. Nhập học viên
                var cadetResult = await ImportCadetsFromExcelAsync(filePath);
                // 6. Nhập kết quả kiểm tra thể lực
                var examResult = await ImportExamRecordsFromExcelAsync(filePath);
                // 7. Nhập môn học tín chỉ, đợt kiểm tra và bảng điểm tín chỉ
                var (csCount, scoreCount) = await ImportCreditDataFromExcelAsync(filePath);

                int clCount = classResult.Classes.Count;
                int cCount = cadetResult.Cadets.Count;
                int sCount = subResult.Subjects.Count;
                int eCount = examResult.Records.Count;
                int offCount = offResult.Officers.Count;

                return (true, $"Khôi phục toàn bộ dữ liệu thành công: {clCount} lớp học, {cCount} học viên, {offCount} cán bộ, {csCount} môn tín chỉ, {scoreCount} đầu điểm, {sCount} môn thể lực, {eCount} lượt kiểm tra thể lực.", clCount, cCount, sCount, eCount, offCount, csCount, scoreCount);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi nhập toàn bộ dữ liệu: {ex.Message}", 0, 0, 0, 0, 0, 0, 0);
            }
        }

        private async Task<(int SubjectsCount, int ScoresCount)> ImportCreditDataFromExcelAsync(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            int importedSubjects = 0;
            int importedScores = 0;

            var wsCreditSub = workbook.Worksheets.FirstOrDefault(w => w.Name == "Môn học tín chỉ");
            var wsComp = workbook.Worksheets.FirstOrDefault(w => w.Name == "Đợt kiểm tra & Thi");
            var wsScores = workbook.Worksheets.FirstOrDefault(w => w.Name == "Bảng điểm tín chỉ");

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
                    if (string.IsNullOrWhiteSpace(assess)) assess = "Kiểm tra và thi";
                    string group = CleanCellText(wsCreditSub.Cell(r, 6).GetString());
                    string isCompStr = CleanCellText(wsCreditSub.Cell(r, 7).GetString());
                    bool isComp = isCompStr.Equals("Có", StringComparison.OrdinalIgnoreCase);
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

                // Nhập Đợt kiểm tra & Thi
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

                // Nhập Bảng điểm chi tiết
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

        #region 8. Báo Cáo Đối Soát & So Sánh Đợt Thi
        public async Task<(bool Success, string Message)> ExportComparisonToExcelAsync(ExamComparisonResultDto comparison, string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook();

                    // ================= Sheet 1: Tổng quan Cấp Đại đội =================
                    var wsUnit = workbook.Worksheets.Add("1. Tổng Quan & Đại Đội");
                    wsUnit.ShowGridLines = true;

                    // Tiêu đề
                    wsUnit.Cell(1, 1).Value = "BÁO CÁO PHÂN TÍCH SO SÁNH KẾT QUẢ RÈN LUYỆN THỂ LỰC";
                    wsUnit.Range(1, 1, 1, 10).Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    wsUnit.Cell(2, 1).Value = $"Đợt gốc: {comparison.BaselineSession}   |   Đợt so sánh: {comparison.CompareSession}   |   Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    wsUnit.Range(2, 1, 2, 10).Merge().Style.Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    // Thống kê toàn đơn vị
                    wsUnit.Cell(4, 1).Value = "THỐNG KÊ BIẾN ĐỘNG TOÀN QUÂN SỐ";
                    wsUnit.Range(4, 1, 4, 10).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#1E293B")).Font.SetFontColor(XLColor.White);

                    wsUnit.Cell(5, 1).Value = "Tổng quân số đánh giá:";
                    wsUnit.Cell(5, 2).Value = comparison.TotalEvaluatedCadets;
                    wsUnit.Cell(5, 3).Value = "Tăng trưởng (▲):";
                    wsUnit.Cell(5, 4).Value = $"{comparison.OverallGrowthCount} ({comparison.OverallGrowthPercentage:F1}%)";
                    wsUnit.Cell(5, 4).Style.Font.SetFontColor(XLColor.FromHtml("#16A34A")).Font.SetBold();

                    wsUnit.Cell(5, 5).Value = "Giữ nguyên (—):";
                    wsUnit.Cell(5, 6).Value = $"{comparison.OverallUnchangedCount} ({comparison.OverallUnchangedPercentage:F1}%)";
                    wsUnit.Cell(5, 6).Style.Font.SetFontColor(XLColor.FromHtml("#D97706")).Font.SetBold();

                    wsUnit.Cell(5, 7).Value = "Thụt lùi (▼):";
                    wsUnit.Cell(5, 8).Value = $"{comparison.OverallRegressionCount} ({comparison.OverallRegressionPercentage:F1}%)";
                    wsUnit.Cell(5, 8).Style.Font.SetFontColor(XLColor.FromHtml("#DC2626")).Font.SetBold();

                    wsUnit.Cell(5, 9).Value = "Delta % Đạt:";
                    wsUnit.Cell(5, 10).Value = $"{(comparison.PassRateDelta >= 0 ? "+" : "")}{comparison.PassRateDelta:F1}%";
                    wsUnit.Cell(5, 10).Style.Font.SetBold();

                    // Bảng chi tiết theo từng Đại đội
                    wsUnit.Cell(7, 1).Value = "BẢNG SO SÁNH THEO TỪNG ĐẠI ĐỘI & ĐƠN VỊ";
                    wsUnit.Range(7, 1, 7, 10).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#334155")).Font.SetFontColor(XLColor.White);

                    string[] unitHeaders = { "Đơn vị", "Quân số", "% Đạt Đợt 1", "% Đạt Đợt 2", "Chênh lệch (Delta)", "% Giỏi/Khá Đợt 1", "% Giỏi/Khá Đợt 2", "Tăng trưởng (▲)", "Giữ nguyên (—)", "Thụt lùi (▼)" };
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

                    // ================= Sheet 2: Cấp Lớp & Phân Đội =================
                    var wsClass = workbook.Worksheets.Add("2. Cấp Lớp & Phân Đội");
                    wsClass.ShowGridLines = true;

                    wsClass.Cell(1, 1).Value = "BẢNG SO SÁNH THÀNH TÍCH THEO LỚP & TIỂU ĐỘI";
                    wsClass.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] classHeaders = { "Lớp / Tiểu đội", "Đại đội", "Thứ hạng", "Quân số", "% Đạt Đợt 1", "% Đạt Đợt 2", "Chênh lệch (Delta)", "Tăng (▲)", "Giảm (▼)" };
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
                        wsClass.Cell(clRow, 3).Value = $"Hạng {c.RankInUnit}";
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

                    // ================= Sheet 3: Chi Tiết Cá Nhân Học Viên =================
                    var wsCadet = workbook.Worksheets.Add("3. Chi Tiết Cá Nhân");
                    wsCadet.ShowGridLines = true;

                    wsCadet.Cell(1, 1).Value = "BẢNG CHI TIẾT BIẾN ĐỘNG THÀNH TÍCH TỪNG CÁ NHÂN HỌC VIÊN";
                    wsCadet.Range(1, 1, 1, 12).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] cadetHeaders = { "STT", "Mã HV", "Họ và tên", "Cấp bậc", "Đơn vị", "Lớp", "Môn kiểm tra", "Điểm Đợt 1", "Điểm Đợt 2", "Chênh lệch (Delta)", "Xếp loại Đợt 1", "Xếp loại Đợt 2", "Xu hướng" };
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
                            wsCadet.Cell(cadRow, 7).Value = "Chưa có môn so sánh";
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
                    return (true, $"Xuất báo cáo phân tích đối soát đợt thi thành công ra file: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi xuất file Excel: {ex.Message}");
                }
            });
        }
        #endregion

        #region 9. BÁO CÁO TỔNG QUAN & ĐỀ XUẤT HUẤN LUYỆN AI
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
                    // SHEET 1: TỔNG QUAN & CHỈ TIÊU ĐƠN VỊ
                    // ==========================================
                    var ws1 = workbook.Worksheets.Add("Tổng Quan & Thi Đua");
                    ws1.Cell("A1").Value = "BÁO CÁO TỔNG QUAN CHỈ ĐẠO HUẤN LUYỆN & RÈN LUYỆN THỂ LỰC";
                    ws1.Range("A1:G1").Merge().Style
                        .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#0F172A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    ws1.Cell("A2").Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm} - Hệ Thống Quản Lý Học Viên Quân Đội";
                    ws1.Range("A2:G2").Merge().Style
                        .Font.SetItalic().Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#64748B"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    // Khối KPI Tổng thể
                    ws1.Cell("A4").Value = "I. CHỈ SỐ RÈN LUYỆN TỔNG THỂ";
                    ws1.Range("A4:G4").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));

                    string[] kpiHeaders = { "Tổng Quân Số", "Lượt Kiểm Tra", "Tỷ Lệ Đạt Chuẩn", "Tỷ Lệ Giỏi/XS", "Chưa Đạt Chuẩn", "Đánh Giá Toàn Viện" };
                    for (int i = 0; i < kpiHeaders.Length; i++)
                    {
                        var cell = ws1.Cell(5, i + 1);
                        cell.Value = kpiHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    ws1.Cell(6, 1).Value = $"{summary.TotalCadets} đ/c";
                    ws1.Cell(6, 2).Value = $"{summary.TotalExamRecords} lượt";
                    ws1.Cell(6, 3).Value = $"{summary.OverallPassRate:F1}%";
                    ws1.Cell(6, 4).Value = $"{summary.EliteRate:F1}%";
                    ws1.Cell(6, 5).Value = $"{summary.FailCount} lượt ({summary.FailRate:F1}%)";
                    ws1.Cell(6, 6).Value = summary.OverallRatingLabel;

                    ws1.Range("A6:F6").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                                            .Font.SetBold().Font.SetFontSize(11)
                                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                            .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    // Bảng Xếp hạng Đại đội
                    ws1.Cell("A8").Value = "II. BẢNG XẾP HẠNG THI ĐUA RÈN LUYỆN CÁC ĐẠI ĐỘI / ĐƠN VỊ";
                    ws1.Range("A8:G8").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#15803D"));

                    string[] unitHeaders = { "Hạng", "Đơn Vị / Đại Đội", "Quân Số", "Lượt Kiểm Tra", "Tỷ Lệ Đạt (%)", "Tỷ Lệ Giỏi (%)", "Xếp Loại Đơn Vị" };
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

                    // Bảng Độ khó Môn thể lực
                    int sHeaderRow = uRow + 2;
                    ws1.Cell(sHeaderRow, 1).Value = "III. PHÂN TÍCH TỶ LỆ ĐẠT & ĐỘ KHÓ CÁC MÔN KIỂM TRA";
                    ws1.Range(sHeaderRow, 1, sHeaderRow, 6).Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#B45309"));

                    string[] subHeaders = { "Mã Môn", "Tên Môn Kiểm Tra", "Tổng Lượt Thi", "Tỷ Lệ Đạt (%)", "Tỷ Lệ Chưa Đạt (%)", "Mức Độ Rủi Ro / Đánh Giá" };
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
                    // SHEET 2: ĐỀ XUẤT HUẤN LUYỆN AI THÔNG MINH
                    // ==========================================
                    var ws2 = workbook.Worksheets.Add("Đề Xuất Huấn Luyện AI");
                    ws2.Cell("A1").Value = "CHỈ ĐẠO & PHÁC ĐỒ HUẤN LUYỆN THỂ LỰC QUÂN ĐỘI THÔNG MINH";
                    ws2.Range("A1:G1").Merge().Style
                        .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#4338CA"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    ws2.Cell("A3").Value = "1. ĐÁNH GIÁ CHIẾN LƯỢC & ĐỀ XUẤT PHÂN BỔ THỜI GIAN:";
                    ws2.Range("A3:G3").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));

                    ws2.Cell("A4").Value = aiRecommendations.StrategicDirective.ExecutiveSummary;
                    ws2.Range("A4:G4").Merge().Style.Font.SetItalic().Font.SetFontSize(11);

                    ws2.Cell("A5").Value = $"• Phân bổ quỹ thời gian: {aiRecommendations.StrategicDirective.TimeAllocationDirective}";
                    ws2.Range("A5:G5").Merge().Style.Font.SetFontSize(11);

                    ws2.Cell("A6").Value = $"• Phục hồi & dinh dưỡng: {aiRecommendations.StrategicDirective.RecoveryAndNutritionAdvice}";
                    ws2.Range("A6:G6").Merge().Style.Font.SetFontSize(11);

                    ws2.Cell("A8").Value = "2. PHÁC ĐỒ HUẤN LUYỆN CHUYÊN SÂU THEO TỪNG NHÓM TỐ CHẤT THỂ LỰC QUÂN SỰ:";
                    ws2.Range("A8:G8").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#4338CA"));

                    string[] aiHeaders = { "Nhóm Tố Chất Thể Lực", "Nội Dung Môn", "Tỷ Lệ Chưa Đạt", "Mức Độ Ưu Tiên", "Phân Tích Điểm Nghẽn Kỹ Thuật", "Phác Đồ Bài Tập Khoa Học", "Lịch Huấn Luyện Tuần" };
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
                        ws2.Cell(aiRow, 3).Value = $"{c.FailRate:F1}% ({c.AffectedCadetsCount} đ/c)";
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
                    // SHEET 3: DANH SÁCH CẦN BỒI DƯỠNG THỂ LỰC
                    // ==========================================
                    var ws3 = workbook.Worksheets.Add("DS Cần Bồi Dưỡng Thể Lực");
                    ws3.Cell("A1").Value = "DANH SÁCH HỌC VIÊN CHƯA ĐẠT CHUẨN - ĐƯA VÀO KẾ HOẠCH BỒI DƯỠNG CẤP TỐC";
                    ws3.Range("A1:H1").Merge().Style
                        .Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#B91C1C"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] failHeaders = { "STT", "Mã HV", "Họ và Tên", "Đơn Vị", "Lớp", "Nội Dung Chưa Đạt", "Thành Tích", "Phác Đồ Bồi Dưỡng & Thời Hạn" };
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
                        ws3.Cell(fRow, 8).Value = "Tập bổ trợ thể lực chuyên biệt; kiểm tra sát hạch lại sau 30 ngày";

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
                    // SHEET 4: BIỂU DƯƠNG HỌC VIÊN XUẤT SẮC
                    // ==========================================
                    var ws4 = workbook.Worksheets.Add("DS Biểu Dương Khen Thưởng");
                    ws4.Cell("A1").Value = "BẢNG VÀNG BIỂU DƯƠNG HỌC VIÊN RÈN LUYỆN THỂ LỰC XUẤT SẮC & KIỆN TƯỚNG";
                    ws4.Range("A1:H1").Merge().Style
                        .Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#15803D"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] honorHeaders = { "STT", "Mã HV", "Họ và Tên", "Cấp Bậc", "Đơn Vị", "Lớp", "Danh Hiệu Biểu Dương", "Nội Dung Tiêu Biểu" };
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
                    return (true, $"Xuất báo cáo tổng quan & đề xuất huấn luyện AI thành công: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi xuất báo cáo tổng quan: {ex.Message}");
                }
            });
        }

        public async Task<(bool Success, string Message)> ExportAcademicDashboardMultiSheetReportAsync(
            string filePath,
            QL_HocVien.Models.DTOs.DashboardSummaryDto summary,
            IEnumerable<QL_HocVien.Models.DTOs.UnitLeaderboardDto> units,
            IEnumerable<QL_HocVien.Models.DTOs.AcademicClassComparisonDto> classes,
            IEnumerable<QL_HocVien.Models.DTOs.SubjectPerformanceDto> subjects,
            IEnumerable<QL_HocVien.Models.DTOs.AcademicWarningCadetDto> warningCadets,
            IEnumerable<QL_HocVien.Models.DTOs.CadetHonorDto> honoredCadets,
            IEnumerable<QL_HocVien.Models.DTOs.AcademicCadetAnalyticsDto> cumulativeCadets)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook();

                    // =========================================================================
                    // SHEET 1: TỔNG QUAN HỌC VỤ & KPI
                    // =========================================================================
                    var ws1 = workbook.Worksheets.Add("Tổng Quan Học Vụ & KPI");
                    ws1.Cell("A1").Value = "HỌC VIỆN QUÂN SỰ - PHÒNG ĐÀO TẠO & QUẢN LÝ HỌC VIÊN";
                    ws1.Range("A1:G1").Merge().Style.Font.SetBold().Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#475569"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    ws1.Cell("A2").Value = "BÁO CÁO TỔNG HỢP CÔNG TÁC HỌC VỤ & ĐÀO TẠO THEO HỆ THỐNG TÍN CHỈ";
                    ws1.Range("A2:G2").Merge().Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#8F1515"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    ws1.Cell("A3").Value = $"Thời điểm xuất dữ liệu: {DateTime.Now:dd/MM/yyyy HH:mm} | Hệ thống Quản Lý Đào Tạo Học Viên";
                    ws1.Range("A3:G3").Merge().Style.Font.SetItalic().Font.SetFontSize(10.5).Font.SetFontColor(XLColor.FromHtml("#64748B"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    // Khối I: Chỉ số KPI Chiến lược
                    ws1.Cell("A5").Value = "I. CHỈ SỐ HỌC VỤ CHIẾN LƯỢC TOÀN VIỆN";
                    ws1.Range("A5:G5").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));

                    string[] kpiHeaders = { "Tổng Quân Số", "Học Phần Tín Chỉ", "Điểm TB GPA Toàn Viện", "Tỷ Lệ Đạt Chuẩn", "Tỷ Lệ Giỏi / Khá", "Cảnh Báo Nợ Môn", "Đánh Giá Học Vụ" };
                    for (int i = 0; i < kpiHeaders.Length; i++)
                    {
                        var cell = ws1.Cell(6, i + 1);
                        cell.Value = kpiHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    ws1.Cell(7, 1).Value = $"{summary.TotalCadets} học viên";
                    ws1.Cell(7, 2).Value = $"{summary.TotalCreditSubjects} môn ({summary.TotalCreditScores} lượt điểm)";
                    ws1.Cell(7, 3).Value = $"{summary.AverageGpa:F2} / 10";
                    ws1.Cell(7, 4).Value = $"{summary.GraduationReadinessRate:F1}% ({summary.CompletedCadetsCount}/{summary.TotalCadets})";
                    ws1.Cell(7, 5).Value = $"{summary.EliteRate:F1}%";
                    ws1.Cell(7, 6).Value = $"{summary.WarningCount} đ/c nợ/yếu";
                    ws1.Cell(7, 7).Value = summary.OverallRatingLabel;

                    ws1.Range("A7:G7").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                                            .Font.SetBold().Font.SetFontSize(11)
                                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                            .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    // Khối II: Phân bố phổ điểm & xếp loại học lực
                    ws1.Cell("A9").Value = "II. PHÂN BỐ HỌC LỰC TOÀN KHÓA / ĐƠN VỊ";
                    ws1.Range("A9:E9").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#15803D"));

                    string[] distHeaders = { "Xếp Loại Học Lực", "Tiêu Chuẩn Điểm (GPA)", "Số Lượng Học Viên", "Tỷ Lệ Phần Trăm (%)", "Đánh Giá & Hướng Xử Lý" };
                    for (int i = 0; i < distHeaders.Length; i++)
                    {
                        var cell = ws1.Cell(10, i + 1);
                        cell.Value = distHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#166534"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int totalEvaluated = summary.TotalCadets > 0 ? summary.TotalCadets : 1;
                    (string Tier, string Criteria, int Count, string Note, string ColorHex)[] distData = {
                        ("Xuất sắc / Giỏi", "GPA >= 8.0", summary.ExcellentCount, $"{summary.ExcellentCount * 100.0 / totalEvaluated:F1}%", "Khen thưởng, đưa vào danh sách nguồn cán bộ"),
                        ("Khá", "7.0 <= GPA < 8.0", summary.GoodCount, $"{summary.GoodCount * 100.0 / totalEvaluated:F1}%", "Đạt yêu cầu đào tạo chính quy, duy trì phong độ"),
                        ("Trung bình", "5.0 <= GPA < 7.0", summary.FairCount, $"{summary.FairCount * 100.0 / totalEvaluated:F1}%", "Cần kèm cặp nâng cao các học phần cơ sở ngành"),
                        ("Yếu / Cảnh báo nợ môn", "GPA < 5.0 hoặc nợ môn", summary.FailCount, $"{summary.FailCount * 100.0 / totalEvaluated:F1}%", "Đưa vào diện phụ đạo học kỳ hè, bố trí thi lại")
                    };

                    int distRow = 11;
                    foreach (var d in distData)
                    {
                        ws1.Cell(distRow, 1).Value = d.Tier;
                        ws1.Cell(distRow, 2).Value = d.Criteria;
                        ws1.Cell(distRow, 3).Value = d.Count;
                        ws1.Cell(distRow, 4).Value = d.Note;
                        ws1.Cell(distRow, 5).Value = d.ColorHex;

                        ws1.Range(distRow, 1, distRow, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                               .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        ws1.Cell(distRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws1.Cell(distRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold();
                        ws1.Cell(distRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        distRow++;
                    }

                    ws1.Columns().AdjustToContents();

                    // =========================================================================
                    // SHEET 2: XẾP HẠNG THI ĐUA ĐƠN VỊ
                    // =========================================================================
                    var ws2 = workbook.Worksheets.Add("Xếp Hạng Thi Đua Đơn Vị");
                    ws2.Cell("A1").Value = "BẢNG TỔNG HỢP XẾP HẠNG THI ĐUA HỌC TẬP CẤP ĐẠI ĐỘI & CẤP LỚP";
                    ws2.Range("A1:I1").Merge().Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#15803D"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    ws2.Cell("A3").Value = "1. BẢNG THI ĐUA CẤP ĐẠI ĐỘI / TIỂU ĐOÀN:";
                    ws2.Range("A3:I3").Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#166534"));

                    string[] unitHeaders = { "Thứ Hạng", "Đơn Vị / Đại Đội", "Quân Số", "Điểm TB GPA", "% Giỏi/XS", "% Khá", "% TB", "% Yếu/Nợ Môn", "Xếp Loại Đơn Vị" };
                    for (int i = 0; i < unitHeaders.Length; i++)
                    {
                        var cell = ws2.Cell(4, i + 1);
                        cell.Value = unitHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#15803D"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int uRow = 5;
                    foreach (var u in units)
                    {
                        ws2.Cell(uRow, 1).Value = u.RankMedal;
                        ws2.Cell(uRow, 2).Value = u.UnitName;
                        ws2.Cell(uRow, 3).Value = u.TotalCadets;
                        ws2.Cell(uRow, 4).Value = $"{u.AverageGpa:F2}";
                        ws2.Cell(uRow, 5).Value = $"{u.ExcellentRate:F1}%";
                        ws2.Cell(uRow, 6).Value = $"{u.GoodRate:F1}%";
                        ws2.Cell(uRow, 7).Value = $"{u.FairRate:F1}%";
                        ws2.Cell(uRow, 8).Value = $"{100.0 - u.PassRate:F1}%";
                        ws2.Cell(uRow, 9).Value = u.EvaluationStatus;

                        ws2.Range(uRow, 1, uRow, 9).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        if (uRow % 2 == 1) ws2.Range(uRow, 1, uRow, 9).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F0FDF4"));

                        ws2.Cell(uRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(uRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(uRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold();
                        ws2.Cell(uRow, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(uRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(uRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(uRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        uRow++;
                    }

                    // Phần 2: Cấp Lớp
                    int clHeaderRow = uRow + 2;
                    ws2.Cell(clHeaderRow, 1).Value = "2. BẢNG XẾP HẠNG HỌC LỰC CẤP LỚP / PHÂN ĐỘI:";
                    ws2.Range(clHeaderRow, 1, clHeaderRow, 8).Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));

                    string[] classHeaders = { "Thứ Hạng", "Lớp / Phân Đội", "Đại Đội", "Quân Số", "Điểm TB GPA", "% Khá/Giỏi", "Số HV Nợ Môn", "Đánh Giá Thi Đua" };
                    for (int i = 0; i < classHeaders.Length; i++)
                    {
                        var cell = ws2.Cell(clHeaderRow + 1, i + 1);
                        cell.Value = classHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int clRow = clHeaderRow + 2;
                    foreach (var cl in classes)
                    {
                        ws2.Cell(clRow, 1).Value = cl.RankInUnit;
                        ws2.Cell(clRow, 2).Value = cl.ClassName;
                        ws2.Cell(clRow, 3).Value = cl.Unit;
                        ws2.Cell(clRow, 4).Value = cl.TotalCadets;
                        ws2.Cell(clRow, 5).Value = $"{cl.AverageGpa:F2}";
                        ws2.Cell(clRow, 6).Value = $"{cl.GoodOrAboveRate:F1}%";
                        ws2.Cell(clRow, 7).Value = cl.MissingCount;
                        ws2.Cell(clRow, 8).Value = cl.EvaluationComment;

                        ws2.Range(clRow, 1, clRow, 8).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                           .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        if (clRow % 2 == 1) ws2.Range(clRow, 1, clRow, 8).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));

                        ws2.Cell(clRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(clRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(clRow, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold();
                        ws2.Cell(clRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws2.Cell(clRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        clRow++;
                    }

                    ws2.Columns().AdjustToContents();

                    // =========================================================================
                    // SHEET 3: PHÂN TÍCH CHI TIẾT MÔN HỌC
                    // =========================================================================
                    var ws3 = workbook.Worksheets.Add("Phân Tích Chi Tiết Môn Học");
                    ws3.Cell("A1").Value = "BẢNG PHÂN TÍCH HIỆU SUẤT & ĐỘ KHÓ CÁC HỌC PHẦN TÍN CHỈ";
                    ws3.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#B45309"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] subHeaders = { "STT", "Mã Học Phần", "Tên Môn Học", "Số Tín Chỉ", "Hình Thức", "Tổng Lượt Điểm", "Điểm TB Môn", "Tỷ Lệ Đạt (%)", "Tỷ Lệ Giỏi (%)", "Số Điểm F (Rớt)", "Đánh Giá Độ Khó" };
                    for (int i = 0; i < subHeaders.Length; i++)
                    {
                        var cell = ws3.Cell(3, i + 1);
                        cell.Value = subHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#D97706"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int sRow = 4;
                    int sStt = 1;
                    foreach (var s in subjects)
                    {
                        ws3.Cell(sRow, 1).Value = sStt++;
                        ws3.Cell(sRow, 2).Value = s.SubjectCode;
                        ws3.Cell(sRow, 3).Value = s.SubjectName;
                        ws3.Cell(sRow, 4).Value = s.Credits;
                        ws3.Cell(sRow, 5).Value = s.AssessmentType;
                        ws3.Cell(sRow, 6).Value = s.TotalTested;
                        ws3.Cell(sRow, 7).Value = $"{s.AverageScore:F2}";
                        ws3.Cell(sRow, 8).Value = $"{s.PassRate:F1}%";
                        ws3.Cell(sRow, 9).Value = $"{s.EliteRate:F1}%";
                        ws3.Cell(sRow, 10).Value = s.FailedCount;
                        ws3.Cell(sRow, 11).Value = s.DifficultyLevel;

                        ws3.Range(sRow, 1, sRow, 11).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                           .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        if (sRow % 2 == 1) ws3.Range(sRow, 1, sRow, 11).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FFFBEB"));

                        ws3.Cell(sRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws3.Cell(sRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws3.Cell(sRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws3.Cell(sRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws3.Cell(sRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold();
                        ws3.Cell(sRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws3.Cell(sRow, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws3.Cell(sRow, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        sRow++;
                    }

                    ws3.Columns().AdjustToContents();

                    // =========================================================================
                    // SHEET 4: CẢNH BÁO HỌC VỤ & HỌC LẠI
                    // =========================================================================
                    var ws4 = workbook.Worksheets.Add("Cảnh Báo Học Vụ & Học Lại");
                    ws4.Cell("A1").Value = "DANH SÁCH HỌC VIÊN TRONG DIỆN CẢNH BÁO HỌC VỤ & NỢ MÔN CẦN PHỤ ĐẠO";
                    ws4.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#DC2626"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] warnHeaders = { "STT", "Mã HV", "Họ và Tên", "Cấp Bậc", "Đơn Vị", "Lớp", "Điểm TB GPA", "Số TC Nợ/Thiếu", "Lý Do Cảnh Báo", "Mức Độ", "Kế Hoạch Khắc Phục" };
                    for (int i = 0; i < warnHeaders.Length; i++)
                    {
                        var cell = ws4.Cell(3, i + 1);
                        cell.Value = warnHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#DC2626"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int wRow = 4;
                    int wStt = 1;
                    foreach (var w in warningCadets)
                    {
                        ws4.Cell(wRow, 1).Value = wStt++;
                        ws4.Cell(wRow, 2).Value = w.CadetCode;
                        ws4.Cell(wRow, 3).Value = w.FullName;
                        ws4.Cell(wRow, 4).Value = w.Rank;
                        ws4.Cell(wRow, 5).Value = w.Unit;
                        ws4.Cell(wRow, 6).Value = w.ClassName;
                        ws4.Cell(wRow, 7).Value = $"{w.Gpa:F2}";
                        ws4.Cell(wRow, 8).Value = w.MissingSubjectsCount + w.FailedSubjectsCount;
                        ws4.Cell(wRow, 9).Value = w.WarningReason;
                        ws4.Cell(wRow, 10).Value = w.WarningSeverity;
                        ws4.Cell(wRow, 11).Value = w.ActionPlan;

                        ws4.Range(wRow, 1, wRow, 11).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                           .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        if (wRow % 2 == 1) ws4.Range(wRow, 1, wRow, 11).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEF2F2"));

                        ws4.Cell(wRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws4.Cell(wRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws4.Cell(wRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold();
                        ws4.Cell(wRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws4.Cell(wRow, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        wRow++;
                    }

                    ws4.Columns().AdjustToContents();

                    // =========================================================================
                    // SHEET 5: BẢNG VÀNG DANH DỰ (TOP GPA)
                    // =========================================================================
                    var ws5 = workbook.Worksheets.Add("Bảng Vàng Danh Dự (Top GPA)");
                    ws5.Cell("A1").Value = "BẢNG VÀNG DANH DỰ BIỂU DƯƠNG HỌC VIÊN CÓ THÀNH TÍCH HỌC TẬP XUẤT SẮC";
                    ws5.Range("A1:J1").Merge().Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#15803D"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] honorHeaders = { "Thứ Hạng", "Mã HV", "Họ và Tên", "Cấp Bậc", "Đơn Vị", "Lớp", "Điểm TB GPA", "TC Tích Lũy", "Danh Hiệu Khen Thưởng", "Môn Học Tiêu Biểu" };
                    for (int i = 0; i < honorHeaders.Length; i++)
                    {
                        var cell = ws5.Cell(3, i + 1);
                        cell.Value = honorHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#16A34A"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int hRow = 4;
                    int hRank = 1;
                    foreach (var h in honoredCadets)
                    {
                        ws5.Cell(hRow, 1).Value = hRank++;
                        ws5.Cell(hRow, 2).Value = h.CadetCode;
                        ws5.Cell(hRow, 3).Value = h.FullName;
                        ws5.Cell(hRow, 4).Value = h.Rank;
                        ws5.Cell(hRow, 5).Value = h.Unit;
                        ws5.Cell(hRow, 6).Value = h.ClassName;
                        ws5.Cell(hRow, 7).Value = $"{h.Gpa:F2}";
                        ws5.Cell(hRow, 8).Value = $"{h.TotalCreditsEarned:F2}";
                        ws5.Cell(hRow, 9).Value = h.HonorTitle;
                        ws5.Cell(hRow, 10).Value = $"{h.BestSubject} ({h.BestScore})";

                        ws5.Range(hRow, 1, hRow, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                           .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        if (hRow % 2 == 1) ws5.Range(hRow, 1, hRow, 10).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F0FDF4"));

                        ws5.Cell(hRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws5.Cell(hRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws5.Cell(hRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold().Font.SetFontColor(XLColor.FromHtml("#15803D"));
                        ws5.Cell(hRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws5.Cell(hRow, 9).Style.Font.SetBold();
                        hRow++;
                    }

                    ws5.Columns().AdjustToContents();

                    // =========================================================================
                    // SHEET 6: BẢNG ĐIỂM TÍCH LŨY CHI TIẾT
                    // =========================================================================
                    var ws6 = workbook.Worksheets.Add("Bảng Điểm Tích Lũy Chi Tiết");
                    ws6.Cell("A1").Value = "BẢNG ĐIỂM TÍCH LŨY TOÀN DIỆN CỦA TOÀN BỘ HỌC VIÊN THEO HỆ THỐNG TÍN CHỈ";
                    ws6.Range("A1:J1").Merge().Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] cumHeaders = { "STT", "Mã HV", "Họ và Tên", "Cấp Bậc", "Đơn Vị", "Lớp", "Điểm TB GPA", "Xếp Loại", "TC Đã Tích Lũy", "Tình Trạng Môn" };
                    for (int i = 0; i < cumHeaders.Length; i++)
                    {
                        var cell = ws6.Cell(3, i + 1);
                        cell.Value = cumHeaders[i];
                        cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    int cumRow = 4;
                    int cumStt = 1;
                    foreach (var c in cumulativeCadets)
                    {
                        if (c.HasMissingSubjects)
                        {
                            ws6.Range(cumRow, 1, cumRow, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF9C3");
                        }
                        else if (cumRow % 2 == 1)
                        {
                            ws6.Range(cumRow, 1, cumRow, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                        }

                        ws6.Cell(cumRow, 1).Value = cumStt++;
                        ws6.Cell(cumRow, 2).Value = c.CadetCode;
                        ws6.Cell(cumRow, 3).Value = c.FullName;
                        ws6.Cell(cumRow, 4).Value = c.Rank;
                        ws6.Cell(cumRow, 5).Value = c.Unit;
                        ws6.Cell(cumRow, 6).Value = c.ClassName;
                        ws6.Cell(cumRow, 7).Value = $"{c.Gpa:F2}";
                        ws6.Cell(cumRow, 8).Value = c.AcademicRating;
                        ws6.Cell(cumRow, 9).Value = $"{c.TotalCreditsEarned:F2} / {c.TotalCurriculumCredits:F2}";
                        ws6.Cell(cumRow, 10).Value = c.HasMissingSubjects ? $"Thiếu {c.MissingSubjectsCount} nội dung" : "Đủ 100% môn";

                        ws6.Range(cumRow, 1, cumRow, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                              .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                        ws6.Cell(cumRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws6.Cell(cumRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws6.Cell(cumRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold();
                        ws6.Cell(cumRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws6.Cell(cumRow, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        ws6.Cell(cumRow, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        cumRow++;
                    }

                    ws6.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                    return (true, $"Xuất báo cáo học vụ đa sheet thành công (6 worksheets): {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi xuất báo cáo học vụ đa sheet: {ex.Message}");
                }
            });
        }
        #endregion
    }
}
