using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;

namespace QL_HocVien.Infrastructure.Validation
{
    /// <summary>
    /// Quy tắc kiểm duyệt an ninh và dữ liệu học viên (Cadet).
    /// Chống chèn mã độc, SQL Injection, XSS trong thông tin học viên.
    /// </summary>
    public class CadetValidationRule : IValidationRule<Cadet>
    {
        private readonly ISecuritySanitizer _sanitizer;

        public CadetValidationRule(ISecuritySanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public Task ValidateAsync(Cadet cadet, CancellationToken cancellationToken = default)
        {
            if (cadet == null)
            {
                throw new ValidationException("Hồ sơ học viên không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(cadet.CadetCode))
                throw new ValidationException("Mã học viên không được để trống.", nameof(cadet.CadetCode));

            if (string.IsNullOrWhiteSpace(cadet.FullName))
                throw new ValidationException("Họ và tên học viên không được để trống.", nameof(cadet.FullName));

            // Quét chống Injection
            _sanitizer.EnsureSafeInput(cadet.CadetCode, "Mã học viên");
            _sanitizer.EnsureSafeInput(cadet.FullName, "Họ và tên");
            _sanitizer.EnsureSafeInput(cadet.Rank, "Cấp bậc");
            _sanitizer.EnsureSafeInput(cadet.Unit, "Đơn vị");
            _sanitizer.EnsureSafeInput(cadet.ClassName, "Lớp học");
            _sanitizer.EnsureSafeInput(cadet.Position, "Chức vụ");
            _sanitizer.EnsureSafeInput(cadet.PhoneNumber, "Số điện thoại");
            _sanitizer.EnsureSafeInput(cadet.Email, "Email");

            return Task.CompletedTask;
        }
    }
}
