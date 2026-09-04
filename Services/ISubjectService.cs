using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface ISubjectService
    {
        Task<IEnumerable<Subject>> GetAllSubjectsAsync();
        Task<IEnumerable<Subject>> SearchSubjectsAsync(string? keyword, string? category);
        Task<IEnumerable<Subject>> SearchSubjectsAsync(QL_HocVien.Models.Filters.SubjectFilterCriteria criteria);
        Task<Subject?> GetSubjectByIdAsync(int id);
        Task<(bool Success, string Message, Subject? Subject)> AddSubjectAsync(Subject subject);
        Task<(bool Success, string Message)> UpdateSubjectAsync(Subject subject);
        Task<(bool Success, string Message)> DeleteSubjectAsync(int id);
    }
}
