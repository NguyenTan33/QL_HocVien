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
                Rank = rank ?? "Tất cả",
                Unit = unit ?? "Tất cả",
                ClassName = className ?? "Tất cả"
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

            // 1. Từ khóa: Tên, Mã HV, SĐT, Email
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var kw = criteria.Keyword.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(kw) ||
                                         c.CadetCode.ToLower().Contains(kw) ||
                                         c.PhoneNumber.Contains(kw) ||
                                         c.Email.ToLower().Contains(kw));
            }

            // 2. Cấp bậc
            if (!string.IsNullOrWhiteSpace(criteria.Rank) && criteria.Rank != "Tất cả")
            {
                query = query.Where(c => c.Rank == criteria.Rank);
            }

            // 3. Đơn vị
            if (!string.IsNullOrWhiteSpace(criteria.Unit) && criteria.Unit != "Tất cả")
            {
                var targetUnit = criteria.Unit.Trim().ToLower();
                var altUnit = GetEquivalentUnit(targetUnit).ToLower();
                if (!string.IsNullOrEmpty(altUnit))
                {
                    query = query.Where(c => c.Unit.ToLower() == targetUnit || 
                                             c.Unit.ToLower() == altUnit || 
                                             c.Unit.ToLower().Contains(targetUnit) || 
                                             c.Unit.ToLower().Contains(altUnit));
                }
                else
                {
                    query = query.Where(c => c.Unit.ToLower() == targetUnit || 
                                             c.Unit.ToLower().Contains(targetUnit));
                }
            }

            // 4. Lớp học
            if (!string.IsNullOrWhiteSpace(criteria.ClassName) && criteria.ClassName != "Tất cả")
            {
                query = query.Where(c => c.ClassName == criteria.ClassName);
            }

            // 5. Chức vụ
            if (!string.IsNullOrWhiteSpace(criteria.Position) && criteria.Position != "Tất cả")
            {
                query = query.Where(c => c.Position == criteria.Position);
            }

            // 6. Giới tính
            if (!string.IsNullOrWhiteSpace(criteria.Gender) && criteria.Gender != "Tất cả")
            {
                query = query.Where(c => c.Gender == criteria.Gender);
            }

            // 7. Độ tuổi tối thiểu
            if (criteria.MinAge.HasValue)
            {
                query = query.Where(c => c.Age >= criteria.MinAge.Value);
            }

            // 8. Độ tuổi tối đa
            if (criteria.MaxAge.HasValue)
            {
                query = query.Where(c => c.Age <= criteria.MaxAge.Value);
            }

            // 9. Trạng thái tài khoản người dùng
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

            // 10. Xếp loại rèn luyện thể lực
            if (!string.IsNullOrWhiteSpace(criteria.FitnessGrade) && criteria.FitnessGrade != "Tất cả")
            {
                if (criteria.FitnessGrade == "Chưa kiểm tra")
                {
                    query = query.Where(c => !c.ExamRecords.Any());
                }
                else if (criteria.FitnessGrade == "Đạt chuẩn")
                {
                    query = query.Where(c => c.ExamRecords.Any(r => r.Grade == "Xuất sắc" || r.Grade == "Giỏi" || r.Grade == "Khá" || r.Grade == "Đạt"));
                }
                else if (criteria.FitnessGrade == "Không đạt")
                {
                    query = query.Where(c => c.ExamRecords.Any(r => r.Grade == "Không đạt"));
                }
                else
                {
                    query = query.Where(c => c.ExamRecords.Any(r => r.Grade == criteria.FitnessGrade));
                }
            }

            // 11. Khóa học (Cohort)
            if (!string.IsNullOrWhiteSpace(criteria.Cohort) && criteria.Cohort != "Tất cả")
            {
                query = query.Where(c => c.Cohort == criteria.Cohort);
            }

            // 12. Niên khóa (AcademicYear)
            if (!string.IsNullOrWhiteSpace(criteria.AcademicYear) && criteria.AcademicYear != "Tất cả")
            {
                query = query.Where(c => c.AcademicYear == criteria.AcademicYear);
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

        public async Task<List<string>> GetDistinctCohortsAsync()
        {
            return await _context.Cadets
                .Where(c => !string.IsNullOrWhiteSpace(c.Cohort))
                .Select(c => c.Cohort.Trim())
                .Distinct()
                .OrderBy(ch => ch)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctAcademicYearsAsync()
        {
            return await _context.Cadets
                .Where(c => !string.IsNullOrWhiteSpace(c.AcademicYear))
                .Select(c => c.AcademicYear.Trim())
                .Distinct()
                .OrderBy(ay => ay)
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

        private static string GetEquivalentUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
            return unit.Trim().ToLowerInvariant() switch
            {
                "b1" => "Trung đội 1",
                "trung đội 1" => "b1",
                "b2" => "Trung đội 2",
                "trung đội 2" => "b2",
                "b3" => "Trung đội 3",
                "trung đội 3" => "b3",
                "b4" => "Trung đội 4",
                "trung đội 4" => "b4",
                "b5" => "Trung đội 5",
                "trung đội 5" => "b5",
                "b6" => "Trung đội 6",
                "trung đội 6" => "b6",
                "c1" => "Đại đội 1",
                "đại đội 1" => "c1",
                "c2" => "Đại đội 2",
                "đại đội 2" => "c2",
                "c3" => "Đại đội 3",
                "đại đội 3" => "c3",
                "c4" => "Đại đội 4",
                "đại đội 4" => "c4",
                "d1" => "Tiểu đoàn 1",
                "tiểu đoàn 1" => "d1",
                "d2" => "Tiểu đoàn 2",
                "tiểu đoàn 2" => "d2",
                "e1" => "Trung đoàn 1",
                "trung đoàn 1" => "e1",
                "a1" => "Tiểu đội 1",
                "tiểu đội 1" => "a1",
                "a2" => "Tiểu đội 2",
                "tiểu đội 2" => "a2",
                "a3" => "Tiểu đội 3",
                "tiểu đội 3" => "a3",
                "n1" => "Nhóm 1",
                "nhóm 1" => "n1",
                "n2" => "Nhóm 2",
                "nhóm 2" => "n2",
                _ => string.Empty
            };
        }
    }
}
