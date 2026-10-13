using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Interfaces
{
    public interface ICadetService
    {
        Task<IEnumerable<Cadet>> GetAllCadetsAsync();
        Task<IEnumerable<Cadet>> SearchCadetsAsync(string? keyword, string? rank, string? unit, string? className);
        Task<IEnumerable<Cadet>> SearchCadetsAsync(QL_HocVien.Models.Filters.CadetFilterCriteria criteria);
        Task<Cadet?> GetCadetByIdAsync(int id);
        Task<Cadet?> GetCadetWithRecordsAsync(int id);
        Task<(bool Success, string Message, Cadet? Cadet)> AddCadetAsync(Cadet cadet);
        Task<(bool Success, string Message)> UpdateCadetAsync(Cadet cadet);
        Task<(bool Success, string Message)> DeleteCadetAsync(int id);
        Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleCadetsAsync(IEnumerable<int> cadetIds);
        Task<List<string>> GetDistinctUnitsAsync();
        Task<List<string>> GetDistinctClassesAsync();
        Task<List<string>> GetDistinctRanksAsync();
        Task<List<string>> GetDistinctPositionsAsync();
        Task<List<string>> GetDistinctCohortsAsync();
        Task<List<string>> GetDistinctAcademicYearsAsync();
        Task<string> GenerateSuggestedCadetCodeAsync();

        // Nâng cấp: Quản lý điều chuyển đơn vị & Dòng thời gian đào tạo (Timeline)
        Task<(bool Success, string Message)> TransferCadetAsync(
            int cadetId,
            string newUnit,
            string newSchoolYear,
            int yearOrder,
            string? newCohort = null,
            string? newClassName = null,
            string? reason = null,
            System.DateTime? effectiveDate = null,
            string status = "Đang học");

        Task<List<QL_HocVien.Models.DTOs.CadetTimelineNodeDto>> GetCadetTimelineAsync(int cadetId);
        Task<List<QL_HocVien.Models.Entity.CadetUnitHistory>> GetUnitHistoriesBySchoolYearAsync(string schoolYear);
        Task<List<string>> GetDistinctSchoolYearsFromHistoryAsync();
        Task EnsureInitialUnitHistoryAsync(int cadetId);
    }
}
