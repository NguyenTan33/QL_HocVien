using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public interface IOfficerRepository : IRepository<Officer>
    {
        Task<IEnumerable<Officer>> SearchOfficersAsync(string? keyword, string? rank, string? unit, string? position);
        Task<Officer?> GetByCodeAsync(string officerCode);
        Task<Officer?> GetOfficerWithDetailsAsync(int id);
        Task<bool> ExistsByCodeAsync(string officerCode);
        Task<int> GetNextOfficerSequenceNumberAsync();
    }
}
