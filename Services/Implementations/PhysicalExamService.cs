using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
{
    public class PhysicalExamService : IPhysicalExamService
    {
        private readonly IPhysicalExamRepository _examRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly IEvaluationService _evaluationService;

        public PhysicalExamService(
            IPhysicalExamRepository examRepository,
            ISubjectRepository subjectRepository,
            IEvaluationService evaluationService)
        {
            _examRepository = examRepository;
            _subjectRepository = subjectRepository;
            _evaluationService = evaluationService;
        }

        public async Task<IEnumerable<PhysicalExamRecord>> GetAllRecordsAsync()
        {
            return await _examRepository.GetAllWithDetailsAsync();
        }

        public async Task<IEnumerable<PhysicalExamRecord>> GetRecordsByCadetIdAsync(int cadetId)
        {
            return await _examRepository.GetRecordsByCadetIdAsync(cadetId);
        }

        public async Task<IEnumerable<PhysicalExamRecord>> GetFailedRecordsAsync()
        {
            return await _examRepository.GetFailedRecordsAsync();
        }

        public async Task<IEnumerable<PhysicalExamRecord>> SearchRecordsAsync(string? cadetKeyword, int? subjectId, string? grade, string? session)
        {
            return await _examRepository.SearchRecordsAsync(cadetKeyword, subjectId, grade, session);
        }

        public async Task<IEnumerable<PhysicalExamRecord>> SearchRecordsAsync(QL_HocVien.Models.Filters.PhysicalExamFilterCriteria criteria)
        {
            return await _examRepository.SearchWithCriteriaAsync(criteria);
        }

        public async Task<(bool Success, string Message, PhysicalExamRecord? Record)> AddExamRecordAsync(PhysicalExamRecord record)
        {
            if (record.CadetId <= 0)
                return (false, "Vui lÃ²ng chá»n há»c viÃªn kiá»ƒm tra.", null);

            if (record.SubjectId <= 0)
                return (false, "Vui lÃ²ng chá»n ná»™i dung mÃ´n kiá»ƒm tra.", null);

            var subject = await _subjectRepository.GetByIdAsync(record.SubjectId);
            if (subject != null)
            {
                record.Grade = _evaluationService.EvaluateGrade(subject, record.ScoreValue);
            }

            record.CreatedAt = DateTime.Now;
            await _examRepository.AddAsync(record);
            await _examRepository.SaveChangesAsync();

            return (true, $"LÆ°u káº¿t quáº£ thÃ nh cÃ´ng! Xáº¿p loáº¡i: {record.Grade}", record);
        }

        public async Task<(bool Success, string Message)> UpdateExamRecordAsync(PhysicalExamRecord record)
        {
            var existing = await _examRepository.GetByIdAsync(record.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y káº¿t quáº£ kiá»ƒm tra cáº§n cáº­p nháº­t.");

            var subject = await _subjectRepository.GetByIdAsync(record.SubjectId);
            if (subject != null)
            {
                record.Grade = _evaluationService.EvaluateGrade(subject, record.ScoreValue);
            }

            existing.CadetId = record.CadetId;
            existing.SubjectId = record.SubjectId;
            existing.ExamDate = record.ExamDate;
            existing.ExamSession = record.ExamSession;
            existing.ScoreValue = record.ScoreValue;
            existing.Grade = record.Grade;
            existing.Notes = record.Notes;

            _examRepository.Update(existing);
            await _examRepository.SaveChangesAsync();

            return (true, $"Cáº­p nháº­t káº¿t quáº£ thÃ nh cÃ´ng! Xáº¿p loáº¡i má»›i: {existing.Grade}");
        }

        public async Task<(bool Success, string Message)> DeleteExamRecordAsync(int id)
        {
            var existing = await _examRepository.GetByIdAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y káº¿t quáº£ kiá»ƒm tra cáº§n xÃ³a.");

            _examRepository.Delete(existing);
            await _examRepository.SaveChangesAsync();

            return (true, "XÃ³a káº¿t quáº£ kiá»ƒm tra thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleExamRecordsAsync(IEnumerable<int> recordIds)
        {
            var idList = recordIds?.Distinct().ToList() ?? new List<int>();
            if (!idList.Any())
                return (false, "KhÃ´ng cÃ³ káº¿t quáº£ kiá»ƒm tra nÃ o Ä‘Æ°á»£c chá»n Ä‘á»ƒ xÃ³a.", 0);

            int deleted = 0;
            try
            {
                foreach (var id in idList)
                {
                    var existing = await _examRepository.GetByIdAsync(id);
                    if (existing != null)
                    {
                        _examRepository.Delete(existing);
                        deleted++;
                    }
                }
                if (deleted > 0)
                {
                    await _examRepository.SaveChangesAsync();
                }
                return (true, $"ÄÃ£ xÃ³a thÃ nh cÃ´ng {deleted} káº¿t quáº£ kiá»ƒm tra thá»ƒ lá»±c.", deleted);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xÃ³a káº¿t quáº£ kiá»ƒm tra: {ex.Message}", deleted);
            }
        }
    }
}

