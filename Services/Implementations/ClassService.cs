using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
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

        public async Task<IEnumerable<MilitaryClass>> SearchClassesAsync(QL_HocVien.Models.Filters.ClassFilterCriteria criteria)
        {
            return await _classRepository.SearchWithCriteriaAsync(criteria);
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
                return (false, "MÃ£ lá»›p há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            if (string.IsNullOrWhiteSpace(militaryClass.ClassName))
                return (false, "TÃªn lá»›p há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            militaryClass.ClassCode = militaryClass.ClassCode.Trim().ToUpper();
            militaryClass.ClassName = militaryClass.ClassName.Trim();

            if (await _classRepository.ExistsByCodeAsync(militaryClass.ClassCode))
                return (false, $"MÃ£ lá»›p há»c '{militaryClass.ClassCode}' Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.", null);

            militaryClass.CreatedAt = DateTime.Now;
            await _classRepository.AddAsync(militaryClass);
            await _classRepository.SaveChangesAsync();

            return (true, "ThÃªm lá»›p há»c thÃ nh cÃ´ng!", militaryClass);
        }

        public async Task<(bool Success, string Message)> UpdateClassAsync(MilitaryClass militaryClass)
        {
            if (string.IsNullOrWhiteSpace(militaryClass.ClassCode))
                return (false, "MÃ£ lá»›p há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            if (string.IsNullOrWhiteSpace(militaryClass.ClassName))
                return (false, "TÃªn lá»›p há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            var existing = await _classRepository.GetByIdAsync(militaryClass.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y lá»›p há»c cáº§n cáº­p nháº­t.");

            militaryClass.ClassCode = militaryClass.ClassCode.Trim().ToUpper();
            militaryClass.ClassName = militaryClass.ClassName.Trim();

            if (!existing.ClassCode.Equals(militaryClass.ClassCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _classRepository.ExistsByCodeAsync(militaryClass.ClassCode))
                    return (false, $"MÃ£ lá»›p há»c '{militaryClass.ClassCode}' Ä‘Ã£ tá»“n táº¡i.");
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

            return (true, "Cáº­p nháº­t thÃ´ng tin lá»›p há»c thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> DeleteClassAsync(int id)
        {
            var existing = await _classRepository.GetClassWithCadetsAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y lá»›p há»c cáº§n xÃ³a.");

            int cadetCount = existing.Cadets.Count;

            _classRepository.Delete(existing);
            await _classRepository.SaveChangesAsync();

            if (cadetCount > 0)
            {
                return (true, $"ÄÃ£ xÃ³a lá»›p há»c thÃ nh cÃ´ng! ({cadetCount} há»c viÃªn thuá»™c lá»›p Ä‘Ã£ Ä‘Æ°á»£c chuyá»ƒn tráº¡ng thÃ¡i tá»± do).");
            }

            return (true, "XÃ³a lá»›p há»c thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleClassesAsync(IEnumerable<int> classIds)
        {
            var idList = classIds?.Distinct().ToList() ?? new List<int>();
            if (!idList.Any())
                return (false, "KhÃ´ng cÃ³ lá»›p há»c nÃ o Ä‘Æ°á»£c chá»n Ä‘á»ƒ xÃ³a.", 0);

            int deleted = 0;
            try
            {
                foreach (var id in idList)
                {
                    var existing = await _classRepository.GetClassWithCadetsAsync(id);
                    if (existing != null)
                    {
                        _classRepository.Delete(existing);
                        deleted++;
                    }
                }
                if (deleted > 0)
                {
                    await _classRepository.SaveChangesAsync();
                }
                return (true, $"ÄÃ£ xÃ³a thÃ nh cÃ´ng {deleted} lá»›p há»c.", deleted);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xÃ³a lá»›p há»c: {ex.Message}", deleted);
            }
        }
    }
}

