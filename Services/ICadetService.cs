using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface ICadetService
    {
        Task<IEnumerable<Cadet>> GetAllCadetsAsync();
        Task<IEnumerable<Cadet>> SearchCadetsAsync(string? keyword, string? rank, string? unit, string? className);
        Task<Cadet?> GetCadetByIdAsync(int id);
        Task<Cadet?> GetCadetWithRecordsAsync(int id);
        Task<(bool Success, string Message, Cadet? Cadet)> AddCadetAsync(Cadet cadet);
        Task<(bool Success, string Message)> UpdateCadetAsync(Cadet cadet);
        Task<(bool Success, string Message)> DeleteCadetAsync(int id);
        Task<string> GenerateSuggestedCadetCodeAsync();
    }
}
