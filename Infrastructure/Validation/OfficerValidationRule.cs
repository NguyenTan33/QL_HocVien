using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;

namespace QL_HocVien.Infrastructure.Validation
{
    /// <summary>
    /// Quy tắc kiểm duyệt an ninh và dữ liệu cán bộ sĩ quan (Officer).
    /// </summary>
    public class OfficerValidationRule : IValidationRule<Officer>
    {
        private readonly ISecuritySanitizer _sanitizer;

        public OfficerValidationRule(ISecuritySanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public Task ValidateAsync(Officer officer, CancellationToken cancellationToken = default)
        {
            if (officer == null)
            {
                throw new ValidationException("Hồ sơ cán bộ không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(officer.OfficerCode))
                throw new ValidationException("Mã cán bộ không được để trống.", nameof(officer.OfficerCode));

            if (string.IsNullOrWhiteSpace(officer.FullName))
                throw new ValidationException("Họ và tên cán bộ không được để trống.", nameof(officer.FullName));

            _sanitizer.EnsureSafeInput(officer.OfficerCode, "Mã cán bộ");
            _sanitizer.EnsureSafeInput(officer.FullName, "Họ và tên");
            _sanitizer.EnsureSafeInput(officer.Rank, "Cấp bậc");
            _sanitizer.EnsureSafeInput(officer.Position, "Chức vụ");
            _sanitizer.EnsureSafeInput(officer.Unit, "Đơn vị");
            _sanitizer.EnsureSafeInput(officer.Specialty, "Chuyên môn");
            _sanitizer.EnsureSafeInput(officer.PhoneNumber, "Số điện thoại");
            _sanitizer.EnsureSafeInput(officer.Email, "Email");

            return Task.CompletedTask;
        }
    }
}
