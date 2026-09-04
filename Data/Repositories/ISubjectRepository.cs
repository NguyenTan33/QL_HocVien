using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public interface ISubjectRepository : IRepository<Subject>
    {
        Task<IEnumerable<Subject>> SearchSubjectsAsync(string? keyword, string? category);
        Task<Subject?> GetByCodeAsync(string subjectCode);
        Task<bool> ExistsByCodeAsync(string subjectCode);
    }
}
