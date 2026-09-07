using System;
using System.IO;
using System.Management;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace QL_HocVien.Services
{
    public class SecureKeyVault : ISecureKeyVault
    {
        private static readonly byte[] EntropySalt = Encoding.UTF8.GetBytes("QLHV_Defense_Military_SecureSalt_2026#@!");
        private const string VaultFileName = "vault.dat";

        private static string? _cachedPassphrase;
        private static readonly object _lock = new();

        public string GetDatabasePassphrase()
        {
            return GetPassphrase();
        }

        /// <summary>
        /// Lấy hoặc sinh mới chuỗi Passphrase mã hóa CSDL an toàn
        /// </summary>
        public static string GetPassphrase()
        {
            if (!string.IsNullOrEmpty(_cachedPassphrase))
            {
                return _cachedPassphrase;
            }

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_cachedPassphrase))
                {
                    return _cachedPassphrase;
                }

                string vaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, VaultFileName);

                if (!File.Exists(vaultPath))
                {
                    // Lần đầu tiên khởi tạo: Tạo khóa ngẫu nhiên 256-bit và mã hóa với DPAPI + Machine Fingerprint
                    string newRawKey = GenerateRandom256BitKey();
                    byte[] protectedBytes = EncryptKeyWithDpapi(newRawKey);
                    File.WriteAllBytes(vaultPath, protectedBytes);

                    _cachedPassphrase = newRawKey;
                    return _cachedPassphrase;
                }

                try
                {
                    byte[] protectedBytes = File.ReadAllBytes(vaultPath);
                    _cachedPassphrase = DecryptKeyWithDpapi(protectedBytes);
                    return _cachedPassphrase;
                }
                catch (Exception ex)
                {
                    throw new SecurityException(
                        "Cơ sở dữ liệu được mã hóa bảo vệ bằng Windows DPAPI và Chữ ký phần cứng máy tính!\n" +
                        "Tệp khóa không thể giải mã trên thiết bị này hoặc đã bị can thiệp trái phép.\n\n" +
                        $"Chi tiết: {ex.Message}", ex);
                }
            }
        }

        private static string GenerateRandom256BitKey()
        {
            byte[] randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private static byte[] EncryptKeyWithDpapi(string rawKey)
        {
            string machineId = GetMachineFingerprint();
            string payload = $"{rawKey}::{machineId}";
            byte[] plainBytes = Encoding.UTF8.GetBytes(payload);

            return ProtectedData.Protect(plainBytes, EntropySalt, DataProtectionScope.CurrentUser);
        }

        private static string DecryptKeyWithDpapi(byte[] cipherBytes)
        {
            byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, EntropySalt, DataProtectionScope.CurrentUser);
            string payload = Encoding.UTF8.GetString(plainBytes);

            string currentMachineId = GetMachineFingerprint();
            string expectedSuffix = $"::{currentMachineId}";

            if (!payload.EndsWith(expectedSuffix))
            {
                throw new SecurityException("Chữ ký phần cứng máy tính không khớp! Bản sao chép không có quyền mở CSDL này.");
            }

            return payload.Substring(0, payload.Length - expectedSuffix.Length);
        }

        /// <summary>
        /// Tạo chữ ký phần cứng duy nhất cho thiết bị chạy ứng dụng
        /// </summary>
        private static string GetMachineFingerprint()
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(Environment.MachineName).Append('-');

                using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
                foreach (var obj in searcher.Get())
                {
                    var uuid = obj["UUID"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(uuid))
                    {
                        sb.Append(uuid.Trim());
                        break;
                    }
                }

                if (sb.Length <= Environment.MachineName.Length + 1)
                {
                    sb.Append(Environment.UserName).Append('-').Append(Environment.ProcessorCount);
                }

                return sb.ToString();
            }
            catch
            {
                return $"{Environment.MachineName}-{Environment.UserName}-{Environment.ProcessorCount}";
            }
        }
    }
}
