using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public class RankRepository : Repository<MilitaryRank>, IRankRepository
    {
        public RankRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MilitaryRank>> SearchRanksAsync(string? keyword, string? group)
        {
            var query = _context.MilitaryRanks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(r => r.RankCode.ToLower().Contains(kw) ||
                                         r.RankName.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(group) && group != "Tất cả")
            {
                query = query.Where(r => r.RankGroup == group);
            }

            return await query.OrderBy(r => r.DisplayOrder).ToListAsync();
        }

        public async Task<MilitaryRank?> GetByCodeAsync(string rankCode)
        {
            if (string.IsNullOrWhiteSpace(rankCode)) return null;
            return await _context.MilitaryRanks.FirstOrDefaultAsync(r => r.RankCode.ToLower() == rankCode.Trim().ToLower());
        }

        public async Task<bool> ExistsByCodeAsync(string rankCode)
        {
            if (string.IsNullOrWhiteSpace(rankCode)) return false;
            return await _context.MilitaryRanks.AnyAsync(r => r.RankCode.ToLower() == rankCode.Trim().ToLower());
        }
    }

    public class PositionRepository : Repository<MilitaryPosition>, IPositionRepository
    {
        public PositionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MilitaryPosition>> SearchPositionsAsync(string? keyword, string? group)
        {
            var query = _context.MilitaryPositions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(p => p.PositionCode.ToLower().Contains(kw) ||
                                         p.PositionName.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(group) && group != "Tất cả")
            {
                query = query.Where(p => p.PositionGroup == group);
            }

            return await query.OrderBy(p => p.DisplayOrder).ToListAsync();
        }

        public async Task<MilitaryPosition?> GetByCodeAsync(string positionCode)
        {
            if (string.IsNullOrWhiteSpace(positionCode)) return null;
            return await _context.MilitaryPositions.FirstOrDefaultAsync(p => p.PositionCode.ToLower() == positionCode.Trim().ToLower());
        }

        public async Task<bool> ExistsByCodeAsync(string positionCode)
        {
            if (string.IsNullOrWhiteSpace(positionCode)) return false;
            return await _context.MilitaryPositions.AnyAsync(p => p.PositionCode.ToLower() == positionCode.Trim().ToLower());
        }
    }

    public class UnitRepository : Repository<MilitaryUnit>, IUnitRepository
    {
        public UnitRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MilitaryUnit>> SearchUnitsAsync(string? keyword, string? parentUnit)
        {
            var query = _context.MilitaryUnits.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(u => u.UnitCode.ToLower().Contains(kw) ||
                                         u.UnitName.ToLower().Contains(kw) ||
                                         u.CommanderName.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(parentUnit) && parentUnit != "Tất cả")
            {
                query = query.Where(u => u.ParentUnit == parentUnit);
            }

            return await query.OrderBy(u => u.UnitCode).ToListAsync();
        }

        public async Task<MilitaryUnit?> GetByCodeAsync(string unitCode)
        {
            if (string.IsNullOrWhiteSpace(unitCode)) return null;
            return await _context.MilitaryUnits.FirstOrDefaultAsync(u => u.UnitCode.ToLower() == unitCode.Trim().ToLower());
        }

        public async Task<bool> ExistsByCodeAsync(string unitCode)
        {
            if (string.IsNullOrWhiteSpace(unitCode)) return false;
            return await _context.MilitaryUnits.AnyAsync(u => u.UnitCode.ToLower() == unitCode.Trim().ToLower());
        }
    }

    public class MajorRepository : Repository<MilitaryMajor>, IMajorRepository
    {
        public MajorRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MilitaryMajor>> SearchMajorsAsync(string? keyword, string? department)
        {
            var query = _context.MilitaryMajors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(m => m.MajorCode.ToLower().Contains(kw) ||
                                         m.MajorName.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(department) && department != "Tất cả")
            {
                query = query.Where(m => m.Department == department);
            }

            return await query.OrderBy(m => m.MajorCode).ToListAsync();
        }

        public async Task<MilitaryMajor?> GetByCodeAsync(string majorCode)
        {
            if (string.IsNullOrWhiteSpace(majorCode)) return null;
            return await _context.MilitaryMajors.FirstOrDefaultAsync(m => m.MajorCode.ToLower() == majorCode.Trim().ToLower());
        }

        public async Task<bool> ExistsByCodeAsync(string majorCode)
        {
            if (string.IsNullOrWhiteSpace(majorCode)) return false;
            return await _context.MilitaryMajors.AnyAsync(m => m.MajorCode.ToLower() == majorCode.Trim().ToLower());
        }
    }
}
