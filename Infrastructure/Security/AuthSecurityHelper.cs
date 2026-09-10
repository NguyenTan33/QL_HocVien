using System;
using System.Security.Cryptography;
using System.Text;

namespace QL_HocVien.Infrastructure.Security
{
    /// <summary>
    /// Các tiện ích bảo mật độc lập dùng cho hệ thống xác thực 100% Offline
    /// </summary>
    public static class AuthSecurityHelper
    {
        private const string SecurityAnswerSalt = "_QLHV_SEC_ANSWER_SALT_2026!#";

        /// <summary>
        /// Băm câu trả lời bảo mật một chiều với Salt.
        /// Chuẩn hóa: loại bỏ khoảng trắng thừa ở đầu/cuối và chuyển về chữ thường để tránh phân biệt HOA/thường.
        /// </summary>
        public static string HashSecurityAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return string.Empty;

            string normalized = answer.Trim().ToLowerInvariant();
            byte[] bytes = Encoding.UTF8.GetBytes(normalized + SecurityAnswerSalt);
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Kiểm tra câu trả lời bảo mật người dùng nhập với mã băm đã lưu
        /// </summary>
        public static bool VerifySecurityAnswer(string inputAnswer, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(inputAnswer) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            string computedHash = HashSecurityAnswer(inputAnswer);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(storedHash)
            );
        }

        /// <summary>
        /// Mã hóa chuỗi bí mật bằng Windows DPAPI
        /// </summary>
        public static string EncryptSecret(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
                byte[] enc = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return "enc:" + Convert.ToBase64String(enc);
            }
            catch
            {
                return plain;
            }
        }

        /// <summary>
        /// Giải mã chuỗi bí mật bằng Windows DPAPI
        /// </summary>
        public static string DecryptSecret(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return string.Empty;
            if (!cipher.StartsWith("enc:")) return cipher;
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipher.Substring(4));
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
