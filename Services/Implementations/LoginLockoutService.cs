using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;

namespace QL_HocVien.Services.Implementations
{
    public class LoginLockoutService : ILoginLockoutService
    {
        private const int ThresholdAttempts = 5;
        private const int BaseLockoutSeconds = 60; // 1 phÃºt = 60 giÃ¢y
        private const int MaxLockoutSeconds = 3600; // KhÃ³a tá»‘i Ä‘a 60 phÃºt
        private const string StateFileName = "lockout_state.json";
        private static readonly byte[] LockoutEntropy = Encoding.UTF8.GetBytes("MOD_Lockout_State_Entropy_2026!#");

        private readonly string _stateFilePath;
        private readonly DispatcherTimer _timer;
        private readonly object _lock = new();

        private int _failedAttempts = 0;
        private DateTime? _lockoutUntilUtc = null;
        private int _remainingSeconds = 0;

        public event Action? OnLockoutStateChanged;

        public bool IsLockedOut
        {
            get
            {
                lock (_lock)
                {
                    if (!_lockoutUntilUtc.HasValue) return false;
                    return DateTime.UtcNow < _lockoutUntilUtc.Value;
                }
            }
        }

        public int RemainingSeconds
        {
            get
            {
                lock (_lock)
                {
                    return _remainingSeconds;
                }
            }
        }

        public int FailedAttempts
        {
            get
            {
                lock (_lock)
                {
                    return _failedAttempts;
                }
            }
        }

        public string FormattedRemainingTime
        {
            get
            {
                int secs = RemainingSeconds;
                if (secs <= 0) return "00:00";
                int m = secs / 60;
                int s = secs % 60;
                return $"{m:D2}:{s:D2}";
            }
        }

        public string LockoutMessage
        {
            get
            {
                if (!IsLockedOut) return string.Empty;
                return $"Báº¡n Ä‘Ã£ nháº­p sai {FailedAttempts} láº§n. Há»‡ thá»‘ng táº¡m thá»i bá»‹ khÃ³a trong {FormattedRemainingTime}.";
            }
        }

        public LoginLockoutService()
        {
            _stateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StateFileName);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            LoadState();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            bool stateChanged = false;
            lock (_lock)
            {
                if (_lockoutUntilUtc.HasValue)
                {
                    var diff = (int)Math.Ceiling((_lockoutUntilUtc.Value - DateTime.UtcNow).TotalSeconds);
                    if (diff > 0)
                    {
                        _remainingSeconds = diff;
                        stateChanged = true;
                    }
                    else
                    {
                        _remainingSeconds = 0;
                        _lockoutUntilUtc = null;
                        _timer.Stop();
                        SaveState();
                        stateChanged = true;
                    }
                }
                else
                {
                    _remainingSeconds = 0;
                    _timer.Stop();
                }
            }

            if (stateChanged)
            {
                OnLockoutStateChanged?.Invoke();
            }
        }

        public (bool IsLocked, int LockoutSeconds, string Message) RecordFailedAttempt()
        {
            lock (_lock)
            {
                _failedAttempts++;

                if (_failedAttempts >= ThresholdAttempts)
                {
                    // TÃ­nh thá»i gian khÃ³a theo cáº¥p sá»‘ nhÃ¢n:
                    // Láº§n 5: 60s (1 phÃºt)
                    // Láº§n 6: 120s (2 phÃºt)
                    // Láº§n 7: 240s (4 phÃºt)
                    // Láº§n 8: 480s (8 phÃºt)...
                    int exponent = _failedAttempts - ThresholdAttempts;
                    int lockoutSeconds = BaseLockoutSeconds * (int)Math.Pow(2, Math.Min(exponent, 10));
                    if (lockoutSeconds > MaxLockoutSeconds) lockoutSeconds = MaxLockoutSeconds;

                    _lockoutUntilUtc = DateTime.UtcNow.AddSeconds(lockoutSeconds);
                    _remainingSeconds = lockoutSeconds;

                    _timer.Stop();
                    _timer.Start();

                    SaveState();
                    OnLockoutStateChanged?.Invoke();

                    return (true, lockoutSeconds, LockoutMessage);
                }
                else
                {
                    SaveState();
                    return (false, 0, "TÃ i khoáº£n hoáº·c máº­t kháº©u khÃ´ng chÃ­nh xÃ¡c!");
                }
            }
        }

        public void RecordSuccessfulLogin()
        {
            lock (_lock)
            {
                _failedAttempts = 0;
                _lockoutUntilUtc = null;
                _remainingSeconds = 0;
                _timer.Stop();

                SaveState();
                OnLockoutStateChanged?.Invoke();
            }
        }

        private void LoadState()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_stateFilePath))
                    {
                        byte[] fileBytes = File.ReadAllBytes(_stateFilePath);
                        string json;

                        try
                        {
                            byte[] decrypted = ProtectedData.Unprotect(fileBytes, LockoutEntropy, DataProtectionScope.CurrentUser);
                            json = Encoding.UTF8.GetString(decrypted);
                        }
                        catch
                        {
                            // Há»— trá»£ Ä‘á»c file cáº¥u trÃºc cÅ© (náº¿u chÆ°a mÃ£ hÃ³a)
                            json = Encoding.UTF8.GetString(fileBytes);
                        }

                        var data = JsonSerializer.Deserialize<LockoutStateDto>(json);
                        if (data != null)
                        {
                            _failedAttempts = data.FailedAttempts;
                            if (data.LockoutUntilUtc.HasValue && data.LockoutUntilUtc.Value > DateTime.UtcNow)
                            {
                                _lockoutUntilUtc = data.LockoutUntilUtc.Value;
                                _remainingSeconds = (int)Math.Ceiling((_lockoutUntilUtc.Value - DateTime.UtcNow).TotalSeconds);
                                _timer.Start();
                            }
                            else
                            {
                                _lockoutUntilUtc = null;
                                _remainingSeconds = 0;
                            }
                        }
                    }
                }
                catch
                {
                    // Náº¿u phÃ¡t hiá»‡n file bá»‹ can thiá»‡p lá»—i cáº¥u trÃºc, khÃ´ng reset náº¿u Ä‘ang trong bá»™ nhá»›
                    if (!_lockoutUntilUtc.HasValue)
                    {
                        _failedAttempts = 0;
                        _lockoutUntilUtc = null;
                        _remainingSeconds = 0;
                    }
                }
            }
        }

        private void SaveState()
        {
            try
            {
                var data = new LockoutStateDto
                {
                    FailedAttempts = _failedAttempts,
                    LockoutUntilUtc = _lockoutUntilUtc
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                byte[] plainBytes = Encoding.UTF8.GetBytes(json);

                // MÃ£ hÃ³a báº£o vá»‡ toÃ n váº¹n báº±ng Windows DPAPI
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, LockoutEntropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_stateFilePath, cipherBytes);
            }
            catch
            {
                // Bá» qua lá»—i ghi file
            }
        }

        private class LockoutStateDto
        {
            public int FailedAttempts { get; set; }
            public DateTime? LockoutUntilUtc { get; set; }
        }
    }
}

