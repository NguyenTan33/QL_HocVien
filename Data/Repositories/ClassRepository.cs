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
            var query = _context.MilitaryClasses
                .Include(c => c.Cadets)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(c => c.ClassCode.ToLower().Contains(kw) ||
                                         c.ClassName.ToLower().Contains(kw) ||
                                         c.OfficerInCharge.ToLower().Contains(kw) ||
                                         c.Major.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
            {
                query = query.Where(c => c.Unit == unit);
            }

            if (!string.IsNullOrWhiteSpace(major) && major != "Tất cả")
            {
                query = query.Where(c => c.Major == major);
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
