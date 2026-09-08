using System;
using System.Threading.Tasks;
using QL_HocVien.Services;

namespace QL_HocVien.Tests.TestDoubles
{
    public class FakeSecurityGateService : ISecurityGateService
    {
        public bool IsProtectionEnabled => false;
        public bool IsUnlocked => true;
        public int RemainingSeconds => 300;
        public string FormattedRemainingTime => "05:00";

        public event Action? OnSecurityStateChanged;

        public bool EnableProtection(string password) => true;
        public bool DisableProtection(string currentPassword) => true;
        public bool ChangePassword(string oldPassword, string newPassword) => true;
        public bool VerifyPassword(string password) => true;
        public void UnlockForGracePeriod() { }
        public void LockNow() { }
        public Task<bool> EnsureUnlockedAsync(string actionDescription = "thực hiện thao tác này") => Task.FromResult(true);
    }
}
