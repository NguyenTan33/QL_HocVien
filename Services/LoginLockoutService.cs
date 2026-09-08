using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;

namespace QL_HocVien.Services
{
    public class LoginLockoutService : ILoginLockoutService
    {
        private const int ThresholdAttempts = 5;
        private const int BaseLockoutSeconds = 60; // 1 phút = 60 giây
        private const int MaxLockoutSeconds = 3600; // Khóa tối đa 60 phút
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
                return $"Bạn đã nhập sai {FailedAttempts} lần. Hệ thống tạm thời bị khóa trong {FormattedRemainingTime}.";
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
                    // Tính thời gian khóa theo cấp số nhân:
                    // Lần 5: 60s (1 phút)
                    // Lần 6: 120s (2 phút)
                    // Lần 7: 240s (4 phút)
                    // Lần 8: 480s (8 phút)...
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
                    return (false, 0, "Tài khoản hoặc mật khẩu không chính xác!");
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
                            // Hỗ trợ đọc file cấu trúc cũ (nếu chưa mã hóa)
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
                    // Nếu phát hiện file bị can thiệp lỗi cấu trúc, không reset nếu đang trong bộ nhớ
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

                // Mã hóa bảo vệ toàn vẹn bằng Windows DPAPI
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, LockoutEntropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_stateFilePath, cipherBytes);
            }
            catch
            {
                // Bỏ qua lỗi ghi file
            }
        }

        private class LockoutStateDto
        {
            public int FailedAttempts { get; set; }
            public DateTime? LockoutUntilUtc { get; set; }
        }
    }
}
