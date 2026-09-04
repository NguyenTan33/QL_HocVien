using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;

namespace QL_HocVien.Infrastructure.Validation
{
    /// <summary>
    /// Quy tắc kiểm duyệt an ninh và dữ liệu lớp học (MilitaryClass).
    /// </summary>
    public class ClassValidationRule : IValidationRule<MilitaryClass>
    {
        private readonly ISecuritySanitizer _sanitizer;

        public ClassValidationRule(ISecuritySanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public Task ValidateAsync(MilitaryClass militaryClass, CancellationToken cancellationToken = default)
        {
            if (militaryClass == null)
            {
                throw new ValidationException("Dữ liệu lớp học không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(militaryClass.ClassCode))
                throw new ValidationException("Mã lớp học không được để trống.", nameof(militaryClass.ClassCode));

            if (string.IsNullOrWhiteSpace(militaryClass.ClassName))
                throw new ValidationException("Tên lớp học không được để trống.", nameof(militaryClass.ClassName));

            _sanitizer.EnsureSafeInput(militaryClass.ClassCode, "Mã lớp học");
            _sanitizer.EnsureSafeInput(militaryClass.ClassName, "Tên lớp học");
            _sanitizer.EnsureSafeInput(militaryClass.Unit, "Đơn vị trực thuộc");
            _sanitizer.EnsureSafeInput(militaryClass.Major, "Chuyên ngành");
            _sanitizer.EnsureSafeInput(militaryClass.AcademicYear, "Niên khóa");
            _sanitizer.EnsureSafeInput(militaryClass.OfficerInCharge, "Cán bộ phụ trách");

            return Task.CompletedTask;
        }
    }
}
