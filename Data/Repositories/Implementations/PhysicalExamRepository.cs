using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories.Implementations
{
    public class PhysicalExamRepository : Repository<PhysicalExamRecord>, IPhysicalExamRepository
    {
        public PhysicalExamRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PhysicalExamRecord>> GetRecordsByCadetIdAsync(int cadetId)
        {
            return await _context.PhysicalExamRecords
                .Include(r => r.Subject)
                .Where(r => r.CadetId == cadetId)
                .OrderByDescending(r => r.ExamDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhysicalExamRecord>> GetAllWithDetailsAsync()
        {
            return await _context.PhysicalExamRecords
                .Include(r => r.Cadet)
                .Include(r => r.Subject)
                .OrderByDescending(r => r.ExamDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhysicalExamRecord>> GetFailedRecordsAsync()
        {
            return await _context.PhysicalExamRecords
                .Include(r => r.Cadet)
                .Include(r => r.Subject)
                .Where(r => r.Grade == "KhÃ´ng Ä‘áº¡t")
                .OrderByDescending(r => r.ExamDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhysicalExamRecord>> SearchRecordsAsync(string? cadetKeyword, int? subjectId, string? grade, string? session)
        {
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.PhysicalExamFilterCriteria
            {
                CadetKeyword = cadetKeyword,
                SubjectId = subjectId,
                Grade = grade ?? "Táº¥t cáº£",
                ExamSession = session ?? "Táº¥t cáº£"
            });
        }

        public async Task<IEnumerable<PhysicalExamRecord>> SearchWithCriteriaAsync(QL_HocVien.Models.Filters.PhysicalExamFilterCriteria criteria)
        {
            var query = _context.PhysicalExamRecords
                .Include(r => r.Cadet)
                .Include(r => r.Subject)
                .AsQueryable();

            if (criteria == null)
            {
                return await query.OrderByDescending(r => r.ExamDate).ToListAsync();
            }

            // 1. Tá»« khÃ³a há»c viÃªn (TÃªn, MÃ£ HV)
            if (!string.IsNullOrWhiteSpace(criteria.CadetKeyword))
            {
                var kw = criteria.CadetKeyword.Trim().ToLower();
                query = query.Where(r => r.Cadet != null &&
                    (r.Cadet.FullName.ToLower().Contains(kw) ||
                     r.Cadet.CadetCode.ToLower().Contains(kw)));
            }

            // 2. MÃ´n kiá»ƒm tra
            if (criteria.SubjectId.HasValue && criteria.SubjectId.Value > 0)
            {
                query = query.Where(r => r.SubjectId == criteria.SubjectId.Value);
            }

            // 3. Xáº¿p loáº¡i
            if (!string.IsNullOrWhiteSpace(criteria.Grade) && criteria.Grade != "Táº¥t cáº£")
            {
                query = query.Where(r => r.Grade == criteria.Grade);
            }

            // 4. Ká»³ / Äá»£t kiá»ƒm tra
            if (!string.IsNullOrWhiteSpace(criteria.ExamSession) && criteria.ExamSession != "Táº¥t cáº£")
            {
                query = query.Where(r => r.ExamSession == criteria.ExamSession);
            }

            // 5. ÄÆ¡n vá»‹ há»c viÃªn
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Táº¥t cáº£")
            {
                query = query.Where(r => r.Cadet != null && r.Cadet.Unit == criteria.Unit);
            }

            // 6. Lá»›p há»c cá»§a há»c viÃªn
            if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Táº¥t cáº£")
            {
                query = query.Where(r => r.Cadet != null && r.Cadet.ClassName == criteria.ClassName);
            }

            // 7. Khoáº£ng thá»i gian: Tá»« ngÃ y
            if (criteria.FromDate.HasValue)
            {
                var from = criteria.FromDate.Value.Date;
                query = query.Where(r => r.ExamDate >= from);
            }

            // 8. Khoáº£ng thá»i gian: Äáº¿n ngÃ y
            if (criteria.ToDate.HasValue)
            {
                var to = criteria.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.ExamDate <= to);
            }

            return await query.OrderByDescending(r => r.ExamDate).ToListAsync();
        }
    }
}

