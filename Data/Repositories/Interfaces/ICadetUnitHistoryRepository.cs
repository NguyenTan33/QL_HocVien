using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models.Entity;

namespace QL_HocVien.Data.Repositories.Interfaces
{
    public interface ICadetUnitHistoryRepository : IRepository<CadetUnitHistory>
    {
        Task<List<CadetUnitHistory>> GetByCadetIdAsync(int cadetId);
        Task<List<CadetUnitHistory>> GetBySchoolYearAsync(string schoolYear);
        Task<CadetUnitHistory?> GetLatestByCadetIdAsync(int cadetId);
        Task<CadetUnitHistory?> GetByCadetAndYearAsync(int cadetId, string schoolYear);
        Task<List<string>> GetDistinctSchoolYearsAsync();
    }
}
