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

        private const string GenericLoginErrorMessage = "TÃ i khoáº£n hoáº·c máº­t kháº©u khÃ´ng chÃ­nh xÃ¡c!";
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
                // Chá»‘ng táº¥n cÃ´ng dÃ² quÃ©t tÃ i khoáº£n qua thá»i gian pháº£n há»“i (Timing Attack)
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
            return (true, $"ÄÄƒng nháº­p thÃ nh cÃ´ng! ChÃ o má»«ng {user.FullName}.", user);
        }

        public async Task<(bool Success, string Message)> RegisterAsync(
            string username, string fullName, string phoneNumber, string email, string password)
        {
            // Kiá»ƒm tra dá»¯ liá»‡u Ä‘áº§u vÃ o
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
                return (false, "TÃªn tÃ i khoáº£n pháº£i cÃ³ Ã­t nháº¥t 3 kÃ½ tá»±.");

            if (_sanitizer.ContainsDangerousPatterns(username, out var t1))
                return (false, $"[Báº¢O Máº¬T] TÃªn tÃ i khoáº£n khÃ´ng an toÃ n: {t1}");

            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Vui lÃ²ng nháº­p há» vÃ  tÃªn.");

            if (_sanitizer.ContainsDangerousPatterns(fullName, out var t2))
                return (false, $"[Báº¢O Máº¬T] Há» vÃ  tÃªn khÃ´ng an toÃ n: {t2}");

            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Trim().Length < 9)
                return (false, "Sá»‘ Ä‘iá»‡n thoáº¡i khÃ´ng há»£p lá»‡.");

            if (_sanitizer.ContainsDangerousPatterns(phoneNumber, out var t3))
                return (false, $"[Báº¢O Máº¬T] Sá»‘ Ä‘iá»‡n thoáº¡i khÃ´ng an toÃ n: {t3}");

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
                return (false, "Äá»‹a chá»‰ email khÃ´ng há»£p lá»‡.");

            if (_sanitizer.ContainsDangerousPatterns(email, out var t4))
                return (false, $"[Báº¢O Máº¬T] Email khÃ´ng an toÃ n: {t4}");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, "Máº­t kháº©u pháº£i cÃ³ Ã­t nháº¥t 6 kÃ½ tá»±.");

            // Kiá»ƒm tra trÃ¹ng láº·p
            if (await _userRepository.ExistsByUsernameAsync(username))
                return (false, "TÃªn tÃ i khoáº£n Ä‘Ã£ tá»“n táº¡i trÃªn há»‡ thá»‘ng.");

            if (await _userRepository.ExistsByPhoneAsync(phoneNumber))
                return (false, "Sá»‘ Ä‘iá»‡n thoáº¡i nÃ y Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½ tÃ i khoáº£n khÃ¡c.");

            if (await _userRepository.ExistsByEmailAsync(email))
                return (false, "Äá»‹a chá»‰ email nÃ y Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng.");

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

            return (true, "ÄÄƒng kÃ½ tÃ i khoáº£n thÃ nh cÃ´ng! Báº¡n cÃ³ thá»ƒ Ä‘Äƒng nháº­p ngay bÃ¢y giá».");
        }

        public async Task<(bool Success, string Message, string? Otp)> RequestPasswordResetOtpAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return (false, "Vui lÃ²ng nháº­p Email, TÃªn tÃ i khoáº£n hoáº·c Sá»‘ Ä‘iá»‡n thoáº¡i.", null);

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
                return (false, "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n tÆ°Æ¡ng á»©ng vá»›i thÃ´ng tin Ä‘Ã£ nháº­p.", null);
            }

            // Chá»‘ng Spam / Táº¥n cÃ´ng DoS OTP: Giá»›i háº¡n tá»‘i thiá»ƒu 60 giÃ¢y giá»¯a 2 láº§n yÃªu cáº§u
            var lastRecentToken = await _context.PasswordResetTokens
                .Where(t => t.Email.ToLower() == user.Email.ToLower())
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastRecentToken != null)
            {
                var elapsed = (DateTime.Now - lastRecentToken.CreatedAt).TotalSeconds;
                if (elapsed < 60)
                {
                    int waitSec = 60 - (int)elapsed;
                    return (false, $"YÃªu cáº§u OTP quÃ¡ nhanh. Vui lÃ²ng Ä‘á»£i {waitSec} giÃ¢y trÆ°á»›c khi gá»­i láº¡i.", null);
                }
            }

            // Sinh mÃ£ OTP ngáº«u nhiÃªn 6 chá»¯ sá»‘
            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // VÃ´ hiá»‡u hoÃ¡ cÃ¡c token cÅ© chÆ°a dÃ¹ng cá»§a email nÃ y
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.Email == user.Email && !t.IsUsed)
                .ToListAsync();
            foreach (var token in oldTokens)
            {
                token.IsUsed = true;
            }

            // LÆ°u token má»›i vá»›i háº¡n 10 phÃºt
            var resetToken = new PasswordResetToken
            {
                Email = user.Email,
                Token = otp,
                ExpiryTime = DateTime.Now.AddMinutes(10),
                IsUsed = false,
                AttemptCount = 0,
                CreatedAt = DateTime.Now
            };

            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            // Gá»­i email
            var emailResult = await _emailService.SendOtpEmailAsync(user.Email, otp, user.FullName);
            if (!emailResult.Success)
            {
                return (false, emailResult.Message, null);
            }

            return (true, emailResult.Message, otp);
        }

        public async Task<(bool Success, string Message)> ResetPasswordWithOtpAsync(string emailOrIdentifier, string otpCode, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(emailOrIdentifier))
                return (false, "Vui lÃ²ng nháº­p email hoáº·c tÃ i khoáº£n xÃ¡c nháº­n.");

            if (string.IsNullOrWhiteSpace(otpCode))
                return (false, "Vui lÃ²ng nháº­p mÃ£ xÃ¡c thá»±c OTP.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Máº­t kháº©u má»›i pháº£i cÃ³ Ã­t nháº¥t 6 kÃ½ tá»±.");

            string cleanId = emailOrIdentifier.Trim();
            User? user = null;
            string targetEmail = cleanId;

            if (cleanId.Contains("@"))
            {
                user = await _userRepository.GetByEmailAsync(cleanId);
                if (user != null) targetEmail = user.Email;
            }
            else
            {
                user = await _userRepository.GetByUsernameOrPhoneAsync(cleanId);
                if (user != null) targetEmail = user.Email;
            }

            var token = await _context.PasswordResetTokens
                .Where(t => t.Email.ToLower() == targetEmail.ToLower() && !t.IsUsed)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (token == null)
            {
                return (false, "KhÃ´ng tÃ¬m tháº¥y yÃªu cáº§u xÃ¡c thá»±c OTP cÃ²n hiá»‡u lá»±c. Vui lÃ²ng yÃªu cáº§u mÃ£ má»›i.");
            }

            if (token.ExpiryTime < DateTime.Now)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
                return (false, "MÃ£ xÃ¡c thá»±c Ä‘Ã£ háº¿t háº¡n (chá»‰ cÃ³ hiá»‡u lá»±c trong 10 phÃºt). Vui lÃ²ng yÃªu cáº§u mÃ£ má»›i.");
            }

            // Chá»‘ng táº¥n cÃ´ng vÃ©t cáº¡n OTP (Brute-force): Giá»›i háº¡n tá»‘i Ä‘a 5 láº§n nháº­p sai
            if (!string.Equals(token.Token.Trim(), otpCode.Trim(), StringComparison.Ordinal))
            {
                token.AttemptCount++;
                if (token.AttemptCount >= 5)
                {
                    token.IsUsed = true;
                    await _context.SaveChangesAsync();
                    return (false, "MÃ£ xÃ¡c thá»±c Ä‘Ã£ bá»‹ há»§y do nháº­p sai quÃ¡ 5 láº§n liÃªn tiáº¿p Ä‘á»ƒ báº£o Ä‘áº£m an ninh.");
                }

                await _context.SaveChangesAsync();
                return (false, $"MÃ£ xÃ¡c thá»±c khÃ´ng chÃ­nh xÃ¡c! Äá»“ng chÃ­ cÃ²n {5 - token.AttemptCount} láº§n thá»­.");
            }

            if (user == null)
            {
                user = await _userRepository.GetByEmailAsync(token.Email);
            }

            if (user == null)
            {
                return (false, "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng cÃ³ thÃ´ng tin nÃ y.");
            }

            // Cáº­p nháº­t máº­t kháº©u
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            token.IsUsed = true;

            _userRepository.Update(user);
            await _context.SaveChangesAsync();

            return (true, "Äáº·t láº¡i máº­t kháº©u thÃ nh cÃ´ng! Äá»“ng chÃ­ cÃ³ thá»ƒ Ä‘Äƒng nháº­p báº±ng máº­t kháº©u má»›i.");
        }

        public async Task<(bool Success, string Message)> ResetCadetPasswordAsync(int cadetId, string newPassword)
        {
            var cadet = await _cadetRepository.GetByIdAsync(cadetId);
            if (cadet == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y há»c viÃªn.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Máº­t kháº©u má»›i pháº£i cÃ³ Ã­t nháº¥t 6 kÃ½ tá»±.");

            User? user = null;
            if (cadet.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(cadet.UserId.Value);
            }

            if (user == null)
            {
                // TÃ¬m theo sá»‘ Ä‘iá»‡n thoáº¡i hoáº·c táº¡o tÃ i khoáº£n má»›i cho há»c viÃªn
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

                    return (true, $"ÄÃ£ táº¡o tÃ i khoáº£n vÃ  Ä‘áº·t máº­t kháº©u má»›i cho há»c viÃªn: TÃ i khoáº£n '{user.Username}'.");
                }
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return (true, $"ÄÃ£ Ä‘áº·t láº¡i máº­t kháº©u thÃ nh cÃ´ng cho há»c viÃªn {cadet.FullName} (TÃ i khoáº£n: {user.Username}).");
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}

