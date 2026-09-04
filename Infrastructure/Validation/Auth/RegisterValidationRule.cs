using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.DTOs;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;

namespace QL_HocVien.Infrastructure.Validation.Auth
{
    /// <summary>
    /// Quy tắc kiểm duyệt an ninh thông tin Đăng ký tài khoản (Register).
    /// </summary>
    public class RegisterValidationRule : IValidationRule<RegisterValidationRequest>
    {
        private readonly ISecuritySanitizer _sanitizer;

        public RegisterValidationRule(ISecuritySanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public Task ValidateAsync(RegisterValidationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ValidationException("Dữ liệu đăng ký không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ValidationException("Tên tài khoản không được để trống.", nameof(request.Username));

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ValidationException("Mật khẩu không được để trống.", nameof(request.Password));

            if (request.Password != request.ConfirmPassword)
                throw new ValidationException("Mật khẩu xác nhận không khớp.", nameof(request.ConfirmPassword));

            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ValidationException("Họ và tên không được để trống.", nameof(request.FullName));

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new ValidationException("Số điện thoại không được để trống.", nameof(request.PhoneNumber));

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ValidationException("Email không được để trống.", nameof(request.Email));

            // Quét chống Injection
            _sanitizer.EnsureSafeInput(request.Username, "Tên tài khoản");
            _sanitizer.EnsureSafeInput(request.FullName, "Họ và tên");
            _sanitizer.EnsureSafeInput(request.PhoneNumber, "Số điện thoại");
            _sanitizer.EnsureSafeInput(request.Email, "Email");

            // Ràng buộc định dạng Username (chữ và số, dấu gạch dưới, không chứa ký tự đặc biệt nguy hiểm)
            if (!Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_\.\-]{3,50}$"))
            {
                throw new ValidationException("Tên tài khoản chỉ được chứa chữ cái, chữ số, dấu chấm, gạch dưới và có độ dài từ 3-50 ký tự.");
            }

            // Ràng buộc định dạng Số điện thoại Việt Nam
            if (!Regex.IsMatch(request.PhoneNumber.Trim(), @"^(0|\+84)[0-9]{9,10}$"))
            {
                throw new ValidationException("Số điện thoại không đúng định dạng tiêu chuẩn (10-11 số bắt đầu bằng 0 hoặc +84).");
            }

            // Ràng buộc định dạng Email
            if (!Regex.IsMatch(request.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ValidationException("Địa chỉ email không đúng định dạng.");
            }

            return Task.CompletedTask;
        }
    }
}
