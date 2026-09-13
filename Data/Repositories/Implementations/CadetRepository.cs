using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
                var cleanUnit = criteria.Unit.Trim().TrimEnd('/');

                if (cleanUnit.StartsWith("K", StringComparison.OrdinalIgnoreCase) && int.TryParse(cleanUnit.Substring(1), out int cNumFromUnit))
                {
                    string dotPat = $".{cNumFromUnit:D3}.";
                    query = query.Where(c =>
                        c.Cohort == cleanUnit ||
                        (c.AcademicCohort != null && (c.AcademicCohort.CohortCode == cleanUnit || c.AcademicCohort.CohortNumber == cNumFromUnit)) ||
                        c.CadetCode.Contains(dotPat));
                }
                else
                {
                    // 1. Kiểm tra nếu là tên tiếng Việt đơn thuần (ví dụ "Tiểu đoàn 1" hoặc "Đại đội 1")
                    var mD = Regex.Match(cleanUnit, @"(?i)^tiểu\s*đoàn\s*(\d+)$");
                    var mC = Regex.Match(cleanUnit, @"(?i)^đại\s*đội\s*(\d+)$");
                    var mB = Regex.Match(cleanUnit, @"(?i)^tiểu\s*đội\s*(\d+)$");

                    if (mD.Success)
                    {
                        string dCode = $"dBB{mD.Groups[1].Value}";
                        string dCodeShort = $"d{mD.Groups[1].Value}";
                        query = query.Where(c => c.Unit.StartsWith(dCode + "/") || c.Unit == dCode || c.Unit.StartsWith(dCodeShort + "/") || c.Unit == cleanUnit || c.Unit.Contains(cleanUnit));
                    }
                    else if (mC.Success)
                    {
                        string cCode = $"cBB{mC.Groups[1].Value}";
                        string cCodeShort = $"c{mC.Groups[1].Value}";
                        query = query.Where(c => c.Unit.Contains("/" + cCode + "/") || c.Unit.EndsWith("/" + cCode) || c.Unit == cCode ||
                                                 c.Unit.Contains("/" + cCodeShort + "/") || c.Unit.EndsWith("/" + cCodeShort) ||
                                                 c.Unit == cleanUnit || c.Unit.Contains(cleanUnit));
                    }
                    else if (mB.Success)
                    {
                        string bNum = mB.Groups[1].Value;
                        query = query.Where(c => c.Unit.EndsWith($"/bBB{bNum}") || c.Unit.EndsWith($"/dBB{bNum}") ||
                                                 c.Unit.EndsWith($"/b{bNum}") || c.Unit.EndsWith($"/{bNum}") ||
                                                 c.Unit == cleanUnit || c.Unit.Contains(cleanUnit));
                    }
                    else
                    {
                        // 2. Phân tích đường dẫn mã đơn vị phân cấp (ví dụ "dBB1", "dBB1/cBB1", "dBB1/cBB1/bBB1")
                        var segments = cleanUnit.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        if (segments.Length == 1)
                        {
                            // Cấp Tiểu đoàn: dBB1
                            string s0 = segments[0];
                            query = query.Where(c => c.Unit == s0 || c.Unit.StartsWith(s0 + "/") || c.Unit.Contains(s0));
                        }
                        else if (segments.Length == 2)
                        {
                            // Cấp Đại đội: dBB1/cBB1
                            string prefix = $"{segments[0]}/{segments[1]}";
                            query = query.Where(c => c.Unit == prefix || c.Unit.StartsWith(prefix + "/") || c.Unit.Contains(prefix));
                        }
                        else if (segments.Length >= 3)
                        {
                            // Cấp Tiểu đội: dBB1/cBB1/bBB1
                            string s0 = segments[0];
                            string s1 = segments[1];
                            string s2 = segments[2];
                            var mNum = Regex.Match(s2, @"\d+");
                            string num = mNum.Success ? mNum.Value : s2;

                            string patB = $"{s0}/{s1}/bBB{num}";
                            string patD = $"{s0}/{s1}/dBB{num}";
                            string patRaw = $"{s0}/{s1}/{num}";
                            string patExact = cleanUnit;

                            query = query.Where(c =>
                                c.Unit == patExact ||
                                c.Unit == patB ||
                                c.Unit == patD ||
                                c.Unit == patRaw ||
                                c.Unit.EndsWith("/" + s2) ||
                                c.Unit.EndsWith($"/bBB{num}") ||
                                c.Unit.EndsWith($"/dBB{num}") ||
                                c.Unit.EndsWith($"/b{num}"));
                        }
                    }
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
                var targetCohort = criteria.Cohort.Trim();
                int? cohortNum = null;
                if (targetCohort.StartsWith("K", StringComparison.OrdinalIgnoreCase) && int.TryParse(targetCohort.Substring(1), out int pNum))
                {
                    cohortNum = pNum;
                }
                else if (int.TryParse(targetCohort, out int rawNum))
                {
                    cohortNum = rawNum;
                }

                string cohortPatternDot = cohortNum.HasValue ? $".{cohortNum.Value:D3}." : string.Empty;
                string cohortPatternHyphen = cohortNum.HasValue ? $"-{cohortNum.Value:D3}-" : string.Empty;
                string cohortPatternUnderscore = cohortNum.HasValue ? $"_{cohortNum.Value:D3}_" : string.Empty;

                query = query.Where(c =>
                    c.Cohort == targetCohort ||
                    (!string.IsNullOrEmpty(cohortPatternDot) && (c.CadetCode.Contains(cohortPatternDot) || c.CadetCode.Contains(cohortPatternHyphen) || c.CadetCode.Contains(cohortPatternUnderscore)))
                );
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
