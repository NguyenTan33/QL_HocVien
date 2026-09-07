using System;

namespace QL_HocVien.Services
{
    public interface ILoginLockoutService
    {
        bool IsLockedOut { get; }
        int RemainingSeconds { get; }
        string FormattedRemainingTime { get; }
        int FailedAttempts { get; }
        string LockoutMessage { get; }
        event Action? OnLockoutStateChanged;

        /// <summary>
        /// Ghi nhận 1 lần đăng nhập thất bại.
        /// Trả về true nếu tài khoản/ứng dụng bị khóa.
        /// </summary>
        (bool IsLocked, int LockoutSeconds, string Message) RecordFailedAttempt();

        /// <summary>
        /// Ghi nhận đăng nhập thành công, reset toàn bộ bộ đếm về 0.
        /// </summary>
        void RecordSuccessfulLogin();
    }
}
