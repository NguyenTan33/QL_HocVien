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

            var existing = await _cadetRepository.GetByIdAsync(cadet.Id);
            if (existing == null)
                return (false, "Không tìm thấy học viên để cập nhật.");

            // Kiểm tra mã nếu bị đổi
            if (existing.CadetCode != cadet.CadetCode)
            {
                if (await _cadetRepository.ExistsByCodeAsync(cadet.CadetCode))
                    return (false, $"Mã học viên '{cadet.CadetCode}' đã được sử dụng.");
            }

            existing.CadetCode = cadet.CadetCode;
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

            _cadetRepository.Update(existing);
            await _cadetRepository.SaveChangesAsync();

            return (true, "Cập nhật thông tin học viên thành công!");
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

        public async Task<string> GenerateSuggestedCadetCodeAsync()
        {
            int year = DateTime.Today.Year;
            int nextSeq = await _cadetRepository.GetNextCadetSequenceNumberAsync(year);
            return $"HV-{year}-{nextSeq:D3}";
        }
    }
}
