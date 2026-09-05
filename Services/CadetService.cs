using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public class CadetService : ICadetService
    {
        private readonly ICadetRepository _cadetRepository;

        public CadetService(ICadetRepository cadetRepository)
        {
            _cadetRepository = cadetRepository;
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

        public async Task<string> GenerateSuggestedCadetCodeAsync()
        {
            int year = DateTime.Today.Year;
            int nextSeq = await _cadetRepository.GetNextCadetSequenceNumberAsync(year);
            return $"HV-{year}-{nextSeq:D3}";
        }
    }
}
