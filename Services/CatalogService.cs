using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly IRankRepository _rankRepo;
        private readonly IPositionRepository _positionRepo;
        private readonly IUnitRepository _unitRepo;
        private readonly IMajorRepository _majorRepo;

        public CatalogService(
            IRankRepository rankRepo,
            IPositionRepository positionRepo,
            IUnitRepository unitRepo,
            IMajorRepository majorRepo)
        {
            _rankRepo = rankRepo;
            _positionRepo = positionRepo;
            _unitRepo = unitRepo;
            _majorRepo = majorRepo;
        }

        #region 1. CẤP BẬC QUÂN HÀM
        public async Task<IEnumerable<MilitaryRank>> GetAllRanksAsync() => await _rankRepo.GetAllAsync();

        public async Task<IEnumerable<MilitaryRank>> SearchRanksAsync(string? keyword, string? group) =>
            await _rankRepo.SearchRanksAsync(keyword, group);

        public async Task<MilitaryRank?> GetRankByIdAsync(int id) => await _rankRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, MilitaryRank? Rank)> AddRankAsync(MilitaryRank rank)
        {
            if (string.IsNullOrWhiteSpace(rank.RankCode))
                return (false, "Mã cấp bậc không được để trống.", null);
            if (string.IsNullOrWhiteSpace(rank.RankName))
                return (false, "Tên cấp bậc không được để trống.", null);

            rank.RankCode = rank.RankCode.Trim().ToUpper();
            rank.RankName = rank.RankName.Trim();

            if (await _rankRepo.ExistsByCodeAsync(rank.RankCode))
                return (false, $"Mã cấp bậc '{rank.RankCode}' đã tồn tại.", null);

            rank.CreatedAt = DateTime.Now;
            await _rankRepo.AddAsync(rank);
            await _rankRepo.SaveChangesAsync();
            return (true, "Thêm cấp bậc thành công!", rank);
        }

        public async Task<(bool Success, string Message)> UpdateRankAsync(MilitaryRank rank)
        {
            if (string.IsNullOrWhiteSpace(rank.RankCode))
                return (false, "Mã cấp bậc không được để trống.");
            if (string.IsNullOrWhiteSpace(rank.RankName))
                return (false, "Tên cấp bậc không được để trống.");

            var existing = await _rankRepo.GetByIdAsync(rank.Id);
            if (existing == null)
                return (false, "Không tìm thấy cấp bậc cần cập nhật.");

            rank.RankCode = rank.RankCode.Trim().ToUpper();
            rank.RankName = rank.RankName.Trim();

            if (!existing.RankCode.Equals(rank.RankCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _rankRepo.ExistsByCodeAsync(rank.RankCode))
                    return (false, $"Mã cấp bậc '{rank.RankCode}' đã tồn tại.");
            }

            existing.RankCode = rank.RankCode;
            existing.RankName = rank.RankName;
            existing.RankGroup = rank.RankGroup;
            existing.DisplayOrder = rank.DisplayOrder;
            existing.Description = rank.Description;

            _rankRepo.Update(existing);
            await _rankRepo.SaveChangesAsync();
            return (true, "Cập nhật cấp bậc thành công!");
        }

        public async Task<(bool Success, string Message)> DeleteRankAsync(int id)
        {
            var existing = await _rankRepo.GetByIdAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy cấp bậc cần xóa.");

            _rankRepo.Delete(existing);
            await _rankRepo.SaveChangesAsync();
            return (true, "Đã xóa cấp bậc thành công!");
        }

        public async Task<List<string>> GetRankNamesAsync()
        {
            var list = await _rankRepo.GetAllAsync();
            return list.OrderBy(r => r.DisplayOrder).Select(r => r.RankName).ToList();
        }

        public Task<List<string>> GetRankDropdownAsync() => GetRankNamesAsync();
        #endregion

        #region 2. CHỨC VỤ QUÂN SỰ
        public async Task<IEnumerable<MilitaryPosition>> GetAllPositionsAsync() => await _positionRepo.GetAllAsync();

        public async Task<IEnumerable<MilitaryPosition>> SearchPositionsAsync(string? keyword, string? group) =>
            await _positionRepo.SearchPositionsAsync(keyword, group);

        public async Task<MilitaryPosition?> GetPositionByIdAsync(int id) => await _positionRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, MilitaryPosition? Position)> AddPositionAsync(MilitaryPosition position)
        {
            if (string.IsNullOrWhiteSpace(position.PositionCode))
                return (false, "Mã chức vụ không được để trống.", null);
            if (string.IsNullOrWhiteSpace(position.PositionName))
                return (false, "Tên chức vụ không được để trống.", null);

            position.PositionCode = position.PositionCode.Trim().ToUpper();
            position.PositionName = position.PositionName.Trim();

            if (await _positionRepo.ExistsByCodeAsync(position.PositionCode))
                return (false, $"Mã chức vụ '{position.PositionCode}' đã tồn tại.", null);

            position.CreatedAt = DateTime.Now;
            await _positionRepo.AddAsync(position);
            await _positionRepo.SaveChangesAsync();
            return (true, "Thêm chức vụ thành công!", position);
        }

        public async Task<(bool Success, string Message)> UpdatePositionAsync(MilitaryPosition position)
        {
            if (string.IsNullOrWhiteSpace(position.PositionCode))
                return (false, "Mã chức vụ không được để trống.");
            if (string.IsNullOrWhiteSpace(position.PositionName))
                return (false, "Tên chức vụ không được để trống.");

            var existing = await _positionRepo.GetByIdAsync(position.Id);
            if (existing == null)
                return (false, "Không tìm thấy chức vụ cần cập nhật.");

            position.PositionCode = position.PositionCode.Trim().ToUpper();
            position.PositionName = position.PositionName.Trim();

            if (!existing.PositionCode.Equals(position.PositionCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _positionRepo.ExistsByCodeAsync(position.PositionCode))
                    return (false, $"Mã chức vụ '{position.PositionCode}' đã tồn tại.");
            }

            existing.PositionCode = position.PositionCode;
            existing.PositionName = position.PositionName;
            existing.PositionGroup = position.PositionGroup;
            existing.DisplayOrder = position.DisplayOrder;
            existing.Description = position.Description;

            _positionRepo.Update(existing);
            await _positionRepo.SaveChangesAsync();
            return (true, "Cập nhật chức vụ thành công!");
        }

        public async Task<(bool Success, string Message)> DeletePositionAsync(int id)
        {
            var existing = await _positionRepo.GetByIdAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy chức vụ cần xóa.");

            _positionRepo.Delete(existing);
            await _positionRepo.SaveChangesAsync();
            return (true, "Đã xóa chức vụ thành công!");
        }

        public async Task<List<string>> GetPositionNamesAsync()
        {
            var list = await _positionRepo.GetAllAsync();
            return list.OrderBy(p => p.DisplayOrder).Select(p => p.PositionName).ToList();
        }

        public Task<List<string>> GetPositionDropdownAsync() => GetPositionNamesAsync();
        #endregion

        #region 3. ĐƠN VỊ QUÂN ĐỘI
        public async Task<IEnumerable<MilitaryUnit>> GetAllUnitsAsync() => await _unitRepo.GetAllAsync();

        public async Task<IEnumerable<MilitaryUnit>> SearchUnitsAsync(string? keyword, string? parentUnit) =>
            await _unitRepo.SearchUnitsAsync(keyword, parentUnit);

        public async Task<MilitaryUnit?> GetUnitByIdAsync(int id) => await _unitRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, MilitaryUnit? Unit)> AddUnitAsync(MilitaryUnit unit)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitCode))
                return (false, "Mã đơn vị không được để trống.", null);
            if (string.IsNullOrWhiteSpace(unit.UnitName))
                return (false, "Tên đơn vị không được để trống.", null);

            unit.UnitCode = unit.UnitCode.Trim().ToUpper();
            unit.UnitName = unit.UnitName.Trim();

            if (await _unitRepo.ExistsByCodeAsync(unit.UnitCode))
                return (false, $"Mã đơn vị '{unit.UnitCode}' đã tồn tại.", null);

            unit.CreatedAt = DateTime.Now;
            await _unitRepo.AddAsync(unit);
            await _unitRepo.SaveChangesAsync();
            return (true, "Thêm đơn vị thành công!", unit);
        }

        public async Task<(bool Success, string Message)> UpdateUnitAsync(MilitaryUnit unit)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitCode))
                return (false, "Mã đơn vị không được để trống.");
            if (string.IsNullOrWhiteSpace(unit.UnitName))
                return (false, "Tên đơn vị không được để trống.");

            var existing = await _unitRepo.GetByIdAsync(unit.Id);
            if (existing == null)
                return (false, "Không tìm thấy đơn vị cần cập nhật.");

            unit.UnitCode = unit.UnitCode.Trim().ToUpper();
            unit.UnitName = unit.UnitName.Trim();

            if (!existing.UnitCode.Equals(unit.UnitCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _unitRepo.ExistsByCodeAsync(unit.UnitCode))
                    return (false, $"Mã đơn vị '{unit.UnitCode}' đã tồn tại.");
            }

            existing.UnitCode = unit.UnitCode;
            existing.UnitName = unit.UnitName;
            existing.ParentUnit = unit.ParentUnit;
            existing.CommanderName = unit.CommanderName;
            existing.ContactPhone = unit.ContactPhone;
            existing.Description = unit.Description;

            _unitRepo.Update(existing);
            await _unitRepo.SaveChangesAsync();
            return (true, "Cập nhật đơn vị thành công!");
        }

        public async Task<(bool Success, string Message)> DeleteUnitAsync(int id)
        {
            var existing = await _unitRepo.GetByIdAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy đơn vị cần xóa.");

            _unitRepo.Delete(existing);
            await _unitRepo.SaveChangesAsync();
            return (true, "Đã xóa đơn vị thành công!");
        }

        public async Task<List<string>> GetUnitNamesAsync()
        {
            var list = await _unitRepo.GetAllAsync();
            return list.OrderBy(u => u.UnitCode).Select(u => u.UnitName).ToList();
        }

        public Task<List<string>> GetUnitDropdownAsync() => GetUnitNamesAsync();
        #endregion

        #region 4. CHUYÊN NGÀNH ĐÀO TẠO
        public async Task<IEnumerable<MilitaryMajor>> GetAllMajorsAsync() => await _majorRepo.GetAllAsync();

        public async Task<IEnumerable<MilitaryMajor>> SearchMajorsAsync(string? keyword, string? department) =>
            await _majorRepo.SearchMajorsAsync(keyword, department);

        public async Task<MilitaryMajor?> GetMajorByIdAsync(int id) => await _majorRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, MilitaryMajor? Major)> AddMajorAsync(MilitaryMajor major)
        {
            if (string.IsNullOrWhiteSpace(major.MajorCode))
                return (false, "Mã chuyên ngành không được để trống.", null);
            if (string.IsNullOrWhiteSpace(major.MajorName))
                return (false, "Tên chuyên ngành không được để trống.", null);

            major.MajorCode = major.MajorCode.Trim().ToUpper();
            major.MajorName = major.MajorName.Trim();

            if (await _majorRepo.ExistsByCodeAsync(major.MajorCode))
                return (false, $"Mã chuyên ngành '{major.MajorCode}' đã tồn tại.", null);

            major.CreatedAt = DateTime.Now;
            await _majorRepo.AddAsync(major);
            await _majorRepo.SaveChangesAsync();
            return (true, "Thêm chuyên ngành thành công!", major);
        }

        public async Task<(bool Success, string Message)> UpdateMajorAsync(MilitaryMajor major)
        {
            if (string.IsNullOrWhiteSpace(major.MajorCode))
                return (false, "Mã chuyên ngành không được để trống.");
            if (string.IsNullOrWhiteSpace(major.MajorName))
                return (false, "Tên chuyên ngành không được để trống.");

            var existing = await _majorRepo.GetByIdAsync(major.Id);
            if (existing == null)
                return (false, "Không tìm thấy chuyên ngành cần cập nhật.");

            major.MajorCode = major.MajorCode.Trim().ToUpper();
            major.MajorName = major.MajorName.Trim();

            if (!existing.MajorCode.Equals(major.MajorCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _majorRepo.ExistsByCodeAsync(major.MajorCode))
                    return (false, $"Mã chuyên ngành '{major.MajorCode}' đã tồn tại.");
            }

            existing.MajorCode = major.MajorCode;
            existing.MajorName = major.MajorName;
            existing.TrainingDuration = major.TrainingDuration;
            existing.Department = major.Department;
            existing.Description = major.Description;

            _majorRepo.Update(existing);
            await _majorRepo.SaveChangesAsync();
            return (true, "Cập nhật chuyên ngành thành công!");
        }

        public async Task<(bool Success, string Message)> DeleteMajorAsync(int id)
        {
            var existing = await _majorRepo.GetByIdAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy chuyên ngành cần xóa.");

            _majorRepo.Delete(existing);
            await _majorRepo.SaveChangesAsync();
            return (true, "Đã xóa chuyên ngành thành công!");
        }

        public async Task<List<string>> GetMajorNamesAsync()
        {
            var list = await _majorRepo.GetAllAsync();
            return list.OrderBy(m => m.MajorCode).Select(m => m.MajorName).ToList();
        }

        public Task<List<string>> GetMajorDropdownAsync() => GetMajorNamesAsync();
        #endregion
    }
}
