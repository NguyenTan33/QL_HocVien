using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services
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

        public async Task<(bool Success, string Message, PhysicalExamRecord? Record)> AddExamRecordAsync(PhysicalExamRecord record)
        {
            if (record.CadetId <= 0)
                return (false, "Vui lòng chọn học viên kiểm tra.", null);

            if (record.SubjectId <= 0)
                return (false, "Vui lòng chọn nội dung môn kiểm tra.", null);

            var subject = await _subjectRepository.GetByIdAsync(record.SubjectId);
            if (subject != null)
            {
                record.Grade = _evaluationService.EvaluateGrade(subject, record.ScoreValue);
            }

            record.CreatedAt = DateTime.Now;
            await _examRepository.AddAsync(record);
            await _examRepository.SaveChangesAsync();

            return (true, $"Lưu kết quả thành công! Xếp loại: {record.Grade}", record);
        }

        public async Task<(bool Success, string Message)> UpdateExamRecordAsync(PhysicalExamRecord record)
        {
            var existing = await _examRepository.GetByIdAsync(record.Id);
            if (existing == null)
                return (false, "Không tìm thấy kết quả kiểm tra cần cập nhật.");

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

            return (true, $"Cập nhật kết quả thành công! Xếp loại mới: {existing.Grade}");
        }

        public async Task<(bool Success, string Message)> DeleteExamRecordAsync(int id)
        {
            var existing = await _examRepository.GetByIdAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy kết quả kiểm tra cần xóa.");

            _examRepository.Delete(existing);
            await _examRepository.SaveChangesAsync();

            return (true, "Xóa kết quả kiểm tra thành công!");
        }
    }
}
