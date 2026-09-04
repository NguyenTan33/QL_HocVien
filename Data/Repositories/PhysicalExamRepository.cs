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
            return await SearchWithCriteriaAsync(new QL_HocVien.Models.Filters.PhysicalExamFilterCriteria
            {
                CadetKeyword = cadetKeyword,
                SubjectId = subjectId,
                Grade = grade ?? "Tất cả",
                ExamSession = session ?? "Tất cả"
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

            // 1. Từ khóa học viên (Tên, Mã HV)
            if (!string.IsNullOrWhiteSpace(criteria.CadetKeyword))
            {
                var kw = criteria.CadetKeyword.Trim().ToLower();
                query = query.Where(r => r.Cadet != null &&
                    (r.Cadet.FullName.ToLower().Contains(kw) ||
                     r.Cadet.CadetCode.ToLower().Contains(kw)));
            }

            // 2. Môn kiểm tra
            if (criteria.SubjectId.HasValue && criteria.SubjectId.Value > 0)
            {
                query = query.Where(r => r.SubjectId == criteria.SubjectId.Value);
            }

            // 3. Xếp loại
            if (!string.IsNullOrWhiteSpace(criteria.Grade) && criteria.Grade != "Tất cả")
            {
                query = query.Where(r => r.Grade == criteria.Grade);
            }

            // 4. Kỳ / Đợt kiểm tra
            if (!string.IsNullOrWhiteSpace(criteria.ExamSession) && criteria.ExamSession != "Tất cả")
            {
                query = query.Where(r => r.ExamSession == criteria.ExamSession);
            }

            // 5. Đơn vị học viên
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
            {
                query = query.Where(r => r.Cadet != null && r.Cadet.Unit == criteria.Unit);
            }

            // 6. Lớp học của học viên
            if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Tất cả")
            {
                query = query.Where(r => r.Cadet != null && r.Cadet.ClassName == criteria.ClassName);
            }

            // 7. Khoảng thời gian: Từ ngày
            if (criteria.FromDate.HasValue)
            {
                var from = criteria.FromDate.Value.Date;
                query = query.Where(r => r.ExamDate >= from);
            }

            // 8. Khoảng thời gian: Đến ngày
            if (criteria.ToDate.HasValue)
            {
                var to = criteria.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.ExamDate <= to);
            }

            return await query.OrderByDescending(r => r.ExamDate).ToListAsync();
        }
    }
}
