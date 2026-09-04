using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services
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
                return (false, "Mã môn học không được để trống.", null);

            if (string.IsNullOrWhiteSpace(subject.SubjectName))
                return (false, "Tên môn học không được để trống.", null);

            if (await _subjectRepository.ExistsByCodeAsync(subject.SubjectCode))
                return (false, $"Mã môn '{subject.SubjectCode}' đã tồn tại trong hệ thống.", null);

            await _subjectRepository.AddAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            return (true, "Thêm môn học mới thành công!", subject);
        }

        public async Task<(bool Success, string Message)> UpdateSubjectAsync(Subject subject)
        {
            if (string.IsNullOrWhiteSpace(subject.SubjectName))
                return (false, "Tên môn học không được để trống.");

            var existing = await _subjectRepository.GetByIdAsync(subject.Id);
            if (existing == null)
                return (false, "Không tìm thấy môn học cần cập nhật.");

            if (existing.SubjectCode != subject.SubjectCode)
            {
                if (await _subjectRepository.ExistsByCodeAsync(subject.SubjectCode))
                    return (false, $"Mã môn '{subject.SubjectCode}' đã được sử dụng.");
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

            return (true, "Cập nhật thông tin môn học thành công!");
        }

        public async Task<(bool Success, string Message)> DeleteSubjectAsync(int id)
        {
            var existing = await _subjectRepository.GetByIdAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy môn học cần xóa.");

            _subjectRepository.Delete(existing);
            await _subjectRepository.SaveChangesAsync();

            return (true, "Xóa môn học thành công!");
        }
    }
}
