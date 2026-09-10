using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface IClassService
    {
        Task<IEnumerable<MilitaryClass>> GetAllClassesAsync();
        Task<IEnumerable<MilitaryClass>> SearchClassesAsync(string? keyword, string? unit, string? major);
        Task<IEnumerable<MilitaryClass>> SearchClassesAsync(QL_HocVien.Models.Filters.ClassFilterCriteria criteria);
        Task<MilitaryClass?> GetClassByIdAsync(int id);
        Task<MilitaryClass?> GetClassWithCadetsAsync(int id);
        Task<(bool Success, string Message, MilitaryClass? Class)> AddClassAsync(MilitaryClass militaryClass);
        Task<(bool Success, string Message)> UpdateClassAsync(MilitaryClass militaryClass);
        Task<(bool Success, string Message)> DeleteClassAsync(int id);
        Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleClassesAsync(IEnumerable<int> classIds);
    }
}
