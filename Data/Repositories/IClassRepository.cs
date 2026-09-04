using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public interface IClassRepository : IRepository<MilitaryClass>
    {
        Task<IEnumerable<MilitaryClass>> SearchClassesAsync(string? keyword, string? unit, string? major);
        Task<IEnumerable<MilitaryClass>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.ClassFilterCriteria criteria);
        Task<MilitaryClass?> GetByCodeAsync(string classCode);
        Task<MilitaryClass?> GetClassWithCadetsAsync(int id);
        Task<bool> ExistsByCodeAsync(string classCode);
        Task<IEnumerable<MilitaryClass>> GetAllWithCadetsAsync();
    }
}
