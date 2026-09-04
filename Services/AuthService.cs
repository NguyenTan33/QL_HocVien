using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICadetRepository _cadetRepository;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISecuritySanitizer _sanitizer;

        public User? CurrentUser { get; private set; }

        public AuthService(
            IUserRepository userRepository,
            ICadetRepository cadetRepository,
            AppDbContext context,
            IEmailService emailService,
            ISecuritySanitizer? sanitizer = null)
        {
            _userRepository = userRepository;
            _cadetRepository = cadetRepository;
            _context = context;
            _emailService = emailService;
            _sanitizer = sanitizer ?? new SecuritySanitizer();
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(string usernameOrPhone, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrPhone))
                return (false, "Vui lòng nhập tên tài khoản hoặc số điện thoại.", null);

            if (_sanitizer.ContainsDangerousPatterns(usernameOrPhone, out var threat))
                return (false, $"[BẢO MẬT] Thông tin đăng nhập không hợp lệ: {threat}", null);

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Vui lòng nhập mật khẩu.", null);

            var user = await _userRepository.GetByUsernameOrPhoneAsync(usernameOrPhone);
            if (user == null)
            {
                return (false, "Tài khoản hoặc số điện thoại không tồn tại.", null);
            }

            if (!user.IsActive)
            {
                return (false, "Tài khoản này hiện đang bị tạm khóa.", null);
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
                return (false, "Mật khẩu không chính xác.", null);
            }

            user.LastLoginAt = DateTime.Now;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            CurrentUser = user;
            return (true, $"Đăng nhập thành công! Chào mừng {user.FullName}.", user);
        }

        public async Task<(bool Success, string Message)> RegisterAsync(
            string username, string fullName, string phoneNumber, string email, string password)
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

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
                return (false, "Địa chỉ email không hợp lệ.");

            if (_sanitizer.ContainsDangerousPatterns(email, out var t4))
                return (false, $"[BẢO MẬT] Email không an toàn: {t4}");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, "Mật khẩu phải có ít nhất 6 ký tự.");

            // Kiểm tra trùng lặp
            if (await _userRepository.ExistsByUsernameAsync(username))
                return (false, "Tên tài khoản đã tồn tại trên hệ thống.");

            if (await _userRepository.ExistsByPhoneAsync(phoneNumber))
                return (false, "Số điện thoại này đã được đăng ký tài khoản khác.");

            if (await _userRepository.ExistsByEmailAsync(email))
                return (false, "Địa chỉ email này đã được sử dụng.");

            var newUser = new User
            {
                Username = username.Trim(),
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                Email = email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "HocVien",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            return (true, "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.");
        }

        public async Task<(bool Success, string Message, string? Otp)> RequestPasswordResetOtpAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return (false, "Vui lòng nhập Email, Tên tài khoản hoặc Số điện thoại.", null);

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

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return (false, "Không tìm thấy tài khoản tương ứng với thông tin đã nhập.", null);
            }

            // Sinh mã OTP ngẫu nhiên 6 chữ số
            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Vô hiệu hoá các token cũ chưa dùng của email này
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.Email == user.Email && !t.IsUsed)
                .ToListAsync();
            foreach (var token in oldTokens)
            {
                token.IsUsed = true;
            }

            // Lưu token mới với hạn 10 phút
            var resetToken = new PasswordResetToken
            {
                Email = user.Email,
                Token = otp,
                ExpiryTime = DateTime.Now.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            // Gửi email
            var emailResult = await _emailService.SendOtpEmailAsync(user.Email, otp, user.FullName);

            return (true, emailResult.Message, otp);
        }

        public async Task<(bool Success, string Message)> ResetPasswordWithOtpAsync(string email, string otpCode, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Vui lòng nhập email xác nhận.");

            if (string.IsNullOrWhiteSpace(otpCode))
                return (false, "Vui lòng nhập mã xác thực OTP.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            var token = await _context.PasswordResetTokens
                .Where(t => t.Email.ToLower() == email.Trim().ToLower() && t.Token == otpCode.Trim() && !t.IsUsed)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (token == null)
            {
                return (false, "Mã xác thực không chính xác.");
            }

            if (token.ExpiryTime < DateTime.Now)
            {
                return (false, "Mã xác thực đã hết hạn (chỉ có hiệu lực trong 10 phút). Vui lòng yêu cầu mã mới.");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return (false, "Không tìm thấy người dùng có email này.");
            }

            // Cập nhật mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            token.IsUsed = true;

            _userRepository.Update(user);
            await _context.SaveChangesAsync();

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
