using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public class CadetRepository : Repository<Cadet>, ICadetRepository
    {
        public CadetRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Cadet>> SearchCadetsAsync(string? keyword, string? rank, string? unit, string? className)
        {
            var query = _context.Cadets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(kw) ||
                                         c.CadetCode.ToLower().Contains(kw) ||
                                         c.PhoneNumber.Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(rank) && rank != "Tất cả")
            {
                query = query.Where(c => c.Rank == rank);
            }

            if (!string.IsNullOrWhiteSpace(unit) && unit != "Tất cả")
            {
                query = query.Where(c => c.Unit == unit);
            }

            if (!string.IsNullOrWhiteSpace(className) && className != "Tất cả")
            {
                query = query.Where(c => c.ClassName == className);
            }

            return await query.OrderByDescending(c => c.Id).ToListAsync();
        }

        public async Task<Cadet?> GetByCodeAsync(string cadetCode)
        {
            if (string.IsNullOrWhiteSpace(cadetCode))
                return null;

            return await _context.Cadets.FirstOrDefaultAsync(c => c.CadetCode.ToLower() == cadetCode.Trim().ToLower());
        }

        public async Task<Cadet?> GetCadetWithRecordsAsync(int id)
        {
            return await _context.Cadets
                .Include(c => c.ExamRecords)
                    .ThenInclude(r => r.Subject)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string cadetCode)
        {
            return await _context.Cadets.AnyAsync(c => c.CadetCode.ToLower() == cadetCode.Trim().ToLower());
        }

        public async Task<int> GetNextCadetSequenceNumberAsync(int year)
        {
            var prefix = $"HV-{year}-";
            var existingCodes = await _context.Cadets
                .Where(c => c.CadetCode.StartsWith(prefix))
                .Select(c => c.CadetCode)
                .ToListAsync();

            int maxSeq = 0;
            foreach (var code in existingCodes)
            {
                var part = code.Substring(prefix.Length);
                if (int.TryParse(part, out int seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }

            return maxSeq + 1;
        }
    }
}
