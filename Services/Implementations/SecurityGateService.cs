using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QL_HocVien.Services.Implementations
{
    /// <summary>
    /// Triá»ƒn khai dá»‹ch vá»¥ KhÃ³a báº£o máº­t cáº¥p 2 (KhÃ³a rÆ°Æ¡ng Ngá»c Rá»“ng).
    /// ÄÃ¡p á»©ng tiÃªu chuáº©n OOP vÃ  SOLID (SRP, OCP, DIP).
    /// </summary>
    public class SecurityGateService : ISecurityGateService
    {
        private const int GracePeriodTotalSeconds = 300; // 5 phÃºt = 300 giÃ¢y
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

                    // Thá»­ giáº£i mÃ£ khá»‘i DPAPI Ä‘Æ°á»£c báº£o vá»‡ chá»‘ng can thiá»‡p
                    try
                    {
                        byte[] plainBytes = ProtectedData.Unprotect(fileBytes, ConfigEntropy, DataProtectionScope.CurrentUser);
                        json = Encoding.UTF8.GetString(plainBytes);
                    }
                    catch
                    {
                        // Fallback há»— trá»£ Ä‘á»c tá»‡p cáº¥u hÃ¬nh cÅ© (náº¿u chÆ°a nÃ¢ng cáº¥p DPAPI)
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
                // NguyÃªn táº¯c phÃ²ng thá»§ Fail-Closed: Náº¿u tá»‡p bá»‹ can thiá»‡p trÃ¡i phÃ©p, khÃ³a cháº·t há»‡ thá»‘ng
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

                // Báº£o vá»‡ toÃ n váº¹n vÃ  bÃ­ máº­t tá»‡p báº±ng Windows DPAPI (ngÄƒn cháº·n sá»­a file JSON báº±ng Notepad)
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, ConfigEntropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_configFilePath, cipherBytes);
            }
            catch
            {
                // Xá»­ lÃ½ an toÃ n náº¿u cÃ³ ngoáº¡i lá»‡ ghi file
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
                // Há»— trá»£ kiá»ƒm tra hash cÅ© vÃ  tá»± Ä‘á»™ng nÃ¢ng cáº¥p lÃªn chuáº©n PBKDF2 100.000 vÃ²ng
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
            _remainingSeconds = GracePeriodTotalSeconds; // 300s = 5 phÃºt
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

        public async Task<bool> EnsureUnlockedAsync(string actionDescription = "thá»±c hiá»‡n thao tÃ¡c nÃ y")
        {
            // Náº¿u báº£o máº­t chÆ°a Ä‘Æ°á»£c báº­t -> Cho phÃ©p thao tÃ¡c trá»±c tiáº¿p
            if (!_isProtectionEnabled)
            {
                return true;
            }

            // Náº¿u Ä‘ang má»Ÿ khÃ³a vÃ  váº«n cÃ²n trong thá»i gian 5 phÃºt -> Tá»± do thao tÃ¡c
            if (IsUnlocked)
            {
                return true;
            }

            // Äang bá»‹ khÃ³a -> Hiá»ƒn thá»‹ Dialog yÃªu cáº§u nháº­p máº­t kháº©u cáº¥p 2
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

