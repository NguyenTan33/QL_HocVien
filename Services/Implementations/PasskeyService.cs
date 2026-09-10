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

        // Mốc hết hạn dùng thử miễn phí: 23:59:59 ngày 12/09/2026
        public DateTime TrialExpirationDate => new DateTime(2026, 9, 12, 23, 59, 59);

        public bool IsTrialActive => DateTime.Now <= TrialExpirationDate;

        public PasskeyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> ActivatePasskeyAsync(string username, string passkey)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Tên tài khoản không hợp lệ.");

            if (string.IsNullOrWhiteSpace(passkey))
                return (false, "Vui lòng nhập mã Passkey bản quyền.");

            string cleanKey = passkey.Trim().ToUpperInvariant();

            // Tìm Passkey trong danh mục
            var keyRecord = await _context.AccountPasskeys
                .FirstOrDefaultAsync(k => k.Passkey.ToUpper() == cleanKey);

            if (keyRecord == null)
            {
                return (false, "Mã Passkey không hợp lệ hoặc không tồn tại trong hệ thống Quân đội.");
            }

            // Kiểm tra trạng thái sử dụng của Passkey
            if (keyRecord.IsUsed)
            {
                if (string.Equals(keyRecord.UsedByUsername, username, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, $"Passkey này đã được kích hoạt thành công trước đó cho chính tài khoản '{username}'.");
                }

                return (false, "[BẢO MẬT] Passkey này đã được kích hoạt cho tài khoản khác và không thể tái sử dụng!");
            }

            // Tìm tài khoản người dùng
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                return (false, $"Không tìm thấy tài khoản '{username}' trong hệ thống.");
            }

            // Gán Passkey độc quyền cho tài khoản
            keyRecord.IsUsed = true;
            keyRecord.UsedByUsername = user.Username;
            keyRecord.ActivatedAt = DateTime.Now;

            user.HasPasskeyActivated = true;
            user.ActivatedPasskey = keyRecord.Passkey;
            user.PasskeyActivatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, $"Kích hoạt bản quyền thành công cho tài khoản '{user.FullName}' ({user.Username})! Chúc mừng đồng chí đã mở khóa toàn bộ tính năng tác chiến.");
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
