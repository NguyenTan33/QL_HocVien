using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public class SubjectRepository : Repository<Subject>, ISubjectRepository
    {
        public SubjectRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Subject>> SearchSubjectsAsync(string? keyword, string? category)
        {
            var query = _context.Subjects.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(s => s.SubjectName.ToLower().Contains(kw) ||
                                         s.SubjectCode.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
            {
                query = query.Where(s => s.Category == category);
            }

            return await query.OrderBy(s => s.SubjectCode).ToListAsync();
        }

        public async Task<Subject?> GetByCodeAsync(string subjectCode)
        {
            if (string.IsNullOrWhiteSpace(subjectCode))
                return null;

            return await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectCode.ToLower() == subjectCode.Trim().ToLower());
        }

        public async Task<bool> ExistsByCodeAsync(string subjectCode)
        {
            return await _context.Subjects.AnyAsync(s => s.SubjectCode.ToLower() == subjectCode.Trim().ToLower());
        }
    }
}
