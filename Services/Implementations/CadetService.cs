using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
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
                return (false, "MÃ£ há»c viÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            if (string.IsNullOrWhiteSpace(cadet.FullName))
                return (false, "Há» vÃ  tÃªn há»c viÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            if (await _cadetRepository.ExistsByCodeAsync(cadet.CadetCode))
                return (false, $"MÃ£ há»c viÃªn '{cadet.CadetCode}' Ä‘Ã£ tá»“n táº¡i.", null);

            if (cadet.DateOfBirth.HasValue && !cadet.Age.HasValue)
            {
                cadet.Age = DateTime.Today.Year - cadet.DateOfBirth.Value.Year;
            }

            await _cadetRepository.AddAsync(cadet);
            await _cadetRepository.SaveChangesAsync();

            return (true, "ThÃªm há»c viÃªn thÃ nh cÃ´ng!", cadet);
        }

        public async Task<(bool Success, string Message)> UpdateCadetAsync(Cadet cadet)
        {
            if (string.IsNullOrWhiteSpace(cadet.FullName))
                return (false, "Há» vÃ  tÃªn há»c viÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            if (string.IsNullOrWhiteSpace(cadet.CadetCode))
                return (false, "MÃ£ há»c viÃªn (ID) khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            if (await _cadetRepository.ExistsByCodeAsync(cadet.CadetCode, cadet.Id))
            {
                return (false, $"MÃ£ há»c viÃªn '{cadet.CadetCode}' Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng.");
            }

            var existing = await _cadetRepository.GetByIdAsync(cadet.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y há»c viÃªn Ä‘á»ƒ cáº­p nháº­t.");

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
                return (true, "Cáº­p nháº­t thÃ´ng tin há»c viÃªn thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i cáº­p nháº­t há»c viÃªn: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteCadetAsync(int id)
        {
            var cadet = await _cadetRepository.GetByIdAsync(id);
            if (cadet == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y há»c viÃªn cáº§n xÃ³a.");

            _cadetRepository.Delete(cadet);
            await _cadetRepository.SaveChangesAsync();

            return (true, "XÃ³a há»c viÃªn thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleCadetsAsync(IEnumerable<int> cadetIds)
        {
            var ids = cadetIds?.Distinct().ToList() ?? new List<int>();
            if (!ids.Any())
                return (false, "KhÃ´ng cÃ³ há»c viÃªn nÃ o Ä‘Æ°á»£c chá»n Ä‘á»ƒ xÃ³a.", 0);

            try
            {
                int count = await _cadetRepository.DeleteMultipleAsync(ids);
                return (true, $"ÄÃ£ xÃ³a thÃ nh cÃ´ng {count} há»c viÃªn.", count);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xÃ³a há»c viÃªn: {ex.Message}", 0);
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

