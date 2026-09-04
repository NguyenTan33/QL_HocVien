using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public class ExcelService : IExcelService
    {
        private readonly AppDbContext _context;
        private readonly ICadetRepository _cadetRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly IPhysicalExamRepository _examRepository;
        private readonly IEvaluationService _evaluationService;

        public ExcelService(
            AppDbContext context,
            ICadetRepository cadetRepository,
            ISubjectRepository subjectRepository,
            IPhysicalExamRepository examRepository,
            IEvaluationService evaluationService)
        {
            _context = context;
            _cadetRepository = cadetRepository;
            _subjectRepository = subjectRepository;
            _examRepository = examRepository;
            _evaluationService = evaluationService;
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

                    var existing = await _cadetRepository.GetByCodeAsync(code);
                    if (existing != null)
                    {
                        existing.FullName = fullName;
                        if (!string.IsNullOrWhiteSpace(rank)) existing.Rank = rank;
                        if (!string.IsNullOrWhiteSpace(pos)) existing.Position = pos;
                        if (!string.IsNullOrWhiteSpace(unit)) existing.Unit = unit;
                        if (!string.IsNullOrWhiteSpace(className)) existing.ClassName = className;
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
                            ClassName = !string.IsNullOrWhiteSpace(className) ? className : "K26A",
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

        #region 4. XUẤT & NHẬP TOÀN BỘ DỮ LIỆU HỆ THỐNG (FULL BACKUP / RESTORE)
        public async Task<(bool Success, string Message)> ExportAllDataToExcelAsync(string filePath)
        {
            try
            {
                var cadets = (await _cadetRepository.GetAllAsync()).ToList();
                var subjects = (await _subjectRepository.GetAllAsync()).ToList();
                var records = (await _examRepository.GetAllWithDetailsAsync()).ToList();
                var failedRecords = records.Where(r => r.Grade == "Không đạt").ToList();

                using var workbook = new XLWorkbook();

                // 1. Sheet Tổng quan (KPI Dashboard)
                var wsDash = workbook.Worksheets.Add("Trang tổng quan");
                wsDash.Cell("A1").Value = "BÁO CÁO TỔNG QUAN QUẢN LÝ HỌC VIÊN & RÈN LUYỆN THỂ LỰC";
                wsDash.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(16)
                    .Font.SetFontColor(XLColor.FromHtml("#1E3A8A")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                wsDash.Cell("A3").Value = "CHỈ SỐ TỔNG HỢP TOÀN ĐƠN VỊ";
                wsDash.Range("A3:C3").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));

                wsDash.Cell("A4").Value = "Tổng quân số học viên:";
                wsDash.Cell("B4").Value = cadets.Count;
                wsDash.Cell("A5").Value = "Tổng số môn rèn luyện:";
                wsDash.Cell("B5").Value = subjects.Count;
                wsDash.Cell("A6").Value = "Tổng số lượt kiểm tra:";
                wsDash.Cell("B6").Value = records.Count;

                double passRate = records.Count > 0 
                    ? Math.Round((double)(records.Count - failedRecords.Count) / records.Count * 100, 1) 
                    : 100.0;
                wsDash.Cell("A7").Value = "Tỷ lệ đạt chuẩn quân sự:";
                wsDash.Cell("B7").Value = $"{passRate}%";

                wsDash.Cell("A9").Value = "PHÂN LOẠI XẾP LOẠI CHI TIẾT";
                wsDash.Range("A9:C9").Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));

                wsDash.Cell("A10").Value = "Xuất sắc:";
                wsDash.Cell("B10").Value = records.Count(r => r.Grade == "Xuất sắc");
                wsDash.Cell("A11").Value = "Giỏi:";
                wsDash.Cell("B11").Value = records.Count(r => r.Grade == "Giỏi");
                wsDash.Cell("A12").Value = "Khá:";
                wsDash.Cell("B12").Value = records.Count(r => r.Grade == "Khá");
                wsDash.Cell("A13").Value = "Đạt:";
                wsDash.Cell("B13").Value = records.Count(r => r.Grade == "Đạt");
                wsDash.Cell("A14").Value = "Không đạt (Cần rèn luyện thêm):";
                wsDash.Cell("B14").Value = failedRecords.Count;
                wsDash.Columns().AdjustToContents();

                // 2. Sheet Học viên
                var wsCadet = workbook.Worksheets.Add("Trang học viên");
                wsCadet.Cell("A1").Value = "DANH SÁCH HỌC VIÊN TOÀN ĐƠN VỊ";
                wsCadet.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] cHeaders = { "STT", "Mã học viên", "Họ và tên", "Cấp bậc", "Chức vụ", "Đơn vị", "Lớp", "Số điện thoại", "Email", "Tuổi", "Giới tính" };
                for (int i = 0; i < cHeaders.Length; i++)
                {
                    wsCadet.Cell(3, i + 1).Value = cHeaders[i];
                    wsCadet.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                int rRow = 4;
                for (int i = 0; i < cadets.Count; i++)
                {
                    var c = cadets[i];
                    wsCadet.Cell(rRow, 1).Value = i + 1;
                    wsCadet.Cell(rRow, 2).Value = c.CadetCode;
                    wsCadet.Cell(rRow, 3).Value = c.FullName;
                    wsCadet.Cell(rRow, 4).Value = c.Rank;
                    wsCadet.Cell(rRow, 5).Value = c.Position;
                    wsCadet.Cell(rRow, 6).Value = c.Unit;
                    wsCadet.Cell(rRow, 7).Value = c.ClassName;
                    wsCadet.Cell(rRow, 8).Value = c.PhoneNumber;
                    wsCadet.Cell(rRow, 9).Value = c.Email;
                    wsCadet.Cell(rRow, 10).Value = c.Age ?? 0;
                    wsCadet.Cell(rRow, 11).Value = c.Gender;
                    rRow++;
                }
                wsCadet.Columns().AdjustToContents();

                // 3. Sheet Môn học
                var wsSub = workbook.Worksheets.Add("Trang môn học");
                wsSub.Cell("A1").Value = "DANH MỤC TIÊU CHUẨN RÈN LUYỆN THỂ LỰC";
                wsSub.Range("A1:I1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
                string[] sHeaders = { "STT", "Mã môn", "Tên môn", "Nhóm tố chất", "Đơn vị tính", "Chuẩn Giỏi", "Chuẩn Khá", "Chuẩn Đạt", "Càng cao càng tốt" };
                for (int i = 0; i < sHeaders.Length; i++)
                {
                    wsSub.Cell(3, i + 1).Value = sHeaders[i];
                    wsSub.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"));
                }
                int subRow = 4;
                for (int i = 0; i < subjects.Count; i++)
                {
                    var s = subjects[i];
                    wsSub.Cell(subRow, 1).Value = i + 1;
                    wsSub.Cell(subRow, 2).Value = s.SubjectCode;
                    wsSub.Cell(subRow, 3).Value = s.SubjectName;
                    wsSub.Cell(subRow, 4).Value = s.Category;
                    wsSub.Cell(subRow, 5).Value = s.Unit;
                    wsSub.Cell(subRow, 6).Value = s.ExcellentThreshold;
                    wsSub.Cell(subRow, 7).Value = s.GoodThreshold;
                    wsSub.Cell(subRow, 8).Value = s.PassThreshold;
                    wsSub.Cell(subRow, 9).Value = s.IsHigherBetter ? "Có" : "Không";
                    subRow++;
                }
                wsSub.Columns().AdjustToContents();

                // 4. Sheet Kiểm tra thể lực
                var wsExam = workbook.Worksheets.Add("Kiểm tra thể lực");
                wsExam.Cell("A1").Value = "BẢNG KẾT QUẢ KIỂM TRA ĐỊNH KỲ";
                wsExam.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#15803D"));
                string[] eHeaders = { "STT", "Mã học viên", "Họ và tên", "Đơn vị", "Lớp", "Mã môn", "Tên môn", "Thành tích", "Xếp loại", "Đợt kiểm tra", "Ngày kiểm tra" };
                for (int i = 0; i < eHeaders.Length; i++)
                {
                    wsExam.Cell(3, i + 1).Value = eHeaders[i];
                    wsExam.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#15803D"));
                }
                int exRow = 4;
                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];
                    wsExam.Cell(exRow, 1).Value = i + 1;
                    wsExam.Cell(exRow, 2).Value = r.Cadet?.CadetCode ?? "";
                    wsExam.Cell(exRow, 3).Value = r.Cadet?.FullName ?? "";
                    wsExam.Cell(exRow, 4).Value = r.Cadet?.Unit ?? "";
                    wsExam.Cell(exRow, 5).Value = r.Cadet?.ClassName ?? "";
                    wsExam.Cell(exRow, 6).Value = r.Subject?.SubjectCode ?? "";
                    wsExam.Cell(exRow, 7).Value = r.Subject?.SubjectName ?? "";
                    wsExam.Cell(exRow, 8).Value = r.ScoreValue;
                    wsExam.Cell(exRow, 9).Value = r.Grade;
                    wsExam.Cell(exRow, 10).Value = r.ExamSession;
                    wsExam.Cell(exRow, 11).Value = r.ExamDate.ToString("dd/MM/yyyy");
                    exRow++;
                }
                wsExam.Columns().AdjustToContents();

                // 5. Sheet Học viên chưa đạt (Rèn luyện bổ sung)
                var wsFail = workbook.Worksheets.Add("Rèn luyện bổ sung");
                wsFail.Cell("A1").Value = "DANH SÁCH HỌC VIÊN CHƯA ĐẠT CẦN RÈN LUYỆN BỔ SUNG";
                wsFail.Range("A1:H1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#DC2626"));
                string[] fHeaders = { "STT", "Mã học viên", "Họ và tên", "Đơn vị", "Lớp", "Nội dung chưa đạt", "Thành tích", "Ngày kiểm tra" };
                for (int i = 0; i < fHeaders.Length; i++)
                {
                    wsFail.Cell(3, i + 1).Value = fHeaders[i];
                    wsFail.Cell(3, i + 1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#DC2626"));
                }
                int fRow = 4;
                for (int i = 0; i < failedRecords.Count; i++)
                {
                    var f = failedRecords[i];
                    wsFail.Cell(fRow, 1).Value = i + 1;
                    wsFail.Cell(fRow, 2).Value = f.Cadet?.CadetCode ?? "";
                    wsFail.Cell(fRow, 3).Value = f.Cadet?.FullName ?? "";
                    wsFail.Cell(fRow, 4).Value = f.Cadet?.Unit ?? "";
                    wsFail.Cell(fRow, 5).Value = f.Cadet?.ClassName ?? "";
                    wsFail.Cell(fRow, 6).Value = f.Subject?.SubjectName ?? "";
                    wsFail.Cell(fRow, 7).Value = f.ScoreValue;
                    wsFail.Cell(fRow, 8).Value = f.ExamDate.ToString("dd/MM/yyyy");
                    fRow++;
                }
                wsFail.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
                return (true, $"Xuất toàn bộ dữ liệu thành công ra file Excel ({cadets.Count} học viên, {subjects.Count} môn, {records.Count} lượt kiểm tra trên 5 sheets).");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xuất toàn bộ dữ liệu: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int CadetsCount, int SubjectsCount, int ExamsCount)> ImportAllDataFromExcelAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "Tệp không tồn tại.", 0, 0, 0);

                // 1. Nhập môn học trước
                var subResult = await ImportSubjectsFromExcelAsync(filePath);
                // 2. Nhập học viên
                var cadetResult = await ImportCadetsFromExcelAsync(filePath);
                // 3. Nhập kết quả kiểm tra
                var examResult = await ImportExamRecordsFromExcelAsync(filePath);

                int cCount = cadetResult.Cadets.Count;
                int sCount = subResult.Subjects.Count;
                int eCount = examResult.Records.Count;

                return (true, $"Khôi phục/Nhập toàn bộ thành công: {cCount} học viên, {sCount} môn học, {eCount} lượt kiểm tra thể lực.", cCount, sCount, eCount);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi nhập toàn bộ dữ liệu: {ex.Message}", 0, 0, 0);
            }
        }
        #endregion
    }
}
