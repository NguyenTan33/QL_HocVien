using System;

namespace QL_HocVien.Services.Interfaces
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
        /// Ghi nháº­n 1 láº§n Ä‘Äƒng nháº­p tháº¥t báº¡i.
        /// Tráº£ vá» true náº¿u tÃ i khoáº£n/á»©ng dá»¥ng bá»‹ khÃ³a.
        /// </summary>
        (bool IsLocked, int LockoutSeconds, string Message) RecordFailedAttempt();

        /// <summary>
        /// Ghi nháº­n Ä‘Äƒng nháº­p thÃ nh cÃ´ng, reset toÃ n bá»™ bá»™ Ä‘áº¿m vá» 0.
        /// </summary>
        void RecordSuccessfulLogin();
    }
}

