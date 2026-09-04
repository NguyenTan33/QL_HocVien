using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;

namespace QL_HocVien.Infrastructure.Validation
{
    /// <summary>
    /// Quy tắc kiểm duyệt an ninh và dữ liệu môn học / rèn luyện thể lực (Subject).
    /// </summary>
    public class SubjectValidationRule : IValidationRule<Subject>
    {
        private readonly ISecuritySanitizer _sanitizer;

        public SubjectValidationRule(ISecuritySanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public Task ValidateAsync(Subject subject, CancellationToken cancellationToken = default)
        {
            if (subject == null)
            {
                throw new ValidationException("Dữ liệu môn học không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(subject.SubjectCode))
                throw new ValidationException("Mã môn học không được để trống.", nameof(subject.SubjectCode));

            if (string.IsNullOrWhiteSpace(subject.SubjectName))
                throw new ValidationException("Tên môn học không được để trống.", nameof(subject.SubjectName));

            _sanitizer.EnsureSafeInput(subject.SubjectCode, "Mã môn học");
            _sanitizer.EnsureSafeInput(subject.SubjectName, "Tên môn học");
            _sanitizer.EnsureSafeInput(subject.Category, "Nhóm tố chất");
            _sanitizer.EnsureSafeInput(subject.Unit, "Đơn vị tính");
            _sanitizer.EnsureSafeInput(subject.Description, "Mô tả tiêu chuẩn");

            return Task.CompletedTask;
        }
    }
}
