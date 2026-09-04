using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.DTOs;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;

namespace QL_HocVien.Infrastructure.Validation.Auth
{
    /// <summary>
    /// Quy tắc kiểm duyệt bảo mật thông tin Đăng nhập (Login).
    /// Chống SQL Injection, XSS và ký tự thực thi trong tên đăng nhập/sđt và mật khẩu.
    /// </summary>
    public class LoginValidationRule : IValidationRule<LoginValidationRequest>
    {
        private readonly ISecuritySanitizer _sanitizer;

        public LoginValidationRule(ISecuritySanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public Task ValidateAsync(LoginValidationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ValidationException("Dữ liệu đăng nhập không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(request.UsernameOrPhone))
            {
                throw new ValidationException("Tên đăng nhập hoặc số điện thoại không được để trống.", nameof(request.UsernameOrPhone));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ValidationException("Mật khẩu không được để trống.", nameof(request.Password));
            }

            // Kiểm tra Injection trong trường Tên đăng nhập / Số điện thoại
            _sanitizer.EnsureSafeInput(request.UsernameOrPhone, "Tên đăng nhập / Số điện thoại");

            // Kiểm tra độ dài
            if (request.UsernameOrPhone.Length > 100)
            {
                throw new ValidationException("Tên đăng nhập / Số điện thoại không được vượt quá 100 ký tự.");
            }

            if (request.Password.Length > 200)
            {
                throw new ValidationException("Mật khẩu không được vượt quá 200 ký tự.");
            }

            return Task.CompletedTask;
        }
    }
}
