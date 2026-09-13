using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models.Entity;

namespace QL_HocVien.Services.Interfaces
{
    /// <summary>
    /// Service quản lý Khóa học (AcademicCohort): K75, K26, K27...
    /// </summary>
    public interface ICohortService
    {
        Task<IEnumerable<AcademicCohort>> GetAllCohortsAsync();
        Task<AcademicCohort?> GetCohortByIdAsync(int id);
        Task<AcademicCohort?> GetCohortByCodeAsync(string cohortCode);

        /// <summary>
        /// Thêm khóa học mới.
        /// </summary>
        Task<(bool Success, string Message, AcademicCohort? Cohort)> AddCohortAsync(AcademicCohort cohort);

        Task<(bool Success, string Message)> UpdateCohortAsync(AcademicCohort cohort);
        Task<(bool Success, string Message)> DeleteCohortAsync(int id);

        /// <summary>
        /// Tự động phân tích mã học viên dạng "ĐH.075.299" và trả về mã khóa học (ví dụ "K75").
        /// Trả về null nếu không parse được.
        /// </summary>
        string? ParseCohortCodeFromCadetCode(string cadetCode);

        /// <summary>
        /// Tự động gán CohortId cho học viên dựa vào mã học viên (CadetCode).
        /// Ví dụ: "ĐH.075.299" → tìm AcademicCohort có CohortNumber=75 → gán CohortId.
        /// </summary>
        Task<AcademicCohort?> ResolveCohortFromCadetCodeAsync(string cadetCode);

        /// <summary>
        /// Đồng bộ CohortId cho toàn bộ học viên trong DB dựa vào mã học viên.
        /// </summary>
        Task<(int Synced, int Skipped)> SyncAllCadetCohortIdsAsync();
    }
}
