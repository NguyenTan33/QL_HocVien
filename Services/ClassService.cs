using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public class ClassService : IClassService
    {
        private readonly IClassRepository _classRepository;

        public ClassService(IClassRepository classRepository)
        {
            _classRepository = classRepository;
        }

        public async Task<IEnumerable<MilitaryClass>> GetAllClassesAsync()
        {
            return await _classRepository.GetAllWithCadetsAsync();
        }

        public async Task<IEnumerable<MilitaryClass>> SearchClassesAsync(string? keyword, string? unit, string? major)
        {
            return await _classRepository.SearchClassesAsync(keyword, unit, major);
        }

        public async Task<MilitaryClass?> GetClassByIdAsync(int id)
        {
            return await _classRepository.GetByIdAsync(id);
        }

        public async Task<MilitaryClass?> GetClassWithCadetsAsync(int id)
        {
            return await _classRepository.GetClassWithCadetsAsync(id);
        }

        public async Task<(bool Success, string Message, MilitaryClass? Class)> AddClassAsync(MilitaryClass militaryClass)
        {
            if (string.IsNullOrWhiteSpace(militaryClass.ClassCode))
                return (false, "Mã lớp học không được để trống.", null);

            if (string.IsNullOrWhiteSpace(militaryClass.ClassName))
                return (false, "Tên lớp học không được để trống.", null);

            militaryClass.ClassCode = militaryClass.ClassCode.Trim().ToUpper();
            militaryClass.ClassName = militaryClass.ClassName.Trim();

            if (await _classRepository.ExistsByCodeAsync(militaryClass.ClassCode))
                return (false, $"Mã lớp học '{militaryClass.ClassCode}' đã tồn tại trong hệ thống.", null);

            militaryClass.CreatedAt = DateTime.Now;
            await _classRepository.AddAsync(militaryClass);
            await _classRepository.SaveChangesAsync();

            return (true, "Thêm lớp học thành công!", militaryClass);
        }

        public async Task<(bool Success, string Message)> UpdateClassAsync(MilitaryClass militaryClass)
        {
            if (string.IsNullOrWhiteSpace(militaryClass.ClassCode))
                return (false, "Mã lớp học không được để trống.");

            if (string.IsNullOrWhiteSpace(militaryClass.ClassName))
                return (false, "Tên lớp học không được để trống.");

            var existing = await _classRepository.GetByIdAsync(militaryClass.Id);
            if (existing == null)
                return (false, "Không tìm thấy lớp học cần cập nhật.");

            militaryClass.ClassCode = militaryClass.ClassCode.Trim().ToUpper();
            militaryClass.ClassName = militaryClass.ClassName.Trim();

            if (!existing.ClassCode.Equals(militaryClass.ClassCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _classRepository.ExistsByCodeAsync(militaryClass.ClassCode))
                    return (false, $"Mã lớp học '{militaryClass.ClassCode}' đã tồn tại.");
            }

            existing.ClassCode = militaryClass.ClassCode;
            existing.ClassName = militaryClass.ClassName;
            existing.Unit = militaryClass.Unit;
            existing.Major = militaryClass.Major;
            existing.OfficerInCharge = militaryClass.OfficerInCharge;
            existing.AcademicYear = militaryClass.AcademicYear;
            existing.Description = militaryClass.Description;

            _classRepository.Update(existing);
            await _classRepository.SaveChangesAsync();

            return (true, "Cập nhật thông tin lớp học thành công!");
        }

        public async Task<(bool Success, string Message)> DeleteClassAsync(int id)
        {
            var existing = await _classRepository.GetClassWithCadetsAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy lớp học cần xóa.");

            int cadetCount = existing.Cadets.Count;

            _classRepository.Delete(existing);
            await _classRepository.SaveChangesAsync();

            if (cadetCount > 0)
            {
                return (true, $"Đã xóa lớp học thành công! ({cadetCount} học viên thuộc lớp đã được chuyển trạng thái tự do).");
            }

            return (true, "Xóa lớp học thành công!");
        }
    }
}
