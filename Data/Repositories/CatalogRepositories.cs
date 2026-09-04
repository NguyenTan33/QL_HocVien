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
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.CatalogFilterCriteria
            {
                Keyword = keyword,
                Group = group ?? "Tất cả"
            });
        }

        public async Task<IEnumerable<MilitaryRank>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria)
        {
            var query = _context.MilitaryRanks.AsQueryable();

            if (criteria != null)
            {
                if (!string.IsNullOrWhiteSpace(criteria.Keyword))
                {
                    var kw = criteria.Keyword.Trim().ToLower();
                    query = query.Where(r => r.RankCode.ToLower().Contains(kw) ||
                                             r.RankName.ToLower().Contains(kw) ||
                                             r.Description.ToLower().Contains(kw));
                }

                if (!string.IsNullOrWhiteSpace(criteria.Group) && criteria.Group != "Tất cả")
                {
                    query = query.Where(r => r.RankGroup == criteria.Group);
                }
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
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.CatalogFilterCriteria
            {
                Keyword = keyword,
                Group = group ?? "Tất cả"
            });
        }

        public async Task<IEnumerable<MilitaryPosition>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria)
        {
            var query = _context.MilitaryPositions.AsQueryable();

            if (criteria != null)
            {
                if (!string.IsNullOrWhiteSpace(criteria.Keyword))
                {
                    var kw = criteria.Keyword.Trim().ToLower();
                    query = query.Where(p => p.PositionCode.ToLower().Contains(kw) ||
                                             p.PositionName.ToLower().Contains(kw) ||
                                             p.Description.ToLower().Contains(kw));
                }

                if (!string.IsNullOrWhiteSpace(criteria.Group) && criteria.Group != "Tất cả")
                {
                    query = query.Where(p => p.PositionGroup == criteria.Group);
                }
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
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.CatalogFilterCriteria
            {
                Keyword = keyword,
                ParentUnit = parentUnit ?? "Tất cả"
            });
        }

        public async Task<IEnumerable<MilitaryUnit>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria)
        {
            var query = _context.MilitaryUnits.AsQueryable();

            if (criteria != null)
            {
                if (!string.IsNullOrWhiteSpace(criteria.Keyword))
                {
                    var kw = criteria.Keyword.Trim().ToLower();
                    query = query.Where(u => u.UnitCode.ToLower().Contains(kw) ||
                                             u.UnitName.ToLower().Contains(kw) ||
                                             u.CommanderName.ToLower().Contains(kw) ||
                                             u.Description.ToLower().Contains(kw));
                }

                if (!string.IsNullOrWhiteSpace(criteria.ParentUnit) && criteria.ParentUnit != "Tất cả")
                {
                    query = query.Where(u => u.ParentUnit == criteria.ParentUnit);
                }
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
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.CatalogFilterCriteria
            {
                Keyword = keyword,
                Department = department ?? "Tất cả"
            });
        }

        public async Task<IEnumerable<MilitaryMajor>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria)
        {
            var query = _context.MilitaryMajors.AsQueryable();

            if (criteria != null)
            {
                if (!string.IsNullOrWhiteSpace(criteria.Keyword))
                {
                    var kw = criteria.Keyword.Trim().ToLower();
                    query = query.Where(m => m.MajorCode.ToLower().Contains(kw) ||
                                             m.MajorName.ToLower().Contains(kw) ||
                                             m.Description.ToLower().Contains(kw));
                }

                if (!string.IsNullOrWhiteSpace(criteria.Department) && criteria.Department != "Tất cả")
                {
                    query = query.Where(m => m.Department == criteria.Department);
                }
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
