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

namespace QL_HocVien.Services
{
    public class CreditSubjectService : ICreditSubjectService
    {
        private readonly AppDbContext _context;

        public CreditSubjectService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CreditSubject>> GetAllSubjectsAsync()
        {
            return await _context.CreditSubjects
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
                .Include(s => s.CreditSubject)
                .AsNoTracking()
                .ToListAsync();

            var subjects = await _context.CreditSubjects.AsNoTracking().ToListAsync();
            var subjectDict = subjects.ToDictionary(s => s.Id);

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
                    ClassName = cadet.MilitaryClass?.ClassName ?? cadet.Unit
                };

                int totalCredits = 0;
                double weightedSum = 0;
                int completedSubjects = 0;

                // Chọn điểm mới nhất hoặc cao nhất cho mỗi môn
                var distinctSubjectScores = cadetScores
                    .GroupBy(s => s.CreditSubjectId)
                    .Select(g => g.OrderByDescending(s => s.ExamDate).First())
                    .ToList();

                foreach (var rec in distinctSubjectScores)
                {
                    dto.SubjectScores[rec.CreditSubjectId] = rec.FinalScore;

                    if (subjectDict.TryGetValue(rec.CreditSubjectId, out var subj))
                    {
                        totalCredits += subj.Credits;
                        weightedSum += rec.FinalScore * subj.Credits;
                        completedSubjects++;
                    }
                }

                dto.TotalCreditsEarned = totalCredits;
                dto.TotalSubjectsCompleted = completedSubjects;
                dto.Gpa = totalCredits > 0 ? Math.Round(weightedSum / totalCredits, 2) : 0;

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

        public async Task<(bool Success, string Message)> ExportAcademicReportAsync(
            string filePath, List<CadetAcademicSummaryDto> summaries, List<CreditSubject> subjects)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Bảng Điểm Tín Chỉ");

                    // Title
                    ws.Cell(1, 1).Value = "HỌC VIỆN QUÂN SỰ - BẢNG TỔNG HỢP ĐIỂM HỌC PHẦN TÍN CHỈ VÀ ĐIỂM TRUNG BÌNH";
                    ws.Range(1, 1, 1, 6 + subjects.Count + 3).Merge();
                    ws.Cell(1, 1).Style.Font.Bold = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 14;
                    ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
                    ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(2, 1).Value = $"Ngày kết xuất: {DateTime.Now:dd/MM/yyyy HH:mm} - Tổng số học viên: {summaries.Count}";
                    ws.Range(2, 1, 2, 6 + subjects.Count + 3).Merge();
                    ws.Cell(2, 1).Style.Font.Italic = true;
                    ws.Cell(2, 1).Style.Font.FontSize = 11;
                    ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Headers
                    int headerRow = 4;
                    ws.Cell(headerRow, 1).Value = "STT";
                    ws.Cell(headerRow, 2).Value = "MÃ HV";
                    ws.Cell(headerRow, 3).Value = "HỌ VÀ TÊN";
                    ws.Cell(headerRow, 4).Value = "CẤP BẬC";
                    ws.Cell(headerRow, 5).Value = "ĐƠN VỊ";
                    ws.Cell(headerRow, 6).Value = "LỚP";

                    int colIdx = 7;
                    foreach (var subj in subjects)
                    {
                        ws.Cell(headerRow, colIdx).Value = $"{subj.SubjectName}\n({subj.Credits} TC - {subj.AssessmentType})";
                        colIdx++;
                    }

                    ws.Cell(headerRow, colIdx).Value = "TỔNG TÍN CHỈ";
                    ws.Cell(headerRow, colIdx + 1).Value = "ĐIỂM TB (GPA)";
                    ws.Cell(headerRow, colIdx + 2).Value = "XẾP LOẠI";

                    var headerRange = ws.Range(headerRow, 1, headerRow, colIdx + 2);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    headerRange.Style.Alignment.WrapText = true;
                    ws.Row(headerRow).Height = 35;

                    // Data
                    int row = 5;
                    int stt = 1;
                    foreach (var item in summaries)
                    {
                        ws.Cell(row, 1).Value = stt++;
                        ws.Cell(row, 2).Value = item.CadetCode;
                        ws.Cell(row, 3).Value = item.FullName;
                        ws.Cell(row, 4).Value = item.Rank;
                        ws.Cell(row, 5).Value = item.Unit;
                        ws.Cell(row, 6).Value = item.ClassName;

                        colIdx = 7;
                        foreach (var subj in subjects)
                        {
                            if (item.SubjectScores.TryGetValue(subj.Id, out var score) && score.HasValue)
                            {
                                ws.Cell(row, colIdx).Value = score.Value;
                                ws.Cell(row, colIdx).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                if (score.Value < 5.0)
                                    ws.Cell(row, colIdx).Style.Font.FontColor = XLColor.Red;
                            }
                            else
                            {
                                ws.Cell(row, colIdx).Value = "-";
                                ws.Cell(row, colIdx).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                ws.Cell(row, colIdx).Style.Font.FontColor = XLColor.Gray;
                            }
                            colIdx++;
                        }

                        ws.Cell(row, colIdx).Value = item.TotalCreditsEarned;
                        ws.Cell(row, colIdx).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Cell(row, colIdx + 1).Value = item.Gpa;
                        ws.Cell(row, colIdx + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, colIdx + 1).Style.Font.Bold = true;

                        ws.Cell(row, colIdx + 2).Value = item.AcademicRating;
                        ws.Cell(row, colIdx + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, colIdx + 2).Style.Font.Bold = true;

                        row++;
                    }

                    ws.Range(headerRow, 1, row - 1, colIdx + 2).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    ws.Range(headerRow, 1, row - 1, colIdx + 2).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    ws.Columns().AdjustToContents();

                    wb.SaveAs(filePath);
                    return (true, $"Đã xuất báo cáo bảng điểm tín chỉ ra file thành công: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi xuất file Excel: {ex.Message}");
                }
            });
        }
    }
}
