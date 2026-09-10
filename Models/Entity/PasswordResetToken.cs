using System;

namespace QL_HocVien.Models.Entity
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty; // MÃ£ OTP 6 chá»¯ sá»‘
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; } = false;
        public int AttemptCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

