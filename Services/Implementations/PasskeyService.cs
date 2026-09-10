using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
{
    public class PasskeyService : IPasskeyService
    {
        private readonly AppDbContext _context;

        // Má»‘c háº¿t háº¡n dÃ¹ng thá»­ miá»…n phÃ­: 23:59:59 ngÃ y 12/09/2026
        public DateTime TrialExpirationDate => new DateTime(2026, 9, 12, 23, 59, 59);

        public bool IsTrialActive => DateTime.Now <= TrialExpirationDate;

        public PasskeyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> ActivatePasskeyAsync(string username, string passkey)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "TÃªn tÃ i khoáº£n khÃ´ng há»£p lá»‡.");

            if (string.IsNullOrWhiteSpace(passkey))
                return (false, "Vui lÃ²ng nháº­p mÃ£ Passkey báº£n quyá»n.");

            string cleanKey = passkey.Trim().ToUpperInvariant();

            // TÃ¬m Passkey trong danh má»¥c
            var keyRecord = await _context.AccountPasskeys
                .FirstOrDefaultAsync(k => k.Passkey.ToUpper() == cleanKey);

            if (keyRecord == null)
            {
                return (false, "MÃ£ Passkey khÃ´ng há»£p lá»‡ hoáº·c khÃ´ng tá»“n táº¡i trong há»‡ thá»‘ng QuÃ¢n Ä‘á»™i.");
            }

            // Kiá»ƒm tra tráº¡ng thÃ¡i sá»­ dá»¥ng cá»§a Passkey
            if (keyRecord.IsUsed)
            {
                if (string.Equals(keyRecord.UsedByUsername, username, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, $"Passkey nÃ y Ä‘Ã£ Ä‘Æ°á»£c kÃ­ch hoáº¡t thÃ nh cÃ´ng trÆ°á»›c Ä‘Ã³ cho chÃ­nh tÃ i khoáº£n '{username}'.");
                }

                return (false, "[Báº¢O Máº¬T] Passkey nÃ y Ä‘Ã£ Ä‘Æ°á»£c kÃ­ch hoáº¡t cho tÃ i khoáº£n khÃ¡c vÃ  khÃ´ng thá»ƒ tÃ¡i sá»­ dá»¥ng!");
            }

            // TÃ¬m tÃ i khoáº£n ngÆ°á»i dÃ¹ng
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                return (false, $"KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n '{username}' trong há»‡ thá»‘ng.");
            }

            // GÃ¡n Passkey Ä‘á»™c quyá»n cho tÃ i khoáº£n
            keyRecord.IsUsed = true;
            keyRecord.UsedByUsername = user.Username;
            keyRecord.ActivatedAt = DateTime.Now;

            user.HasPasskeyActivated = true;
            user.ActivatedPasskey = keyRecord.Passkey;
            user.PasskeyActivatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, $"KÃ­ch hoáº¡t báº£n quyá»n thÃ nh cÃ´ng cho tÃ i khoáº£n '{user.FullName}' ({user.Username})! ChÃºc má»«ng Ä‘á»“ng chÃ­ Ä‘Ã£ má»Ÿ khÃ³a toÃ n bá»™ tÃ­nh nÄƒng tÃ¡c chiáº¿n.");
        }

        public async Task<bool> HasUserActivatedPasskeyAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            return user?.HasPasskeyActivated ?? false;
        }

        public async Task<List<AccountPasskey>> GetAllPasskeysAsync()
        {
            return await _context.AccountPasskeys
                .AsNoTracking()
                .OrderBy(k => k.Id)
                .ToListAsync();
        }
    }
}

