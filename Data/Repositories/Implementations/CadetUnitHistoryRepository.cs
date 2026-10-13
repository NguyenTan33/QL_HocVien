using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data.Repositories.Interfaces;
using QL_HocVien.Models.Entity;

namespace QL_HocVien.Data.Repositories.Implementations
{
    public class CadetUnitHistoryRepository : Repository<CadetUnitHistory>, ICadetUnitHistoryRepository
    {
        public CadetUnitHistoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<CadetUnitHistory>> GetByCadetIdAsync(int cadetId)
        {
            return await _context.CadetUnitHistories
                .Where(h => h.CadetId == cadetId)
                .OrderBy(h => h.YearOrder)
                .ThenBy(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CadetUnitHistory>> GetBySchoolYearAsync(string schoolYear)
        {
            return await _context.CadetUnitHistories
                .Include(h => h.Cadet)
                .Where(h => h.SchoolYear == schoolYear)
                .ToListAsync();
        }

        public async Task<CadetUnitHistory?> GetLatestByCadetIdAsync(int cadetId)
        {
            return await _context.CadetUnitHistories
                .Where(h => h.CadetId == cadetId)
                .OrderByDescending(h => h.YearOrder)
                .ThenByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<CadetUnitHistory?> GetByCadetAndYearAsync(int cadetId, string schoolYear)
        {
            return await _context.CadetUnitHistories
                .FirstOrDefaultAsync(h => h.CadetId == cadetId && h.SchoolYear == schoolYear);
        }

        public async Task<List<string>> GetDistinctSchoolYearsAsync()
        {
            return await _context.CadetUnitHistories
                .Select(h => h.SchoolYear)
                .Where(y => !string.IsNullOrWhiteSpace(y))
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync();
        }
    }
}
