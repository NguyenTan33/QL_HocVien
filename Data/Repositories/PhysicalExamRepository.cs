using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
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
                .Where(r => r.Grade == "Không đạt")
                .OrderByDescending(r => r.ExamDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhysicalExamRecord>> SearchRecordsAsync(string? cadetKeyword, int? subjectId, string? grade, string? session)
        {
            var query = _context.PhysicalExamRecords
                .Include(r => r.Cadet)
                .Include(r => r.Subject)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(cadetKeyword))
            {
                var kw = cadetKeyword.Trim().ToLower();
                query = query.Where(r => r.Cadet != null &&
                    (r.Cadet.FullName.ToLower().Contains(kw) ||
                     r.Cadet.CadetCode.ToLower().Contains(kw)));
            }

            if (subjectId.HasValue && subjectId.Value > 0)
            {
                query = query.Where(r => r.SubjectId == subjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(grade) && grade != "Tất cả")
            {
                query = query.Where(r => r.Grade == grade);
            }

            if (!string.IsNullOrWhiteSpace(session) && session != "Tất cả")
            {
                query = query.Where(r => r.ExamSession == session);
            }

            return await query.OrderByDescending(r => r.ExamDate).ToListAsync();
        }
    }
}
