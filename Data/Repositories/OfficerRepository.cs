using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public class OfficerRepository : Repository<Officer>, IOfficerRepository
    {
        public OfficerRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Officer>> SearchOfficersAsync(string? keyword, string? rank, string? unit, string? position)
        {
            var query = _context.Officers
                .Include(o => o.ManagedClasses)
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(o => o.OfficerCode.ToLower().Contains(kw) ||
                                         o.FullName.ToLower().Contains(kw) ||
                                         o.PhoneNumber.Contains(kw) ||
                                         o.Email.ToLower().Contains(kw) ||
                                         o.Specialty.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(rank) && rank != "Tất cả")
            {
                query = query.Where(o => o.Rank == rank);
            }

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
            {
                query = query.Where(o => o.Unit == unit);
            }

            if (!string.IsNullOrWhiteSpace(position) && position != "Tất cả")
            {
                query = query.Where(o => o.Position == position);
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
