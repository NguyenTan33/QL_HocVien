using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public interface ICadetRepository : IRepository<Cadet>
    {
        Task<IEnumerable<Cadet>> SearchCadetsAsync(string? keyword, string? rank, string? unit, string? className);
        Task<IEnumerable<Cadet>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CadetFilterCriteria criteria);
        Task<Cadet?> GetByCodeAsync(string cadetCode);
        Task<Cadet?> GetCadetWithRecordsAsync(int id);
        Task<bool> ExistsByCodeAsync(string cadetCode);
        Task<int> GetNextCadetSequenceNumberAsync(int year);
    }
}
