using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories.Implementations
{
    public class SubjectRepository : Repository<Subject>, ISubjectRepository
    {
        public SubjectRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Subject>> SearchSubjectsAsync(string? keyword, string? category)
        {
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.SubjectFilterCriteria
            {
                Keyword = keyword,
                Category = category ?? "Táº¥t cáº£"
            });
        }

        public async Task<IEnumerable<Subject>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.SubjectFilterCriteria criteria)
        {
            var query = _context.Subjects.AsQueryable();

            if (criteria == null)
            {
                return await query.OrderBy(s => s.SubjectCode).ToListAsync();
            }

            // 0. Tá»« khÃ³a chung (MÃ£ hoáº·c TÃªn)
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var kw = criteria.Keyword.Trim().ToLower();
                query = query.Where(s => s.SubjectCode.ToLower().Contains(kw) || s.SubjectName.ToLower().Contains(kw));
            }

            // 1. MÃ£ mÃ´n
            if (!string.IsNullOrWhiteSpace(criteria.SubjectCode))
            {
                var code = criteria.SubjectCode.Trim().ToLower();
                query = query.Where(s => s.SubjectCode.ToLower().Contains(code));
            }

            // 2. TÃªn mÃ´n
            if (!string.IsNullOrWhiteSpace(criteria.SubjectName))
            {
                var name = criteria.SubjectName.Trim().ToLower();
                query = query.Where(s => s.SubjectName.ToLower().Contains(name));
            }

            // 3. PhÃ¢n loáº¡i nhÃ³m tá»‘ cháº¥t
            if (!string.IsNullOrWhiteSpace(criteria.Category) && criteria.Category != "Táº¥t cáº£")
            {
                query = query.Where(s => s.Category == criteria.Category);
            }

            // 4. ÄÆ¡n vá»‹ tÃ­nh
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Táº¥t cáº£")
            {
                query = query.Where(s => s.Unit == criteria.Unit);
            }

            // 5. Quy luáº­t thÃ nh tÃ­ch (cÃ ng cao cÃ ng tá»‘t / cÃ ng tháº¥p cÃ ng tá»‘t)
            if (criteria.IsHigherBetter.HasValue)
            {
                query = query.Where(s => s.IsHigherBetter == criteria.IsHigherBetter.Value);
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

