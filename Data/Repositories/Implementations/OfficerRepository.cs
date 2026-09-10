using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories.Implementations
{
    public class OfficerRepository : Repository<Officer>, IOfficerRepository
    {
        public OfficerRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Officer>> SearchOfficersAsync(string? keyword, string? rank, string? unit, string? position)
        {
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.OfficerFilterCriteria
            {
                Keyword = keyword,
                Rank = rank ?? "Táº¥t cáº£",
                Unit = unit ?? "Táº¥t cáº£",
                Position = position ?? "Táº¥t cáº£"
            });
        }

        public async Task<IEnumerable<Officer>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.OfficerFilterCriteria criteria)
        {
            var query = _context.Officers
                .Include(o => o.ManagedClasses)
                .Include(o => o.User)
                .AsQueryable();

            if (criteria == null)
            {
                return await query.OrderByDescending(o => o.Id).ToListAsync();
            }

            // 1. Tá»« khÃ³a: MÃ£ CB, Há» tÃªn, SÄT, Email, ChuyÃªn mÃ´n
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var kw = criteria.Keyword.Trim().ToLower();
                query = query.Where(o => o.OfficerCode.ToLower().Contains(kw) ||
                                         o.FullName.ToLower().Contains(kw) ||
                                         o.PhoneNumber.Contains(kw) ||
                                         o.Email.ToLower().Contains(kw) ||
                                         o.Specialty.ToLower().Contains(kw));
            }

            // 2. Cáº¥p báº­c
            if (!string.IsNullOrWhiteSpace(criteria.Rank) && criteria.Rank != "Táº¥t cáº£")
            {
                query = query.Where(o => o.Rank == criteria.Rank);
            }

            // 3. ÄÆ¡n vá»‹
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Táº¥t cáº£")
            {
                query = query.Where(o => o.Unit == criteria.Unit);
            }

            // 4. Chá»©c vá»¥
            if (!string.IsNullOrWhiteSpace(criteria.Position) && criteria.Position != "Táº¥t cáº£")
            {
                query = query.Where(o => o.Position == criteria.Position);
            }

            // 5. ChuyÃªn mÃ´n lá»c riÃªng
            if (!string.IsNullOrWhiteSpace(criteria.Specialty))
            {
                var spec = criteria.Specialty.Trim().ToLower();
                query = query.Where(o => o.Specialty.ToLower().Contains(spec));
            }

            // 6. Tráº¡ng thÃ¡i tÃ i khoáº£n Ä‘Äƒng nháº­p
            if (criteria.HasAccount.HasValue)
            {
                if (criteria.HasAccount.Value)
                {
                    query = query.Where(o => o.UserId != null);
                }
                else
                {
                    query = query.Where(o => o.UserId == null);
                }
            }

            // 7. Äang chá»§ nhiá»‡m / phá»¥ trÃ¡ch lá»›p há»c
            if (criteria.HasAssignedClasses.HasValue)
            {
                if (criteria.HasAssignedClasses.Value)
                {
                    query = query.Where(o => o.ManagedClasses.Any());
                }
                else
                {
                    query = query.Where(o => !o.ManagedClasses.Any());
                }
            }

            return await query.OrderByDescending(o => o.Id).ToListAsync();
        }

        public async Task<Officer?> GetByCodeAsync(string officerCode)
        {
            if (string.IsNullOrWhiteSpace(officerCode))
                return null;

            return await _context.Officers
                .Include(o => o.ManagedClasses)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OfficerCode.ToLower() == officerCode.Trim().ToLower());
        }

        public async Task<Officer?> GetOfficerWithDetailsAsync(int id)
        {
            return await _context.Officers
                .Include(o => o.ManagedClasses)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string officerCode)
        {
            if (string.IsNullOrWhiteSpace(officerCode))
                return false;

            return await _context.Officers.AnyAsync(o => o.OfficerCode.ToLower() == officerCode.Trim().ToLower());
        }

        public async Task<int> GetNextOfficerSequenceNumberAsync()
        {
            var prefix = "CB-";
            var codes = await _context.Officers
                .Where(o => o.OfficerCode.StartsWith(prefix))
                .Select(o => o.OfficerCode)
                .ToListAsync();

            int max = 0;
            foreach (var code in codes)
            {
                var part = code.Substring(prefix.Length);
                if (int.TryParse(part, out int seq) && seq > max)
                {
                    max = seq;
                }
            }

            return max + 1;
        }
    }
}

