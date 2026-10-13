using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Data.Repositories.Interfaces;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Models.Entity;
using QL_HocVien.Services.Interfaces;

namespace QL_HocVien.Services.Implementations
{
    public class CadetService : ICadetService
    {
        private readonly ICadetRepository _cadetRepository;
        private readonly ICadetUnitHistoryRepository _unitHistoryRepository;
        private readonly AppDbContext _context;

        public CadetService(
            ICadetRepository cadetRepository,
            ICadetUnitHistoryRepository unitHistoryRepository,
            AppDbContext context)
        {
            _cadetRepository = cadetRepository;
            _unitHistoryRepository = unitHistoryRepository;
            _context = context;
        }

        public async Task<IEnumerable<Cadet>> GetAllCadetsAsync()
        {
            return await _cadetRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Cadet>> SearchCadetsAsync(string? keyword, string? rank, string? unit, string? className)
        {
            return await _cadetRepository.SearchCadetsAsync(keyword, rank, unit, className);
        }

        public async Task<IEnumerable<Cadet>> SearchCadetsAsync(QL_HocVien.Models.Filters.CadetFilterCriteria criteria)
        {
            return await _cadetRepository.SearchWithCriteriaAsync(criteria);
        }

        public async Task<Cadet?> GetCadetByIdAsync(int id)
        {
            return await _cadetRepository.GetByIdAsync(id);
        }

        public async Task<Cadet?> GetCadetWithRecordsAsync(int id)
        {
            return await _cadetRepository.GetCadetWithRecordsAsync(id);
        }

        public async Task<(bool Success, string Message, Cadet? Cadet)> AddCadetAsync(Cadet cadet)
        {
            if (string.IsNullOrWhiteSpace(cadet.CadetCode))
                return (false, "Mã học viên không được để trống.", null);

            if (string.IsNullOrWhiteSpace(cadet.FullName))
                return (false, "Họ và tên học viên không được để trống.", null);

            if (await _cadetRepository.ExistsByCodeAsync(cadet.CadetCode))
                return (false, $"Mã học viên '{cadet.CadetCode}' đã tồn tại.", null);

            if (cadet.DateOfBirth.HasValue && !cadet.Age.HasValue)
            {
                cadet.Age = DateTime.Today.Year - cadet.DateOfBirth.Value.Year;
            }

            await _cadetRepository.AddAsync(cadet);
            await _cadetRepository.SaveChangesAsync();

            return (true, "Thêm học viên thành công!", cadet);
        }

        public async Task<(bool Success, string Message)> UpdateCadetAsync(Cadet cadet)
        {
            if (string.IsNullOrWhiteSpace(cadet.FullName))
                return (false, "Họ và tên học viên không được để trống.");

            if (string.IsNullOrWhiteSpace(cadet.CadetCode))
                return (false, "Mã học viên (ID) không được để trống.");

            if (await _cadetRepository.ExistsByCodeAsync(cadet.CadetCode, cadet.Id))
            {
                return (false, $"Mã học viên '{cadet.CadetCode}' đã được sử dụng.");
            }

            var existing = await _cadetRepository.GetByIdAsync(cadet.Id);
            if (existing == null)
                return (false, "Không tìm thấy học viên để cập nhật.");

            existing.CadetCode = cadet.CadetCode.Trim();
            existing.FullName = cadet.FullName;
            existing.Rank = cadet.Rank;
            existing.Position = cadet.Position;
            existing.Unit = cadet.Unit;
            existing.ClassName = cadet.ClassName;
            existing.PhoneNumber = cadet.PhoneNumber;
            existing.Email = cadet.Email;
            existing.DateOfBirth = cadet.DateOfBirth;
            existing.Age = cadet.Age;
            existing.Gender = cadet.Gender;

            try
            {
                _cadetRepository.Update(existing);
                await _cadetRepository.SaveChangesAsync();
                return (true, "Cập nhật thông tin học viên thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật học viên: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteCadetAsync(int id)
        {
            var cadet = await _cadetRepository.GetByIdAsync(id);
            if (cadet == null)
                return (false, "Không tìm thấy học viên cần xóa.");

            _cadetRepository.Delete(cadet);
            await _cadetRepository.SaveChangesAsync();

            return (true, "Xóa học viên thành công!");
        }

        public async Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleCadetsAsync(IEnumerable<int> cadetIds)
        {
            var ids = cadetIds?.Distinct().ToList() ?? new List<int>();
            if (!ids.Any())
                return (false, "Không có học viên nào được chọn để xóa.", 0);

            try
            {
                int count = await _cadetRepository.DeleteMultipleAsync(ids);
                return (true, $"Đã xóa thành công {count} học viên.", count);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xóa học viên: {ex.Message}", 0);
            }
        }

        public async Task<List<string>> GetDistinctUnitsAsync() => await _cadetRepository.GetDistinctUnitsAsync();
        public async Task<List<string>> GetDistinctClassesAsync() => await _cadetRepository.GetDistinctClassesAsync();
        public async Task<List<string>> GetDistinctRanksAsync() => await _cadetRepository.GetDistinctRanksAsync();
        public async Task<List<string>> GetDistinctPositionsAsync() => await _cadetRepository.GetDistinctPositionsAsync();
        public async Task<List<string>> GetDistinctCohortsAsync() => await _cadetRepository.GetDistinctCohortsAsync();
        public async Task<List<string>> GetDistinctAcademicYearsAsync() => await _cadetRepository.GetDistinctAcademicYearsAsync();

        public async Task<string> GenerateSuggestedCadetCodeAsync()
        {
            int year = DateTime.Today.Year;
            int nextSeq = await _cadetRepository.GetNextCadetSequenceNumberAsync(year);
            return $"HV-{year}-{nextSeq:D3}";
        }

        public async Task EnsureInitialUnitHistoryAsync(int cadetId)
        {
            var cadet = await _cadetRepository.GetByIdAsync(cadetId);
            if (cadet == null) return;

            var existing = await _unitHistoryRepository.GetByCadetIdAsync(cadetId);
            if (existing.Count == 0)
            {
                string schoolYear = !string.IsNullOrWhiteSpace(cadet.AcademicYear)
                    ? cadet.AcademicYear
                    : $"{DateTime.Today.Year} - {DateTime.Today.Year + 1}";

                var initialHistory = new CadetUnitHistory
                {
                    CadetId = cadet.Id,
                    SchoolYear = schoolYear,
                    YearOrder = 1,
                    Cohort = cadet.Cohort ?? string.Empty,
                    Unit = cadet.Unit ?? string.Empty,
                    UnitName = cadet.Unit ?? string.Empty,
                    ClassName = cadet.ClassName ?? string.Empty,
                    Status = string.IsNullOrWhiteSpace(cadet.Status) ? "Đang học" : cadet.Status,
                    TransferReason = "Tiếp nhận học viên ban đầu",
                    EffectiveDate = DateTime.Today
                };

                await _unitHistoryRepository.AddAsync(initialHistory);
                await _unitHistoryRepository.SaveChangesAsync();
            }
        }

        public async Task<(bool Success, string Message)> TransferCadetAsync(
            int cadetId,
            string newUnit,
            string newSchoolYear,
            int yearOrder,
            string? newCohort = null,
            string? newClassName = null,
            string? reason = null,
            DateTime? effectiveDate = null,
            string status = "Đang học")
        {
            if (cadetId <= 0) return (false, "Học viên không hợp lệ.");
            if (string.IsNullOrWhiteSpace(newUnit)) return (false, "Vui lòng chọn đơn vị mới cần điều chuyển.");
            if (string.IsNullOrWhiteSpace(newSchoolYear)) return (false, "Vui lòng nhập hoặc chọn năm học áp dụng.");

            var cadet = await _cadetRepository.GetByIdAsync(cadetId);
            if (cadet == null) return (false, "Không tìm thấy học viên trong hệ thống.");

            // Đảm bảo có lịch sử mốc ban đầu trước khi thêm mốc điều chuyển mới
            await EnsureInitialUnitHistoryAsync(cadetId);

            var existingHistory = await _unitHistoryRepository.GetByCadetAndYearAsync(cadetId, newSchoolYear);
            if (existingHistory != null)
            {
                existingHistory.Unit = newUnit.Trim();
                existingHistory.UnitName = newUnit.Trim();
                existingHistory.YearOrder = yearOrder > 0 ? yearOrder : existingHistory.YearOrder;
                if (!string.IsNullOrWhiteSpace(newCohort)) existingHistory.Cohort = newCohort.Trim();
                if (!string.IsNullOrWhiteSpace(newClassName)) existingHistory.ClassName = newClassName.Trim();
                existingHistory.Status = status;
                existingHistory.TransferReason = reason ?? existingHistory.TransferReason;
                existingHistory.EffectiveDate = effectiveDate ?? DateTime.Today;
                _unitHistoryRepository.Update(existingHistory);
            }
            else
            {
                var newHistory = new CadetUnitHistory
                {
                    CadetId = cadetId,
                    SchoolYear = newSchoolYear.Trim(),
                    YearOrder = yearOrder > 0 ? yearOrder : 1,
                    Cohort = !string.IsNullOrWhiteSpace(newCohort) ? newCohort.Trim() : (cadet.Cohort ?? string.Empty),
                    Unit = newUnit.Trim(),
                    UnitName = newUnit.Trim(),
                    ClassName = !string.IsNullOrWhiteSpace(newClassName) ? newClassName.Trim() : (cadet.ClassName ?? string.Empty),
                    Status = status,
                    TransferReason = reason ?? "Điều chuyển đơn vị đào tạo",
                    EffectiveDate = effectiveDate ?? DateTime.Today
                };
                await _unitHistoryRepository.AddAsync(newHistory);
            }

            // Đồng thời cập nhật trạng thái và đơn vị hiện hành của học viên
            cadet.Unit = newUnit.Trim();
            if (!string.IsNullOrWhiteSpace(newCohort)) cadet.Cohort = newCohort.Trim();
            if (!string.IsNullOrWhiteSpace(newClassName)) cadet.ClassName = newClassName.Trim();
            cadet.Status = status;
            _cadetRepository.Update(cadet);

            await _context.SaveChangesAsync();
            return (true, "Điều chuyển học viên sang bộ phận/đơn vị mới thành công!");
        }

        public async Task<List<CadetTimelineNodeDto>> GetCadetTimelineAsync(int cadetId)
        {
            var cadet = await _cadetRepository.GetByIdAsync(cadetId);
            if (cadet == null) return new List<CadetTimelineNodeDto>();

            await EnsureInitialUnitHistoryAsync(cadetId);

            var histories = await _unitHistoryRepository.GetByCadetIdAsync(cadetId);
            if (histories.Count == 0) return new List<CadetTimelineNodeDto>();

            // Lấy tất cả bảng điểm tín chỉ của học viên này
            var allScores = await _context.CreditScoreRecords
                .Include(r => r.CreditSubject)
                .Where(r => r.CadetId == cadetId)
                .ToListAsync();

            // Sắp xếp theo Năm thứ (YearOrder) hoặc thứ tự năm học
            var sortedHistories = histories.OrderBy(h => h.YearOrder).ThenBy(h => h.SchoolYear).ToList();
            var result = new List<CadetTimelineNodeDto>();

            for (int i = 0; i < sortedHistories.Count; i++)
            {
                var h = sortedHistories[i];
                var yearScores = allScores
                    .Where(s => string.Equals(s.SchoolYear?.Trim(), h.SchoolYear?.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                double? gpa = null;
                if (yearScores.Count > 0)
                {
                    double totalWeighted = 0;
                    double totalCredits = 0;
                    foreach (var s in yearScores)
                    {
                        double cred = (s.CreditSubject != null && s.CreditSubject.Credits > 0) ? s.CreditSubject.Credits : 1.0;
                        totalWeighted += s.FinalScore * cred;
                        totalCredits += cred;
                    }

                    if (totalCredits > 0)
                    {
                        gpa = Math.Round(totalWeighted / totalCredits, 2);
                    }
                }
                else if (h.AnnualGpa.HasValue && h.AnnualGpa.Value > 0)
                {
                    gpa = h.AnnualGpa.Value;
                }

                bool isLast = (i == sortedHistories.Count - 1);
                string nodeStatus = !string.IsNullOrWhiteSpace(h.Status) ? h.Status : (cadet.Status ?? "Đang học");
                bool isGrad = string.Equals(nodeStatus, "Đã hoàn thành", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(nodeStatus, "Đã tốt nghiệp", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(cadet.Status, "Đã hoàn thành", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(cadet.Status, "Đã tốt nghiệp", StringComparison.OrdinalIgnoreCase);

                var node = new CadetTimelineNodeDto
                {
                    Id = h.Id,
                    CadetId = h.CadetId,
                    SchoolYear = h.SchoolYear,
                    YearOrder = h.YearOrder,
                    Cohort = !string.IsNullOrWhiteSpace(h.Cohort) ? h.Cohort : (cadet.Cohort ?? string.Empty),
                    Unit = h.Unit,
                    UnitName = !string.IsNullOrWhiteSpace(h.UnitName) ? h.UnitName : h.Unit,
                    ClassName = !string.IsNullOrWhiteSpace(h.ClassName) ? h.ClassName : (cadet.ClassName ?? string.Empty),
                    Status = nodeStatus,
                    AnnualGpa = gpa,
                    TransferReason = h.TransferReason ?? string.Empty,
                    EffectiveDate = h.EffectiveDate,
                    IsCurrent = isLast
                };

                result.Add(node);
            }

            return result;
        }

        public async Task<List<CadetUnitHistory>> GetUnitHistoriesBySchoolYearAsync(string schoolYear)
        {
            return await _unitHistoryRepository.GetBySchoolYearAsync(schoolYear);
        }

        public async Task<List<string>> GetDistinctSchoolYearsFromHistoryAsync()
        {
            return await _unitHistoryRepository.GetDistinctSchoolYearsAsync();
        }
    }
}
