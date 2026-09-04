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
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.SubjectFilterCriteria
            {
                Keyword = keyword,
                Category = category ?? "Tất cả"
            });
        }

        public async Task<IEnumerable<Subject>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.SubjectFilterCriteria criteria)
        {
            var query = _context.Subjects.AsQueryable();

            if (criteria == null)
            {
                return await query.OrderBy(s => s.SubjectCode).ToListAsync();
            }

            // 0. Từ khóa chung (Mã hoặc Tên)
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var kw = criteria.Keyword.Trim().ToLower();
                query = query.Where(s => s.SubjectCode.ToLower().Contains(kw) || s.SubjectName.ToLower().Contains(kw));
            }

            // 1. Mã môn
            if (!string.IsNullOrWhiteSpace(criteria.SubjectCode))
            {
                var code = criteria.SubjectCode.Trim().ToLower();
                query = query.Where(s => s.SubjectCode.ToLower().Contains(code));
            }

            // 2. Tên môn
            if (!string.IsNullOrWhiteSpace(criteria.SubjectName))
            {
                var name = criteria.SubjectName.Trim().ToLower();
                query = query.Where(s => s.SubjectName.ToLower().Contains(name));
            }

            // 3. Phân loại nhóm tố chất
            if (!string.IsNullOrWhiteSpace(criteria.Category) && criteria.Category != "Tất cả")
            {
                query = query.Where(s => s.Category == criteria.Category);
            }

            // 4. Đơn vị tính
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
            {
                query = query.Where(s => s.Unit == criteria.Unit);
            }

            // 5. Quy luật thành tích (càng cao càng tốt / càng thấp càng tốt)
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
