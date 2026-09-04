using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public class ClassRepository : Repository<MilitaryClass>, IClassRepository
    {
        public ClassRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MilitaryClass>> SearchClassesAsync(string? keyword, string? unit, string? major)
        {
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.ClassFilterCriteria
            {
                Keyword = keyword,
                Unit = unit ?? "Tất cả",
                Major = major ?? "Tất cả"
            });
        }

        public async Task<IEnumerable<MilitaryClass>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.ClassFilterCriteria criteria)
        {
            var query = _context.MilitaryClasses
                .Include(c => c.Cadets)
                .AsQueryable();

            if (criteria == null)
            {
                return await query.OrderBy(c => c.ClassCode).ToListAsync();
            }

            // 1. Từ khóa: Mã lớp, Tên lớp, Cán bộ chủ nhiệm, Chuyên ngành
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var kw = criteria.Keyword.Trim().ToLower();
                query = query.Where(c => c.ClassCode.ToLower().Contains(kw) ||
                                         c.ClassName.ToLower().Contains(kw) ||
                                         c.OfficerInCharge.ToLower().Contains(kw) ||
                                         c.Major.ToLower().Contains(kw));
            }

            // 2. Đơn vị
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
            {
                query = query.Where(c => c.Unit == criteria.Unit);
            }

            // 3. Chuyên ngành
            if (!string.IsNullOrWhiteSpace(criteria.Major) && criteria.Major != "Tất cả")
            {
                query = query.Where(c => c.Major == criteria.Major);
            }

            // 4. Khóa học / Niên khóa
            if (!string.IsNullOrWhiteSpace(criteria.AcademicYear) && criteria.AcademicYear != "Tất cả")
            {
                query = query.Where(c => c.AcademicYear == criteria.AcademicYear);
            }

            // 5. Trạng thái phân công cán bộ chủ nhiệm
            if (criteria.HasOfficerAssigned.HasValue)
            {
                if (criteria.HasOfficerAssigned.Value)
                {
                    query = query.Where(c => !string.IsNullOrWhiteSpace(c.OfficerInCharge) || c.OfficerId != null);
                }
                else
                {
                    query = query.Where(c => string.IsNullOrWhiteSpace(c.OfficerInCharge) && c.OfficerId == null);
                }
            }

            // 6. Sĩ số học viên tối thiểu
            if (criteria.MinCadets.HasValue)
            {
                query = query.Where(c => c.Cadets.Count >= criteria.MinCadets.Value);
            }

            // 7. Sĩ số học viên tối đa
            if (criteria.MaxCadets.HasValue)
            {
                query = query.Where(c => c.Cadets.Count <= criteria.MaxCadets.Value);
            }

            return await query.OrderBy(c => c.ClassCode).ToListAsync();
        }

        public async Task<MilitaryClass?> GetByCodeAsync(string classCode)
        {
            if (string.IsNullOrWhiteSpace(classCode))
                return null;

            return await _context.MilitaryClasses
                .Include(c => c.Cadets)
                .FirstOrDefaultAsync(c => c.ClassCode.ToLower() == classCode.Trim().ToLower());
        }

        public async Task<MilitaryClass?> GetClassWithCadetsAsync(int id)
        {
            return await _context.MilitaryClasses
                .Include(c => c.Cadets)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string classCode)
        {
            if (string.IsNullOrWhiteSpace(classCode))
                return false;

            return await _context.MilitaryClasses.AnyAsync(c => c.ClassCode.ToLower() == classCode.Trim().ToLower());
        }

        public async Task<IEnumerable<MilitaryClass>> GetAllWithCadetsAsync()
        {
            return await _context.MilitaryClasses
                .Include(c => c.Cadets)
                .OrderBy(c => c.ClassCode)
                .ToListAsync();
        }
    }
}
