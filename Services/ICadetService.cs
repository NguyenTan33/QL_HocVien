using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
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
        Task<string> GenerateSuggestedCadetCodeAsync();
    }
}
