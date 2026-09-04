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

namespace QL_HocVien.Services
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

        #region 1. XUẤT & NHẬP HỌC VIÊN
        public async Task<(bool Success, string Message)> ExportCadetsToExcelAsync(IEnumerable<Cadet> cadets, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Trang học viên");

                // Tiêu đề lớn
                ws.Cell("A1").Value = "DANH SÁCH HỌC VIÊN QUÂN ĐỘI";
                ws.Range("A1:L1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell("A2").Value = $"Thời điểm xuất dữ liệu: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                ws.Range("A2:L2").Merge().Style.Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Hàng Header
                string[] headers = { "STT", "Mã học viên", "Họ và tên", "Cấp bậc", "Chức vụ", "Đơn vị", "Lớp", "Số điện thoại", "Email", "Ngày sinh", "Tuổi", "Giới tính" };
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
                foreach (var c in cadets)
                {
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = c.CadetCode;
                    ws.Cell(row, 3).Value = c.FullName;
                    ws.Cell(row, 4).Value = c.Rank;
                    ws.Cell(row, 5).Value = c.Position;
                    ws.Cell(row, 6).Value = c.Unit;
                    ws.Cell(row, 7).Value = c.ClassName;
                    ws.Cell(row, 8).Value = c.PhoneNumber;
                    ws.Cell(row, 9).Value = c.Email;
                    ws.Cell(row, 10).Value = c.DateOfBirth.HasValue ? c.DateOfBirth.Value.ToString("dd/MM/yyyy") : "";
                    ws.Cell(row, 11).Value = c.Age ?? 0;
                    ws.Cell(row, 12).Value = c.Gender;

                    ws.Range(row, 1, row, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                   .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);

                return (true, $"Xuất thành công {stt - 1} học viên ra file Excel.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xuất file Excel: {ex.Message}");
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
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => c.GetString()));
                    if (textRow.Contains("Họ và tên") || textRow.Contains("Mã học viên"))
                    {
                        headerRow = r;
                        break;
                    }
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
                int addedCount = 0;
                int updatedCount = 0;
                var allClasses = (await _classRepository.GetAllAsync()).ToList();

                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    // Đọc các cột
                    string code = row.Cell(2).GetString().Trim();
                    string fullName = row.Cell(3).GetString().Trim();

                    // Nếu mã rỗng hoặc chỉ có số STT mà không có họ tên thì bỏ qua
                    if (string.IsNullOrWhiteSpace(fullName)) continue;
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        code = $"HV-{DateTime.Today.Year}-{r:D3}";
                    }

                    string rank = row.Cell(4).GetString().Trim();
                    string pos = row.Cell(5).GetString().Trim();
                    string unit = row.Cell(6).GetString().Trim();
                    string className = row.Cell(7).GetString().Trim();
                    string phone = row.Cell(8).GetString().Trim();
                    string email = row.Cell(9).GetString().Trim();
                    string dobStr = row.Cell(10).GetString().Trim();
                    DateTime? dob = null;
                    if (DateTime.TryParse(dobStr, out var d)) dob = d;
                    
                    int.TryParse(row.Cell(11).GetString().Trim(), out int age);
                    string gender = row.Cell(12).GetString().Trim();
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
                            Rank = !string.IsNullOrWhiteSpace(rank) ? rank : "Binh nhì",
                            Position = !string.IsNullOrWhiteSpace(pos) ? pos : "Học viên",
                            Unit = !string.IsNullOrWhiteSpace(unit) ? unit : "Đại đội 1",
                            ClassId = matchedClass?.Id,
                            ClassName = matchedClass?.ClassName ?? (!string.IsNullOrWhiteSpace(className) ? className : "K26A"),
                            PhoneNumber = !string.IsNullOrWhiteSpace(phone) ? phone : $"09{new Random().Next(10000000, 99999999)}",
                            Email = !string.IsNullOrWhiteSpace(email) ? email : $"{code.ToLower().Replace("-", "")}@hocvien.edu.vn",
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
                for (int r = 1; r <= 10; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => c.GetString()));
                    if (textRow.Contains("Mã môn") || textRow.Contains("Tên môn"))
                    {
                        headerRow = r;
                        break;
                    }
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
                int added = 0;
                int updated = 0;

                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string code = row.Cell(2).GetString().Trim();
                    string name = row.Cell(3).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

                    string cat = row.Cell(4).GetString().Trim();
                    string unit = row.Cell(5).GetString().Trim();
                    double.TryParse(row.Cell(6).GetString(), out double exc);
                    double.TryParse(row.Cell(7).GetString(), out double good);
                    double.TryParse(row.Cell(8).GetString(), out double pass);
                    string higher = row.Cell(9).GetString().Trim().ToLower();
                    bool isHigher = higher == "có" || higher == "yes" || higher == "true" || higher == "1";
                    string desc = row.Cell(10).GetString().Trim();

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
                for (int r = 1; r <= 10; r++)
                {
                    var textRow = string.Join(" ", ws.Row(r).Cells().Select(c => c.GetString()));
                    if (textRow.Contains("Mã học viên") || textRow.Contains("Mã môn") || textRow.Contains("Thành tích"))
                    {
                        headerRow = r;
                        break;
                    }
                }

                var allCadets = (await _cadetRepository.GetAllAsync()).ToDictionary(c => c.CadetCode.ToLower().Trim());
                var allSubjects = (await _subjectRepository.GetAllAsync()).ToDictionary(s => s.SubjectCode.ToLower().Trim());

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
                int added = 0;

                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string cadetCode = row.Cell(2).GetString().Trim().ToLower();
                    string subjectCode = row.Cell(6).GetString().Trim().ToLower();

                    if (!allCadets.TryGetValue(cadetCode, out var cadet)) continue;
                    if (!allSubjects.TryGetValue(subjectCode, out var subject)) continue;

                    double.TryParse(row.Cell(8).GetString(), out double score);
                    string session = row.Cell(10).GetString().Trim();
                    string dateStr = row.Cell(11).GetString().Trim();
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
                int startRow = 5;
                for (int r = 1; r <= 10; r++)
                {
                    var text = ws.Cell(r, 2).GetString().Trim();
                    if (text.Equals("Mã cán bộ", StringComparison.OrdinalIgnoreCase) || text.Equals("OfficerCode", StringComparison.OrdinalIgnoreCase))
                    {
                        startRow = r + 1;
                        break;
                    }
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                for (int row = startRow; row <= lastRow; row++)
                {
                    string code = ws.Cell(row, 2).GetString().Trim();
                    string name = ws.Cell(row, 3).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                        continue;

                    string rank = ws.Cell(row, 4).GetString().Trim();
                    string pos = ws.Cell(row, 5).GetString().Trim();
                    string unit = ws.Cell(row, 6).GetString().Trim();
                    string phone = ws.Cell(row, 7).GetString().Trim();
                    string email = ws.Cell(row, 8).GetString().Trim();
                    string specialty = ws.Cell(row, 9).GetString().Trim();
                    string dobStr = ws.Cell(row, 10).GetString().Trim();
                    string enlistStr = ws.Cell(row, 11).GetString().Trim();

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

                workbook.SaveAs(filePath);
                return (true, $"Xuất toàn bộ dữ liệu thành công ra file Excel ({classes.Count} lớp học, {cadets.Count} học viên, {officers.Count} cán bộ, {subjects.Count} môn, {records.Count} lượt kiểm tra trên 11 sheets).");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xuất toàn bộ dữ liệu: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int ClassesCount, int CadetsCount, int SubjectsCount, int ExamsCount, int OfficersCount)> ImportAllDataFromExcelAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tệp không tồn tại.", 0, 0, 0, 0, 0);

                var secCheck = await ValidateExcelSecurityAsync(filePath);
                if (!secCheck.IsValid)
                    return (false, secCheck.Message, 0, 0, 0, 0, 0);

                // 1. Nhập danh mục tổ chức trước
                var catResult = await ImportCatalogsFromExcelAsync(filePath);
                // 2. Nhập cán bộ
                var offResult = await ImportOfficersFromExcelAsync(filePath);
                // 3. Nhập lớp học
                var classResult = await ImportClassesFromExcelAsync(filePath);
                // 4. Nhập môn học
                var subResult = await ImportSubjectsFromExcelAsync(filePath);
                // 5. Nhập học viên
                var cadetResult = await ImportCadetsFromExcelAsync(filePath);
                // 4. Nhập kết quả kiểm tra
                var examResult = await ImportExamRecordsFromExcelAsync(filePath);

                int clCount = classResult.Classes.Count;
                int cCount = cadetResult.Cadets.Count;
                int sCount = subResult.Subjects.Count;
                int eCount = examResult.Records.Count;
                int offCount = offResult.Officers.Count;

                return (true, $"Khôi phục/Nhập toàn bộ thành công: {clCount} lớp học, {cCount} học viên, {offCount} cán bộ, {sCount} môn học, {eCount} lượt kiểm tra thể lực.", clCount, cCount, sCount, eCount, offCount);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi nhập toàn bộ dữ liệu: {ex.Message}", 0, 0, 0, 0, 0);
            }
        }
        #endregion
    }
}
