using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface ICatalogService
    {
        // 1. Cấp bậc
        Task<IEnumerable<MilitaryRank>> GetAllRanksAsync();
        Task<IEnumerable<MilitaryRank>> SearchRanksAsync(string? keyword, string? group);
        Task<MilitaryRank?> GetRankByIdAsync(int id);
        Task<(bool Success, string Message, MilitaryRank? Rank)> AddRankAsync(MilitaryRank rank);
        Task<(bool Success, string Message)> UpdateRankAsync(MilitaryRank rank);
        Task<(bool Success, string Message)> DeleteRankAsync(int id);
        Task<List<string>> GetRankNamesAsync();
        Task<List<string>> GetRankDropdownAsync();

        // 2. Chức vụ
        Task<IEnumerable<MilitaryPosition>> GetAllPositionsAsync();
        Task<IEnumerable<MilitaryPosition>> SearchPositionsAsync(string? keyword, string? group);
        Task<MilitaryPosition?> GetPositionByIdAsync(int id);
        Task<(bool Success, string Message, MilitaryPosition? Position)> AddPositionAsync(MilitaryPosition position);
        Task<(bool Success, string Message)> UpdatePositionAsync(MilitaryPosition position);
        Task<(bool Success, string Message)> DeletePositionAsync(int id);
        Task<List<string>> GetPositionNamesAsync();
        Task<List<string>> GetPositionDropdownAsync();

        // 3. Đơn vị
        Task<IEnumerable<MilitaryUnit>> GetAllUnitsAsync();
        Task<IEnumerable<MilitaryUnit>> SearchUnitsAsync(string? keyword, string? parentUnit);
        Task<MilitaryUnit?> GetUnitByIdAsync(int id);
        Task<(bool Success, string Message, MilitaryUnit? Unit)> AddUnitAsync(MilitaryUnit unit);
        Task<(bool Success, string Message)> UpdateUnitAsync(MilitaryUnit unit);
        Task<(bool Success, string Message)> DeleteUnitAsync(int id);
        Task<List<string>> GetUnitNamesAsync();
        Task<List<string>> GetUnitDropdownAsync();

        // 4. Chuyên ngành
        Task<IEnumerable<MilitaryMajor>> GetAllMajorsAsync();
        Task<IEnumerable<MilitaryMajor>> SearchMajorsAsync(string? keyword, string? department);
        Task<MilitaryMajor?> GetMajorByIdAsync(int id);
        Task<(bool Success, string Message, MilitaryMajor? Major)> AddMajorAsync(MilitaryMajor major);
        Task<(bool Success, string Message)> UpdateMajorAsync(MilitaryMajor major);
        Task<(bool Success, string Message)> DeleteMajorAsync(int id);
        Task<List<string>> GetMajorNamesAsync();
        Task<List<string>> GetMajorDropdownAsync();
    }
}
