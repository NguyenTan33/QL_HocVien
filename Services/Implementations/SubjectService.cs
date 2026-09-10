using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;

        public SubjectService(ISubjectRepository subjectRepository)
        {
            _subjectRepository = subjectRepository;
        }

        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            return await _subjectRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Subject>> SearchSubjectsAsync(string? keyword, string? category)
        {
            return await _subjectRepository.SearchSubjectsAsync(keyword, category);
        }

        public async Task<IEnumerable<Subject>> SearchSubjectsAsync(QL_HocVien.Models.Filters.SubjectFilterCriteria criteria)
        {
            return await _subjectRepository.SearchWithCriteriaAsync(criteria);
        }

        public async Task<Subject?> GetSubjectByIdAsync(int id)
        {
            return await _subjectRepository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message, Subject? Subject)> AddSubjectAsync(Subject subject)
        {
            if (string.IsNullOrWhiteSpace(subject.SubjectCode))
                return (false, "MÃ£ mÃ´n há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            if (string.IsNullOrWhiteSpace(subject.SubjectName))
                return (false, "TÃªn mÃ´n há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            if (await _subjectRepository.ExistsByCodeAsync(subject.SubjectCode))
                return (false, $"MÃ£ mÃ´n '{subject.SubjectCode}' Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.", null);

            await _subjectRepository.AddAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            return (true, "ThÃªm mÃ´n há»c má»›i thÃ nh cÃ´ng!", subject);
        }

        public async Task<(bool Success, string Message)> UpdateSubjectAsync(Subject subject)
        {
            if (string.IsNullOrWhiteSpace(subject.SubjectName))
                return (false, "TÃªn mÃ´n há»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            var existing = await _subjectRepository.GetByIdAsync(subject.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y mÃ´n há»c cáº§n cáº­p nháº­t.");

            if (existing.SubjectCode != subject.SubjectCode)
            {
                if (await _subjectRepository.ExistsByCodeAsync(subject.SubjectCode))
                    return (false, $"MÃ£ mÃ´n '{subject.SubjectCode}' Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng.");
            }

            existing.SubjectCode = subject.SubjectCode;
            existing.SubjectName = subject.SubjectName;
            existing.Category = subject.Category;
            existing.Unit = subject.Unit;
            existing.Description = subject.Description;
            existing.ExcellentThreshold = subject.ExcellentThreshold;
            existing.GoodThreshold = subject.GoodThreshold;
            existing.PassThreshold = subject.PassThreshold;
            existing.IsHigherBetter = subject.IsHigherBetter;

            _subjectRepository.Update(existing);
            await _subjectRepository.SaveChangesAsync();

            return (true, "Cáº­p nháº­t thÃ´ng tin mÃ´n há»c thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> DeleteSubjectAsync(int id)
        {
            var existing = await _subjectRepository.GetByIdAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y mÃ´n há»c cáº§n xÃ³a.");

            _subjectRepository.Delete(existing);
            await _subjectRepository.SaveChangesAsync();

            return (true, "XÃ³a mÃ´n há»c thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleSubjectsAsync(IEnumerable<int> subjectIds)
        {
            var idList = subjectIds?.Distinct().ToList() ?? new List<int>();
            if (!idList.Any())
                return (false, "KhÃ´ng cÃ³ mÃ´n há»c nÃ o Ä‘Æ°á»£c chá»n Ä‘á»ƒ xÃ³a.", 0);

            int deleted = 0;
            try
            {
                foreach (var id in idList)
                {
                    var existing = await _subjectRepository.GetByIdAsync(id);
                    if (existing != null)
                    {
                        _subjectRepository.Delete(existing);
                        deleted++;
                    }
                }
                if (deleted > 0)
                {
                    await _subjectRepository.SaveChangesAsync();
                }
                return (true, $"ÄÃ£ xÃ³a thÃ nh cÃ´ng {deleted} mÃ´n há»c.", deleted);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xÃ³a mÃ´n há»c: {ex.Message}", deleted);
            }
        }
    }
}

