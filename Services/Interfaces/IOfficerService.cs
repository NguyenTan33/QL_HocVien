using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface IOfficerService
    {
        Task<IEnumerable<Officer>> GetAllOfficersAsync();
        Task<IEnumerable<Officer>> SearchOfficersAsync(string? keyword, string? rank, string? unit, string? position);
        Task<IEnumerable<Officer>> SearchOfficersAsync(QL_HocVien.Models.Filters.OfficerFilterCriteria criteria);
        Task<Officer?> GetOfficerByIdAsync(int id);
        Task<Officer?> GetOfficerWithDetailsAsync(int id);
        Task<(bool Success, string Message, Officer? Officer)> AddOfficerAsync(Officer officer, bool createLoginAccount = false, string? rawPassword = null);
        Task<(bool Success, string Message, Officer? Officer)> CreateOfficerAsync(Officer officer, bool createLoginAccount = false, string? rawPassword = null);
        Task<(bool Success, string Message)> UpdateOfficerAsync(Officer officer);
        Task<(bool Success, string Message)> DeleteOfficerAsync(int id);
        Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleOfficersAsync(IEnumerable<int> officerIds);
        Task<(bool Success, string Message)> ResetOfficerPasswordAsync(int officerId, string newPassword);
        Task<string> GenerateSuggestedOfficerCodeAsync();
        Task<string> GenerateNextOfficerCodeAsync();
    }
}
