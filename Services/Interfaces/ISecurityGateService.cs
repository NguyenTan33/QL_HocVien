using System;
using System.Threading.Tasks;

namespace QL_HocVien.Services.Interfaces
{
    /// <summary>
    /// Quáº£n lÃ½ KhÃ³a báº£o máº­t cáº¥p 2 (KhÃ³a rÆ°Æ¡ng báº£o vá»‡ dá»¯ liá»‡u).
    /// TuÃ¢n thá»§ nguyÃªn lÃ½ SOLID (SRP, ISP, DIP).
    /// </summary>
    public interface ISecurityGateService
    {
        /// <summary>
        /// Cho biáº¿t chá»©c nÄƒng khÃ³a báº£o máº­t cáº¥p 2 cÃ³ Ä‘ang Ä‘Æ°á»£c báº­t hay khÃ´ng.
        /// </summary>
        bool IsProtectionEnabled { get; }

        /// <summary>
        /// Cho biáº¿t há»‡ thá»‘ng hiá»‡n táº¡i cÃ³ Ä‘ang trong tráº¡ng thÃ¡i Má»Ÿ khÃ³a (cÃ²n hiá»‡u lá»±c trong 5 phÃºt) hay khÃ´ng.
        /// </summary>
        bool IsUnlocked { get; }

        /// <summary>
        /// Sá»‘ giÃ¢y cÃ²n láº¡i trÆ°á»›c khi tá»± Ä‘á»™ng khÃ³a láº¡i (tá»‘i Ä‘a 300 giÃ¢y = 5 phÃºt).
        /// </summary>
        int RemainingSeconds { get; }

        /// <summary>
        /// Chuá»—i Ä‘á»‹nh dáº¡ng thá»i gian cÃ²n láº¡i (vÃ­ dá»¥: "04:59").
        /// </summary>
        string FormattedRemainingTime { get; }

        /// <summary>
        /// KÃ­ch hoáº¡t tÃ­nh nÄƒng báº£o máº­t vá»›i máº­t kháº©u má»›i.
        /// </summary>
        bool EnableProtection(string password);

        /// <summary>
        /// Táº¯t tÃ­nh nÄƒng báº£o máº­t (yÃªu cáº§u máº­t kháº©u hiá»‡n táº¡i).
        /// </summary>
        bool DisableProtection(string currentPassword);

        /// <summary>
        /// Äá»•i máº­t kháº©u báº£o máº­t cáº¥p 2.
        /// </summary>
        bool ChangePassword(string oldPassword, string newPassword);

        /// <summary>
        /// Kiá»ƒm tra tÃ­nh chÃ­nh xÃ¡c cá»§a máº­t kháº©u.
        /// </summary>
        bool VerifyPassword(string password);

        /// <summary>
        /// Má»Ÿ khÃ³a cáº¥p 2 cho thá»i gian tá»± do (5 phÃºt = 300 giÃ¢y).
        /// </summary>
        void UnlockForGracePeriod();

        /// <summary>
        /// Láº­p tá»©c thu há»“i quyá»n thao tÃ¡c, khÃ³a mÃ n hÃ¬nh / dá»¯ liá»‡u ngay láº­p tá»©c.
        /// </summary>
        void LockNow();

        /// <summary>
        /// Báº£o vá»‡ má»™t hÃ nh Ä‘á»™ng nghiá»‡p vá»¥ (ThÃªm, Sá»­a, XÃ³a, Xuáº¥t/Nháº­p Excel).
        /// Náº¿u báº£o vá»‡ chÆ°a báº­t: Tráº£ vá» true ngay láº­p tá»©c.
        /// Náº¿u Ä‘ang má»Ÿ khÃ³a (cÃ²n trong 5 phÃºt): Tráº£ vá» true ngay láº­p tá»©c.
        /// Náº¿u Ä‘ang khÃ³a: Hiá»ƒn thá»‹ Dialog yÃªu cáº§u nháº­p máº­t kháº©u cáº¥p 2.
        /// Náº¿u ngÆ°á»i dÃ¹ng xÃ¡c thá»±c thÃ nh cÃ´ng: KÃ­ch hoáº¡t 5 phÃºt má»Ÿ khÃ³a vÃ  tráº£ vá» true.
        /// Náº¿u tháº¥t báº¡i hoáº·c ngÆ°á»i dÃ¹ng báº¥m Há»§y: Tráº£ vá» false.
        /// </summary>
        Task<bool> EnsureUnlockedAsync(string actionDescription = "thá»±c hiá»‡n thao tÃ¡c nÃ y");

        /// <summary>
        /// Sá»± kiá»‡n phÃ¡t sinh khi tráº¡ng thÃ¡i báº£o máº­t thay Ä‘á»•i (Báº­t/Táº¯t, Má»Ÿ/KhÃ³a, Äáº¿m giÃ¢y).
        /// </summary>
        event Action? OnSecurityStateChanged;
    }
}

