using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Models.Entity;
using QL_HocVien.Services.Interfaces;

namespace QL_HocVien.Services.Implementations
{
    /// <summary>
    /// Service quản lý Khóa học (AcademicCohort): K75, K26, K27...
    /// Hỗ trợ parse mã học viên dạng "ĐH.075.299" để tự động xác định khóa học.
    /// </summary>
    public class CohortService : ICohortService
    {
        private readonly AppDbContext _context;

        // Regex để parse mã học viên: tiền tố chữ cái rồi dấu chấm + 3 số + dấu chấm + số
        // Ví dụ: ĐH.075.299 → group 1 = "075"
        private static readonly Regex CadetCodeRegex = new(
            @"^[A-Za-zÀ-ỹĐđ]+\.(\d{3})\.\d+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public CohortService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AcademicCohort>> GetAllCohortsAsync()
        {
            var cohorts = await _context.AcademicCohorts
                .OrderBy(c => c.CohortNumber)
                .ToListAsync();

            // Tính số học viên thực tế
            foreach (var cohort in cohorts)
            {
                cohort.CadetCount = await _context.Cadets
                    .CountAsync(c => c.CohortId == cohort.Id);
            }

            return cohorts;
        }

        public async Task<AcademicCohort?> GetCohortByIdAsync(int id)
        {
            return await _context.AcademicCohorts
                .Include(c => c.Cadets)
                .Include(c => c.Classes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<AcademicCohort?> GetCohortByCodeAsync(string cohortCode)
        {
            return await _context.AcademicCohorts
                .FirstOrDefaultAsync(c => c.CohortCode == cohortCode);
        }

        public async Task<(bool Success, string Message, AcademicCohort? Cohort)> AddCohortAsync(AcademicCohort cohort)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cohort.CohortCode))
                    return (false, "Mã khóa học không được để trống.", null);

                if (string.IsNullOrWhiteSpace(cohort.CohortName))
                    return (false, "Tên khóa học không được để trống.", null);

                // Chuẩn hóa mã khóa học
                cohort.CohortCode = cohort.CohortCode.Trim().ToUpper();

                var existing = await _context.AcademicCohorts
                    .FirstOrDefaultAsync(c => c.CohortCode == cohort.CohortCode);
                if (existing != null)
                    return (false, $"Mã khóa học '{cohort.CohortCode}' đã tồn tại trong hệ thống.", null);

                // Tự động trích xuất số khóa từ mã (ví dụ: K75 → 75, K075 → 75)
                if (cohort.CohortNumber == 0)
                {
                    var numMatch = Regex.Match(cohort.CohortCode, @"\d+");
                    if (numMatch.Success && int.TryParse(numMatch.Value, out int num))
                        cohort.CohortNumber = num;
                }

                cohort.CreatedAt = DateTime.Now;
                _context.AcademicCohorts.Add(cohort);
                await _context.SaveChangesAsync();

                return (true, $"Đã thêm khóa học '{cohort.CohortName}' thành công.", cohort);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi thêm khóa học: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateCohortAsync(AcademicCohort cohort)
        {
            try
            {
                var existing = await _context.AcademicCohorts.FindAsync(cohort.Id);
                if (existing == null)
                    return (false, "Không tìm thấy khóa học cần cập nhật.");

                // Kiểm tra mã khóa học trùng (nếu đổi mã)
                cohort.CohortCode = cohort.CohortCode.Trim().ToUpper();
                var duplicate = await _context.AcademicCohorts
                    .FirstOrDefaultAsync(c => c.CohortCode == cohort.CohortCode && c.Id != cohort.Id);
                if (duplicate != null)
                    return (false, $"Mã khóa học '{cohort.CohortCode}' đã được sử dụng bởi khóa khác.");

                existing.CohortCode = cohort.CohortCode;
                existing.CohortName = cohort.CohortName;
                existing.CohortNumber = cohort.CohortNumber;
                existing.EnrollmentYear = cohort.EnrollmentYear;
                existing.GraduationYear = cohort.GraduationYear;
                existing.AcademicYear = cohort.AcademicYear;
                existing.Description = cohort.Description;

                await _context.SaveChangesAsync();
                return (true, $"Đã cập nhật khóa học '{existing.CohortName}' thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi cập nhật khóa học: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteCohortAsync(int id)
        {
            try
            {
                var cohort = await _context.AcademicCohorts.FindAsync(id);
                if (cohort == null)
                    return (false, "Không tìm thấy khóa học cần xóa.");

                int cadetCount = await _context.Cadets.CountAsync(c => c.CohortId == id);
                if (cadetCount > 0)
                    return (false, $"Không thể xóa khóa học '{cohort.CohortName}' vì hiện có {cadetCount} học viên đang thuộc khóa này. Hãy chuyển học viên sang khóa khác trước.");

                _context.AcademicCohorts.Remove(cohort);
                await _context.SaveChangesAsync();
                return (true, $"Đã xóa khóa học '{cohort.CohortName}' thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xóa khóa học: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse mã học viên dạng "ĐH.075.299" → trả về "K75".
        /// Hỗ trợ: ĐH.075.299 (K75), ĐH.026.001 (K26), HV.030.100 (K30)...
        /// </summary>
        public string? ParseCohortCodeFromCadetCode(string cadetCode)
        {
            if (string.IsNullOrWhiteSpace(cadetCode)) return null;

            var match = CadetCodeRegex.Match(cadetCode.Trim());
            if (!match.Success) return null;

            string numStr = match.Groups[1].Value; // "075"
            if (!int.TryParse(numStr, out int num)) return null;

            return $"K{num}"; // "K75"
        }

        public async Task<AcademicCohort?> ResolveCohortFromCadetCodeAsync(string cadetCode)
        {
            var cohortCode = ParseCohortCodeFromCadetCode(cadetCode);
            if (cohortCode == null) return null;

            // Tìm theo mã K75 hoặc theo số khóa (75)
            var numMatch = Regex.Match(cadetCode, @"\.(\d{3})\.");
            if (!numMatch.Success) return null;
            int cohortNum = int.Parse(numMatch.Groups[1].Value);

            return await _context.AcademicCohorts
                .FirstOrDefaultAsync(c => c.CohortCode == cohortCode || c.CohortNumber == cohortNum);
        }

        public async Task<(int Synced, int Skipped)> SyncAllCadetCohortIdsAsync()
        {
            int synced = 0, skipped = 0;

            var cadets = await _context.Cadets
                .Where(c => c.CohortId == null && !string.IsNullOrEmpty(c.CadetCode))
                .ToListAsync();

            var allCohorts = await _context.AcademicCohorts.ToListAsync();

            foreach (var cadet in cadets)
            {
                var cohortCode = ParseCohortCodeFromCadetCode(cadet.CadetCode);
                if (cohortCode == null) { skipped++; continue; }

                // Tìm theo số khóa
                var numMatch = Regex.Match(cadet.CadetCode, @"\.(\d{3})\.");
                if (!numMatch.Success) { skipped++; continue; }

                int cohortNum = int.Parse(numMatch.Groups[1].Value);
                var cohort = allCohorts.FirstOrDefault(c =>
                    c.CohortCode.Equals(cohortCode, StringComparison.OrdinalIgnoreCase)
                    || c.CohortNumber == cohortNum);

                if (cohort == null) { skipped++; continue; }

                cadet.CohortId = cohort.Id;
                // Cũng cập nhật Cohort string nếu rỗng
                if (string.IsNullOrEmpty(cadet.Cohort))
                    cadet.Cohort = cohort.CohortCode;

                synced++;
            }

            if (synced > 0)
                await _context.SaveChangesAsync();

            return (synced, skipped);
        }
    }
}
