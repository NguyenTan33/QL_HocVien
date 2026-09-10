using System;

namespace QL_HocVien.Models
{
    public class AccountPasskey
    {
        public int Id { get; set; }

        /// <summary>
        /// Mã Passkey bản quyền (Ví dụ: QD-2026-HQ88-K01A)
        /// </summary>
        public string Passkey { get; set; } = string.Empty;

        /// <summary>
        /// Đã được kích hoạt hay chưa
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Tài khoản sở hữu Passkey này (Khóa 1-1 với Username)
        /// </summary>
        public string? UsedByUsername { get; set; }

        /// <summary>
        /// Thời điểm kích hoạt
        /// </summary>
        public DateTime? ActivatedAt { get; set; }

        /// <summary>
        /// Ghi chú / Loại bản quyền (Ví dụ: Passkey bản quyền vĩnh viễn)
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}
