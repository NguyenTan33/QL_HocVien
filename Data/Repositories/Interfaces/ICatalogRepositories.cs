using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public interface IRankRepository : IRepository<MilitaryRank>
    {
        Task<IEnumerable<MilitaryRank>> SearchRanksAsync(string? keyword, string? group);
        Task<IEnumerable<MilitaryRank>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria);
        Task<MilitaryRank?> GetByCodeAsync(string rankCode);
        Task<bool> ExistsByCodeAsync(string rankCode);
    }

    public interface IPositionRepository : IRepository<MilitaryPosition>
    {
        Task<IEnumerable<MilitaryPosition>> SearchPositionsAsync(string? keyword, string? group);
        Task<IEnumerable<MilitaryPosition>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria);
        Task<MilitaryPosition?> GetByCodeAsync(string positionCode);
        Task<bool> ExistsByCodeAsync(string positionCode);
    }

    public interface IUnitRepository : IRepository<MilitaryUnit>
    {
        Task<IEnumerable<MilitaryUnit>> SearchUnitsAsync(string? keyword, string? parentUnit);
        Task<IEnumerable<MilitaryUnit>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria);
        Task<MilitaryUnit?> GetByCodeAsync(string unitCode);
        Task<bool> ExistsByCodeAsync(string unitCode);
    }

    public interface IMajorRepository : IRepository<MilitaryMajor>
    {
        Task<IEnumerable<MilitaryMajor>> SearchMajorsAsync(string? keyword, string? department);
        Task<IEnumerable<MilitaryMajor>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria);
        Task<MilitaryMajor?> GetByCodeAsync(string majorCode);
        Task<bool> ExistsByCodeAsync(string majorCode);
    }
}
