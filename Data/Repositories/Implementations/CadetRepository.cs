using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories.Implementations
{
    public class CadetRepository : Repository<Cadet>, ICadetRepository
    {
        public CadetRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Cadet>> SearchCadetsAsync(string? keyword, string? rank, string? unit, string? className)
        {
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.CadetFilterCriteria
            {
                Keyword = keyword,
                Rank = rank ?? "Táº¥t cáº£",
                Unit = unit ?? "Táº¥t cáº£",
                ClassName = className ?? "Táº¥t cáº£"
            });
        }

        public async Task<IEnumerable<Cadet>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.CadetFilterCriteria criteria)
        {
            var query = _context.Cadets
                .Include(c => c.ExamRecords)
                .AsQueryable();

            if (criteria == null)
            {
                return await query.OrderByDescending(c => c.Id).ToListAsync();
            }

            // 1. Tá»« khÃ³a: TÃªn, MÃ£ HV, SÄT, Email
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var kw = criteria.Keyword.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(kw) ||
                                         c.CadetCode.ToLower().Contains(kw) ||
                                         c.PhoneNumber.Contains(kw) ||
                                         c.Email.ToLower().Contains(kw));
            }

            // 2. Cáº¥p báº­c
            if (!string.IsNullOrWhiteSpace(criteria.Rank) && criteria.Rank != "Táº¥t cáº£")
            {
                query = query.Where(c => c.Rank == criteria.Rank);
            }

            // 3. ÄÆ¡n vá»‹
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Táº¥t cáº£")
            {
                query = query.Where(c => c.Unit == criteria.Unit);
            }

            // 4. Lá»›p há»c
            if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Táº¥t cáº£")
            {
                query = query.Where(c => c.ClassName == criteria.ClassName);
            }

            // 5. Chá»©c vá»¥
            if (!string.IsNullOrWhiteSpace(criteria.Position) && criteria.Position != "Táº¥t cáº£")
            {
                query = query.Where(c => c.Position == criteria.Position);
            }

            // 6. Giá»›i tÃ­nh
            if (!string.IsNullOrWhiteSpace(criteria.Gender) && criteria.Gender != "Táº¥t cáº£")
            {
                query = query.Where(c => c.Gender == criteria.Gender);
            }

            // 7. Äá»™ tuá»•i tá»‘i thiá»ƒu
            if (criteria.MinAge.HasValue)
            {
                query = query.Where(c => c.Age >= criteria.MinAge.Value);
            }

            // 8. Äá»™ tuá»•i tá»‘i Ä‘a
            if (criteria.MaxAge.HasValue)
            {
                query = query.Where(c => c.Age <= criteria.MaxAge.Value);
            }

            // 9. Tráº¡ng thÃ¡i tÃ i khoáº£n ngÆ°á»i dÃ¹ng
            if (criteria.HasAccount.HasValue)
            {
                if (criteria.HasAccount.Value)
                {
                    query = query.Where(c => c.UserId != null);
                }
                else
                {
                    query = query.Where(c => c.UserId == null);
                }
            }

            // 10. Xáº¿p loáº¡i rÃ¨n luyá»‡n thá»ƒ lá»±c
            if (!string.IsNullOrWhiteSpace(criteria.FitnessGrade) && criteria.FitnessGrade != "Táº¥t cáº£")
            {
                if (criteria.FitnessGrade == "ChÆ°a kiá»ƒm tra")
                {
                    query = query.Where(c => !c.ExamRecords.Any());
                }
                else if (criteria.FitnessGrade == "Äáº¡t chuáº©n")
                {
                    query = query.Where(c => c.ExamRecords.Any(r => r.Grade == "Xuáº¥t sáº¯c" || r.Grade == "Giá»i" || r.Grade == "KhÃ¡" || r.Grade == "Äáº¡t"));
                }
                else if (criteria.FitnessGrade == "KhÃ´ng Ä‘áº¡t")
                {
                    query = query.Where(c => c.ExamRecords.Any(r => r.Grade == "KhÃ´ng Ä‘áº¡t"));
                }
                else
                {
                    query = query.Where(c => c.ExamRecords.Any(r => r.Grade == criteria.FitnessGrade));
                }
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

        public async Task<bool> ExistsByCodeAsync(string cadetCode, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(cadetCode)) return false;
            var query = _context.Cadets.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }
            return await query.AnyAsync(c => c.CadetCode.ToLower() == cadetCode.Trim().ToLower());
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

        public async Task<List<string>> GetDistinctUnitsAsync()
        {
            return await _context.Cadets
                .Where(c => !string.IsNullOrWhiteSpace(c.Unit))
                .Select(c => c.Unit.Trim())
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctClassesAsync()
        {
            return await _context.Cadets
                .Where(c => !string.IsNullOrWhiteSpace(c.ClassName))
                .Select(c => c.ClassName.Trim())
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctRanksAsync()
        {
            return await _context.Cadets
                .Where(c => !string.IsNullOrWhiteSpace(c.Rank))
                .Select(c => c.Rank.Trim())
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctPositionsAsync()
        {
            return await _context.Cadets
                .Where(c => !string.IsNullOrWhiteSpace(c.Position))
                .Select(c => c.Position.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }

        public async Task<int> DeleteMultipleAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (!idList.Any()) return 0;

            var cadetsToDelete = await _context.Cadets
                .Where(c => idList.Contains(c.Id))
                .ToListAsync();

            if (!cadetsToDelete.Any()) return 0;

            _context.Cadets.RemoveRange(cadetsToDelete);
            return await _context.SaveChangesAsync();
        }
    }
}

