using System;

namespace QL_HocVien.Models.Entity
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "CanBo"; // "Admin", "CanBo", "HocVien"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool HasPasskeyActivated { get; set; } = false;
        public string? ActivatedPasskey { get; set; }
        public DateTime? PasskeyActivatedAt { get; set; }

        // Khôi phục mật khẩu 100% Offline (Không dùng Email OTP / SMS)
        public string? SecurityQuestion { get; set; }
        public string? SecurityAnswerHash { get; set; }
        public string? PasswordHint { get; set; }
    }
}
