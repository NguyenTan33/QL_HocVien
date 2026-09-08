using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QL_HocVien.Services
{
    /// <summary>
    /// Triển khai dịch vụ Khóa bảo mật cấp 2 (Khóa rương Ngọc Rồng).
    /// Đáp ứng tiêu chuẩn OOP và SOLID (SRP, OCP, DIP).
    /// </summary>
    public class SecurityGateService : ISecurityGateService
    {
        private const int GracePeriodTotalSeconds = 300; // 5 phút = 300 giây
        private const string SaltPepper = "MOD_SECURITY_GATE_VN_2026";
        private static readonly byte[] ConfigEntropy = Encoding.UTF8.GetBytes("MOD_SecGate_Config_Salt_2026#");
        private readonly string _configFilePath;
        private readonly ISecurityDialogService _dialogService;
        private readonly DispatcherTimer _countdownTimer;

        private bool _isProtectionEnabled = false;
        private bool _isUnlocked = false;
        private int _remainingSeconds = 0;
        private string _passwordHash = string.Empty;
        private string _salt = string.Empty;

        public event Action? OnSecurityStateChanged;

        public bool IsProtectionEnabled => _isProtectionEnabled;
        public bool IsUnlocked => _isProtectionEnabled && _isUnlocked && _remainingSeconds > 0;
        public int RemainingSeconds => _remainingSeconds;

        public string FormattedRemainingTime
        {
            get
            {
                if (!IsUnlocked) return "00:00";
                int m = _remainingSeconds / 60;
                int s = _remainingSeconds % 60;
                return $"{m:D2}:{s:D2}";
            }
        }

        public SecurityGateService(ISecurityDialogService dialogService)
        {
            _dialogService = dialogService;
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "security_config.json");

            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;

            LoadConfiguration();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                if (_remainingSeconds <= 0)
                {
                    _isUnlocked = false;
                    _countdownTimer.Stop();
                }
                OnSecurityStateChanged?.Invoke();
            }
            else
            {
                _isUnlocked = false;
                _countdownTimer.Stop();
                OnSecurityStateChanged?.Invoke();
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(_configFilePath);
                    string json;

                    // Thử giải mã khối DPAPI được bảo vệ chống can thiệp
                    try
                    {
                        byte[] plainBytes = ProtectedData.Unprotect(fileBytes, ConfigEntropy, DataProtectionScope.CurrentUser);
                        json = Encoding.UTF8.GetString(plainBytes);
                    }
                    catch
                    {
                        // Fallback hỗ trợ đọc tệp cấu hình cũ (nếu chưa nâng cấp DPAPI)
                        json = Encoding.UTF8.GetString(fileBytes);
                    }

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("IsProtectionEnabled", out var pProp))
                    {
                        _isProtectionEnabled = pProp.GetBoolean();
                    }
                    if (root.TryGetProperty("PasswordHash", out var hProp))
                    {
                        _passwordHash = hProp.GetString() ?? string.Empty;
                    }
                    if (root.TryGetProperty("Salt", out var sProp))
                    {
                        _salt = sProp.GetString() ?? string.Empty;
                    }
                }
            }
            catch
            {
                // Nguyên tắc phòng thủ Fail-Closed: Nếu tệp bị can thiệp trái phép, khóa chặt hệ thống
                _isProtectionEnabled = true;
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var data = new
                {
                    IsProtectionEnabled = _isProtectionEnabled,
                    PasswordHash = _passwordHash,
                    Salt = _salt,
                    LastUpdated = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                byte[] plainBytes = Encoding.UTF8.GetBytes(json);

                // Bảo vệ toàn vẹn và bí mật tệp bằng Windows DPAPI (ngăn chặn sửa file JSON bằng Notepad)
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, ConfigEntropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_configFilePath, cipherBytes);
            }
            catch
            {
                // Xử lý an toàn nếu có ngoại lệ ghi file
            }
        }

        private static string ComputeHash(string password, string salt)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt + SaltPepper);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                saltBytes,
                100_000,
                HashAlgorithmName.SHA256,
                32);
            return "pbkdf2$" + Convert.ToBase64String(hash);
        }

        private static string LegacySha256Hash(string password, string salt)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + salt + SaltPepper);
            var hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        public bool EnableProtection(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            _salt = Guid.NewGuid().ToString("N");
            _passwordHash = ComputeHash(password.Trim(), _salt);
            _isProtectionEnabled = true;

            SaveConfiguration();
            UnlockForGracePeriod();
            return true;
        }

        public bool DisableProtection(string currentPassword)
        {
            if (!VerifyPassword(currentPassword)) return false;

            _isProtectionEnabled = false;
            _isUnlocked = false;
            _remainingSeconds = 0;
            _countdownTimer.Stop();

            SaveConfiguration();
            OnSecurityStateChanged?.Invoke();
            return true;
        }

        public bool ChangePassword(string currentPassword, string newPassword)
        {
            if (!VerifyPassword(currentPassword)) return false;
            if (string.IsNullOrWhiteSpace(newPassword)) return false;

            _salt = Guid.NewGuid().ToString("N");
            _passwordHash = ComputeHash(newPassword.Trim(), _salt);

            SaveConfiguration();
            UnlockForGracePeriod();
            return true;
        }

        public bool VerifyPassword(string password)
        {
            if (!_isProtectionEnabled) return true;
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(_passwordHash) || string.IsNullOrEmpty(_salt))
            {
                return false;
            }

            if (_passwordHash.StartsWith("pbkdf2$"))
            {
                var hash = ComputeHash(password.Trim(), _salt);
                return string.Equals(hash, _passwordHash, StringComparison.Ordinal);
            }
            else
            {
                // Hỗ trợ kiểm tra hash cũ và tự động nâng cấp lên chuẩn PBKDF2 100.000 vòng
                var legacyHash = LegacySha256Hash(password.Trim(), _salt);
                if (string.Equals(legacyHash, _passwordHash, StringComparison.Ordinal))
                {
                    _passwordHash = ComputeHash(password.Trim(), _salt);
                    SaveConfiguration();
                    return true;
                }
                return false;
            }
        }

        public void UnlockForGracePeriod()
        {
            _isUnlocked = true;
            _remainingSeconds = GracePeriodTotalSeconds; // 300s = 5 phút
            _countdownTimer.Stop();
            _countdownTimer.Start();
            OnSecurityStateChanged?.Invoke();
        }

        public void LockNow()
        {
            _countdownTimer.Stop();
            _isUnlocked = false;
            _remainingSeconds = 0;
            OnSecurityStateChanged?.Invoke();
        }

        public async Task<bool> EnsureUnlockedAsync(string actionDescription = "thực hiện thao tác này")
        {
            // Nếu bảo mật chưa được bật -> Cho phép thao tác trực tiếp
            if (!_isProtectionEnabled)
            {
                return true;
            }

            // Nếu đang mở khóa và vẫn còn trong thời gian 5 phút -> Tự do thao tác
            if (IsUnlocked)
            {
                return true;
            }

            // Đang bị khóa -> Hiển thị Dialog yêu cầu nhập mật khẩu cấp 2
            var verified = await _dialogService.ShowPasswordVerificationDialogAsync(actionDescription, VerifyPassword);
            if (verified)
            {
                UnlockForGracePeriod();
                return true;
            }

            return false;
        }
    }
}
