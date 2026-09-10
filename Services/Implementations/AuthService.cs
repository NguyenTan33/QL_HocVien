using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICadetRepository _cadetRepository;
        private readonly AppDbContext _context;
        private readonly ISecuritySanitizer _sanitizer;

        public User? CurrentUser { get; private set; }

        public AuthService(
            IUserRepository userRepository,
            ICadetRepository cadetRepository,
            AppDbContext context,
            ISecuritySanitizer? sanitizer = null)
        {
            _userRepository = userRepository;
            _cadetRepository = cadetRepository;
            _context = context;
            _sanitizer = sanitizer ?? new SecuritySanitizer();
        }

        private const string GenericLoginErrorMessage = "Tài khoản hoặc mật khẩu không chính xác!";
        private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("DummySecretAuthSalt2026!#@", 11);

        public async Task<(bool Success, string Message, User? User)> LoginAsync(string usernameOrPhone, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrPhone) || string.IsNullOrWhiteSpace(password))
                return (false, GenericLoginErrorMessage, null);

            if (_sanitizer.ContainsDangerousPatterns(usernameOrPhone, out _))
                return (false, GenericLoginErrorMessage, null);

            var user = await _userRepository.GetByUsernameOrPhoneAsync(usernameOrPhone.Trim());
            if (user == null)
            {
                // Chống tấn công dò quét tài khoản qua thời gian phản hồi (Timing Attack)
                try
                {
                    BCrypt.Net.BCrypt.Verify(password, DummyHash);
                }
                catch { }

                return (false, GenericLoginErrorMessage, null);
            }

            if (!user.IsActive)
            {
                return (false, GenericLoginErrorMessage, null);
            }

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch
            {
                isPasswordValid = false;
            }

            if (!isPasswordValid)
            {
                return (false, GenericLoginErrorMessage, null);
            }

            user.LastLoginAt = DateTime.Now;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            CurrentUser = user;
            return (true, $"Đăng nhập thành công! Chào mừng {user.FullName}.", user);
        }

        public async Task<(bool Success, string Message)> RegisterAsync(
            string username,
            string fullName,
            string phoneNumber,
            string password,
            string securityQuestion,
            string securityAnswer,
            string? passwordHint = null,
            string? email = null)
        {
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
                return (false, "Tên tài khoản phải có ít nhất 3 ký tự.");

            if (_sanitizer.ContainsDangerousPatterns(username, out var t1))
                return (false, $"[BẢO MẬT] Tên tài khoản không an toàn: {t1}");

            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Vui lòng nhập họ và tên.");

            if (_sanitizer.ContainsDangerousPatterns(fullName, out var t2))
                return (false, $"[BẢO MẬT] Họ và tên không an toàn: {t2}");

            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Trim().Length < 9)
                return (false, "Số điện thoại không hợp lệ.");

            if (_sanitizer.ContainsDangerousPatterns(phoneNumber, out var t3))
                return (false, $"[BẢO MẬT] Số điện thoại không an toàn: {t3}");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, "Mật khẩu phải có ít nhất 6 ký tự.");

            if (string.IsNullOrWhiteSpace(securityQuestion))
                return (false, "Vui lòng chọn hoặc nhập câu hỏi bảo mật.");

            if (string.IsNullOrWhiteSpace(securityAnswer) || securityAnswer.Trim().Length < 2)
                return (false, "Câu trả lời bảo mật phải có ít nhất 2 ký tự.");

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!email.Contains("@") || !email.Contains("."))
                    return (false, "Địa chỉ email không hợp lệ.");

                if (_sanitizer.ContainsDangerousPatterns(email, out var t4))
                    return (false, $"[BẢO MẬT] Email không an toàn: {t4}");

                if (await _userRepository.ExistsByEmailAsync(email))
                    return (false, "Địa chỉ email này đã được sử dụng.");
            }

            // Kiểm tra trùng lặp
            if (await _userRepository.ExistsByUsernameAsync(username))
                return (false, "Tên tài khoản đã tồn tại trên hệ thống.");

            if (await _userRepository.ExistsByPhoneAsync(phoneNumber))
                return (false, "Số điện thoại này đã được đăng ký tài khoản khác.");

            var newUser = new User
            {
                Username = username.Trim(),
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? $"{username.Trim().ToLower()}@hocvien.local" : email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                SecurityQuestion = securityQuestion.Trim(),
                SecurityAnswerHash = AuthSecurityHelper.HashSecurityAnswer(securityAnswer),
                PasswordHint = string.IsNullOrWhiteSpace(passwordHint) ? null : passwordHint.Trim(),
                Role = "HocVien",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            return (true, "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.");
        }

        public async Task<(bool Success, string Message, string? PasswordHint, string? SecurityQuestion)> GetAccountRecoveryInfoAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return (false, "Vui lòng nhập Tên tài khoản hoặc Số điện thoại.", null, null);

            var trimmed = identifier.Trim();
            User? user = null;

            if (trimmed.Contains("@"))
            {
                user = await _userRepository.GetByEmailAsync(trimmed);
            }
            else
            {
                user = await _userRepository.GetByUsernameOrPhoneAsync(trimmed);
            }

            if (user == null)
            {
                return (false, "Không tìm thấy tài khoản tương ứng với thông tin đã nhập.", null, null);
            }

            if (string.IsNullOrWhiteSpace(user.SecurityQuestion))
            {
                return (false, "Tài khoản này chưa thiết lập câu hỏi bảo mật. Vui lòng liên hệ cán bộ quản trị để hỗ trợ cấp lại mật khẩu.", user.PasswordHint, null);
            }

            return (true, "Đã tìm thấy thông tin tài khoản.", user.PasswordHint, user.SecurityQuestion);
        }

        public async Task<(bool Success, string Message)> ResetPasswordWithSecurityAnswerAsync(string identifier, string securityAnswer, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return (false, "Vui lòng nhập Tên tài khoản hoặc Số điện thoại.");

            if (string.IsNullOrWhiteSpace(securityAnswer))
                return (false, "Vui lòng nhập câu trả lời bảo mật.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            var trimmed = identifier.Trim();
            User? user = null;

            if (trimmed.Contains("@"))
            {
                user = await _userRepository.GetByEmailAsync(trimmed);
            }
            else
            {
                user = await _userRepository.GetByUsernameOrPhoneAsync(trimmed);
            }

            if (user == null)
            {
                return (false, "Không tìm thấy tài khoản tương ứng.");
            }

            if (string.IsNullOrWhiteSpace(user.SecurityAnswerHash))
            {
                return (false, "Tài khoản chưa thiết lập câu trả lời bảo mật. Vui lòng liên hệ quản trị viên.");
            }

            if (!AuthSecurityHelper.VerifySecurityAnswer(securityAnswer, user.SecurityAnswerHash))
            {
                return (false, "Câu trả lời bảo mật không chính xác. Vui lòng thử lại.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return (true, "Đặt lại mật khẩu thành công! Đồng chí có thể đăng nhập bằng mật khẩu mới.");
        }

        public async Task<(bool Success, string Message)> ResetCadetPasswordAsync(int cadetId, string newPassword)
        {
            var cadet = await _cadetRepository.GetByIdAsync(cadetId);
            if (cadet == null)
                return (false, "Không tìm thấy học viên.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            User? user = null;
            if (cadet.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(cadet.UserId.Value);
            }

            if (user == null)
            {
                // Tìm theo số điện thoại hoặc tạo tài khoản mới cho học viên
                user = await _userRepository.GetByUsernameOrPhoneAsync(cadet.PhoneNumber);
                if (user == null)
                {
                    var username = cadet.CadetCode.ToLower().Replace("-", "");
                    if (await _userRepository.ExistsByUsernameAsync(username))
                    {
                        username = $"{username}_{cadet.Id}";
                    }

                    user = new User
                    {
                        Username = username,
                        FullName = cadet.FullName,
                        PhoneNumber = !string.IsNullOrWhiteSpace(cadet.PhoneNumber) ? cadet.PhoneNumber : $"090{cadet.Id:D7}",
                        Email = !string.IsNullOrWhiteSpace(cadet.Email) ? cadet.Email : $"{username}@hocvien.edu.vn",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword),
                        Role = "HocVien",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _userRepository.AddAsync(user);
                    await _userRepository.SaveChangesAsync();

                    cadet.UserId = user.Id;
                    _cadetRepository.Update(cadet);
                    await _cadetRepository.SaveChangesAsync();

                    return (true, $"Đã tạo tài khoản và đặt mật khẩu mới cho học viên: Tài khoản '{user.Username}'.");
                }
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return (true, $"Đã đặt lại mật khẩu thành công cho học viên {cadet.FullName} (Tài khoản: {user.Username}).");
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
