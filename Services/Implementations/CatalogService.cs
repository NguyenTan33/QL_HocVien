using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
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

        #region 1. Cáº¤P Báº¬C QUÃ‚N HÃ€M
        public async Task<IEnumerable<MilitaryRank>> GetAllRanksAsync() => await _rankRepo.GetAllAsync();

        public async Task<IEnumerable<MilitaryRank>> SearchRanksAsync(string? keyword, string? group) =>
            await _rankRepo.SearchRanksAsync(keyword, group);

        public async Task<IEnumerable<MilitaryRank>> SearchRanksAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria) =>
            await _rankRepo.SearchWithCriteriaAsync(criteria);

        public async Task<MilitaryRank?> GetRankByIdAsync(int id) => await _rankRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, MilitaryRank? Rank)> AddRankAsync(MilitaryRank rank)
        {
            if (string.IsNullOrWhiteSpace(rank.RankCode))
                return (false, "MÃ£ cáº¥p báº­c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);
            if (string.IsNullOrWhiteSpace(rank.RankName))
                return (false, "TÃªn cáº¥p báº­c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            rank.RankCode = rank.RankCode.Trim().ToUpper();
            rank.RankName = rank.RankName.Trim();

            if (await _rankRepo.ExistsByCodeAsync(rank.RankCode))
                return (false, $"MÃ£ cáº¥p báº­c '{rank.RankCode}' Ä‘Ã£ tá»“n táº¡i.", null);

            rank.CreatedAt = DateTime.Now;
            await _rankRepo.AddAsync(rank);
            await _rankRepo.SaveChangesAsync();
            return (true, "ThÃªm cáº¥p báº­c thÃ nh cÃ´ng!", rank);
        }

        public async Task<(bool Success, string Message)> UpdateRankAsync(MilitaryRank rank)
        {
            if (string.IsNullOrWhiteSpace(rank.RankCode))
                return (false, "MÃ£ cáº¥p báº­c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            if (string.IsNullOrWhiteSpace(rank.RankName))
                return (false, "TÃªn cáº¥p báº­c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            var existing = await _rankRepo.GetByIdAsync(rank.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y cáº¥p báº­c cáº§n cáº­p nháº­t.");

            rank.RankCode = rank.RankCode.Trim().ToUpper();
            rank.RankName = rank.RankName.Trim();

            if (!existing.RankCode.Equals(rank.RankCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _rankRepo.ExistsByCodeAsync(rank.RankCode))
                    return (false, $"MÃ£ cáº¥p báº­c '{rank.RankCode}' Ä‘Ã£ tá»“n táº¡i.");
            }

            existing.RankCode = rank.RankCode;
            existing.RankName = rank.RankName;
            existing.RankGroup = rank.RankGroup;
            existing.DisplayOrder = rank.DisplayOrder;
            existing.Description = rank.Description;

            _rankRepo.Update(existing);
            await _rankRepo.SaveChangesAsync();
            return (true, "Cáº­p nháº­t cáº¥p báº­c thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> DeleteRankAsync(int id)
        {
            var existing = await _rankRepo.GetByIdAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y cáº¥p báº­c cáº§n xÃ³a.");

            _rankRepo.Delete(existing);
            await _rankRepo.SaveChangesAsync();
            return (true, "ÄÃ£ xÃ³a cáº¥p báº­c thÃ nh cÃ´ng!");
        }

        public async Task<List<string>> GetRankNamesAsync()
        {
            var list = await _rankRepo.GetAllAsync();
            return list.OrderBy(r => r.DisplayOrder).Select(r => r.RankName).ToList();
        }

        public Task<List<string>> GetRankDropdownAsync() => GetRankNamesAsync();
        #endregion

        #region 2. CHá»¨C Vá»¤ QUÃ‚N Sá»°
        public async Task<IEnumerable<MilitaryPosition>> GetAllPositionsAsync() => await _positionRepo.GetAllAsync();

        public async Task<IEnumerable<MilitaryPosition>> SearchPositionsAsync(string? keyword, string? group) =>
            await _positionRepo.SearchPositionsAsync(keyword, group);

        public async Task<IEnumerable<MilitaryPosition>> SearchPositionsAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria) =>
            await _positionRepo.SearchWithCriteriaAsync(criteria);

        public async Task<MilitaryPosition?> GetPositionByIdAsync(int id) => await _positionRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, MilitaryPosition? Position)> AddPositionAsync(MilitaryPosition position)
        {
            if (string.IsNullOrWhiteSpace(position.PositionCode))
                return (false, "MÃ£ chá»©c vá»¥ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);
            if (string.IsNullOrWhiteSpace(position.PositionName))
                return (false, "TÃªn chá»©c vá»¥ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            position.PositionCode = position.PositionCode.Trim().ToUpper();
            position.PositionName = position.PositionName.Trim();

            if (await _positionRepo.ExistsByCodeAsync(position.PositionCode))
                return (false, $"MÃ£ chá»©c vá»¥ '{position.PositionCode}' Ä‘Ã£ tá»“n táº¡i.", null);

            position.CreatedAt = DateTime.Now;
            await _positionRepo.AddAsync(position);
            await _positionRepo.SaveChangesAsync();
            return (true, "ThÃªm chá»©c vá»¥ thÃ nh cÃ´ng!", position);
        }

        public async Task<(bool Success, string Message)> UpdatePositionAsync(MilitaryPosition position)
        {
            if (string.IsNullOrWhiteSpace(position.PositionCode))
                return (false, "MÃ£ chá»©c vá»¥ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            if (string.IsNullOrWhiteSpace(position.PositionName))
                return (false, "TÃªn chá»©c vá»¥ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            var existing = await _positionRepo.GetByIdAsync(position.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y chá»©c vá»¥ cáº§n cáº­p nháº­t.");

            position.PositionCode = position.PositionCode.Trim().ToUpper();
            position.PositionName = position.PositionName.Trim();

            if (!existing.PositionCode.Equals(position.PositionCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _positionRepo.ExistsByCodeAsync(position.PositionCode))
                    return (false, $"MÃ£ chá»©c vá»¥ '{position.PositionCode}' Ä‘Ã£ tá»“n táº¡i.");
            }

            existing.PositionCode = position.PositionCode;
            existing.PositionName = position.PositionName;
            existing.PositionGroup = position.PositionGroup;
            existing.DisplayOrder = position.DisplayOrder;
            existing.Description = position.Description;

            _positionRepo.Update(existing);
            await _positionRepo.SaveChangesAsync();
            return (true, "Cáº­p nháº­t chá»©c vá»¥ thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> DeletePositionAsync(int id)
        {
            var existing = await _positionRepo.GetByIdAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y chá»©c vá»¥ cáº§n xÃ³a.");

            _positionRepo.Delete(existing);
            await _positionRepo.SaveChangesAsync();
            return (true, "ÄÃ£ xÃ³a chá»©c vá»¥ thÃ nh cÃ´ng!");
        }

        public async Task<List<string>> GetPositionNamesAsync()
        {
            var list = await _positionRepo.GetAllAsync();
            return list.OrderBy(p => p.DisplayOrder).Select(p => p.PositionName).ToList();
        }

        public Task<List<string>> GetPositionDropdownAsync() => GetPositionNamesAsync();
        #endregion

        #region 3. ÄÆ N Vá»Š QUÃ‚N Äá»˜I
        public async Task<IEnumerable<MilitaryUnit>> GetAllUnitsAsync() => await _unitRepo.GetAllAsync();

        public async Task<IEnumerable<MilitaryUnit>> SearchUnitsAsync(string? keyword, string? parentUnit) =>
            await _unitRepo.SearchUnitsAsync(keyword, parentUnit);

        public async Task<IEnumerable<MilitaryUnit>> SearchUnitsAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria) =>
            await _unitRepo.SearchWithCriteriaAsync(criteria);

        public async Task<MilitaryUnit?> GetUnitByIdAsync(int id) => await _unitRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, MilitaryUnit? Unit)> AddUnitAsync(MilitaryUnit unit)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitCode))
                return (false, "MÃ£ Ä‘Æ¡n vá»‹ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);
            if (string.IsNullOrWhiteSpace(unit.UnitName))
                return (false, "TÃªn Ä‘Æ¡n vá»‹ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            unit.UnitCode = unit.UnitCode.Trim().ToUpper();
            unit.UnitName = unit.UnitName.Trim();

            if (await _unitRepo.ExistsByCodeAsync(unit.UnitCode))
                return (false, $"MÃ£ Ä‘Æ¡n vá»‹ '{unit.UnitCode}' Ä‘Ã£ tá»“n táº¡i.", null);

            unit.CreatedAt = DateTime.Now;
            await _unitRepo.AddAsync(unit);
            await _unitRepo.SaveChangesAsync();
            return (true, "ThÃªm Ä‘Æ¡n vá»‹ thÃ nh cÃ´ng!", unit);
        }

        public async Task<(bool Success, string Message)> UpdateUnitAsync(MilitaryUnit unit)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitCode))
                return (false, "MÃ£ Ä‘Æ¡n vá»‹ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            if (string.IsNullOrWhiteSpace(unit.UnitName))
                return (false, "TÃªn Ä‘Æ¡n vá»‹ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            var existing = await _unitRepo.GetByIdAsync(unit.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n vá»‹ cáº§n cáº­p nháº­t.");

            unit.UnitCode = unit.UnitCode.Trim().ToUpper();
            unit.UnitName = unit.UnitName.Trim();

            if (!existing.UnitCode.Equals(unit.UnitCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _unitRepo.ExistsByCodeAsync(unit.UnitCode))
                    return (false, $"MÃ£ Ä‘Æ¡n vá»‹ '{unit.UnitCode}' Ä‘Ã£ tá»“n táº¡i.");
            }

            existing.UnitCode = unit.UnitCode;
            existing.UnitName = unit.UnitName;
            existing.ParentUnit = unit.ParentUnit;
            existing.CommanderName = unit.CommanderName;
            existing.ContactPhone = unit.ContactPhone;
            existing.Description = unit.Description;

            _unitRepo.Update(existing);
            await _unitRepo.SaveChangesAsync();
            return (true, "Cáº­p nháº­t Ä‘Æ¡n vá»‹ thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> DeleteUnitAsync(int id)
        {
            var existing = await _unitRepo.GetByIdAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n vá»‹ cáº§n xÃ³a.");

            _unitRepo.Delete(existing);
            await _unitRepo.SaveChangesAsync();
            return (true, "ÄÃ£ xÃ³a Ä‘Æ¡n vá»‹ thÃ nh cÃ´ng!");
        }

        public async Task<List<string>> GetUnitNamesAsync()
        {
            var list = await _unitRepo.GetAllAsync();
            return list.OrderBy(u => u.UnitCode).Select(u => u.UnitName).ToList();
        }

        public Task<List<string>> GetUnitDropdownAsync() => GetUnitNamesAsync();
        #endregion

        #region 4. CHUYÃŠN NGÃ€NH ÄÃ€O Táº O
        public async Task<IEnumerable<MilitaryMajor>> GetAllMajorsAsync() => await _majorRepo.GetAllAsync();

        public async Task<IEnumerable<MilitaryMajor>> SearchMajorsAsync(string? keyword, string? department) =>
            await _majorRepo.SearchMajorsAsync(keyword, department);

        public async Task<IEnumerable<MilitaryMajor>> SearchMajorsAsync(QL_HocVien.Models.Filters.CatalogFilterCriteria criteria) =>
            await _majorRepo.SearchWithCriteriaAsync(criteria);

        public async Task<MilitaryMajor?> GetMajorByIdAsync(int id) => await _majorRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, MilitaryMajor? Major)> AddMajorAsync(MilitaryMajor major)
        {
            if (string.IsNullOrWhiteSpace(major.MajorCode))
                return (false, "MÃ£ chuyÃªn ngÃ nh khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);
            if (string.IsNullOrWhiteSpace(major.MajorName))
                return (false, "TÃªn chuyÃªn ngÃ nh khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            major.MajorCode = major.MajorCode.Trim().ToUpper();
            major.MajorName = major.MajorName.Trim();

            if (await _majorRepo.ExistsByCodeAsync(major.MajorCode))
                return (false, $"MÃ£ chuyÃªn ngÃ nh '{major.MajorCode}' Ä‘Ã£ tá»“n táº¡i.", null);

            major.CreatedAt = DateTime.Now;
            await _majorRepo.AddAsync(major);
            await _majorRepo.SaveChangesAsync();
            return (true, "ThÃªm chuyÃªn ngÃ nh thÃ nh cÃ´ng!", major);
        }

        public async Task<(bool Success, string Message)> UpdateMajorAsync(MilitaryMajor major)
        {
            if (string.IsNullOrWhiteSpace(major.MajorCode))
                return (false, "MÃ£ chuyÃªn ngÃ nh khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            if (string.IsNullOrWhiteSpace(major.MajorName))
                return (false, "TÃªn chuyÃªn ngÃ nh khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            var existing = await _majorRepo.GetByIdAsync(major.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y chuyÃªn ngÃ nh cáº§n cáº­p nháº­t.");

            major.MajorCode = major.MajorCode.Trim().ToUpper();
            major.MajorName = major.MajorName.Trim();

            if (!existing.MajorCode.Equals(major.MajorCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _majorRepo.ExistsByCodeAsync(major.MajorCode))
                    return (false, $"MÃ£ chuyÃªn ngÃ nh '{major.MajorCode}' Ä‘Ã£ tá»“n táº¡i.");
            }

            existing.MajorCode = major.MajorCode;
            existing.MajorName = major.MajorName;
            existing.TrainingDuration = major.TrainingDuration;
            existing.Department = major.Department;
            existing.Description = major.Description;

            _majorRepo.Update(existing);
            await _majorRepo.SaveChangesAsync();
            return (true, "Cáº­p nháº­t chuyÃªn ngÃ nh thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> DeleteMajorAsync(int id)
        {
            var existing = await _majorRepo.GetByIdAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y chuyÃªn ngÃ nh cáº§n xÃ³a.");

            _majorRepo.Delete(existing);
            await _majorRepo.SaveChangesAsync();
            return (true, "ÄÃ£ xÃ³a chuyÃªn ngÃ nh thÃ nh cÃ´ng!");
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

