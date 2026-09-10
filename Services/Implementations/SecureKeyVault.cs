using System;
using System.IO;
using System.Management;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace QL_HocVien.Services.Implementations
{
    public class SecureKeyVault : ISecureKeyVault
    {
        private static readonly byte[] EntropySalt = Encoding.UTF8.GetBytes("QLHV_Defense_Military_SecureSalt_2026#@!");
        private static readonly byte[] RecoveryKey = SHA256.HashData(Encoding.UTF8.GetBytes("QLHV_Military_Secure_Recovery_SecretKey_2026!@#$"));
        private static readonly byte[] MagicHeader = new byte[] { 0x51, 0x4C, 0x56, 0x32 }; // "QLV2"
        private const string VaultFileName = "vault.dat";

        private static string? _cachedPassphrase;
        private static readonly object _lock = new();

        public string GetDatabasePassphrase()
        {
            return GetPassphrase();
        }

        /// <summary>
        /// Lấy hoặc sinh mới chuỗi Passphrase mã hóa CSDL an toàn.
        /// Hỗ trợ bảo vệ đa lớp: Windows DPAPI + Hardware Fingerprint + Recovery Auto-Rebind khi chuyển máy.
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
                    // Lần đầu tiên khởi tạo: Tạo khóa ngẫu nhiên 256-bit và ghi tệp cấu trúc QLV2
                    string newRawKey = GenerateRandom256BitKey();
                    SaveVaultFile(vaultPath, newRawKey);
                    _cachedPassphrase = newRawKey;
                    return _cachedPassphrase;
                }

                try
                {
                    byte[] fileBytes = File.ReadAllBytes(vaultPath);

                    // 1. Kiểm tra cấu trúc định dạng QLV2
                    if (IsMagicHeader(fileBytes))
                    {
                        using var ms = new MemoryStream(fileBytes);
                        ms.Seek(4, SeekOrigin.Begin); // Bỏ qua 4 byte Magic

                        using var reader = new BinaryReader(ms);
                        int dpapiLen = reader.ReadInt32();
                        byte[] dpapiBytes = reader.ReadBytes(dpapiLen);
                        int recoveryLen = reader.ReadInt32();
                        byte[] recoveryBytes = reader.ReadBytes(recoveryLen);

                        // Thử giải mã khối DPAPI (ưu tiên cao nhất trên máy hiện tại)
                        try
                        {
                            string key = DecryptKeyWithDpapi(dpapiBytes);
                            _cachedPassphrase = key;
                            return _cachedPassphrase;
                        }
                        catch
                        {
                            // Nếu DPAPI thất bại (ứng dụng được copy sang máy mới hoặc user khác):
                            // Thử giải mã qua khối Recovery để tự động liên kết lại (Auto-Rebind) sang máy mới
                            string recoveredKey = DecryptRecoveryBlock(recoveryBytes);

                            // Tự động mã hóa lại DPAPI cho thiết bị mới và lưu lại tệp
                            try
                            {
                                SaveVaultFile(vaultPath, recoveredKey);
                            }
                            catch { }

                            _cachedPassphrase = recoveredKey;
                            return _cachedPassphrase;
                        }
                    }
                    else
                    {
                        // Định dạng cũ (single DPAPI block)
                        try
                        {
                            string oldKey = DecryptKeyWithDpapi(fileBytes);
                            // Nâng cấp lên cấu trúc QLV2
                            try { SaveVaultFile(vaultPath, oldKey); } catch { }
                            _cachedPassphrase = oldKey;
                            return _cachedPassphrase;
                        }
                        catch
                        {
                            // Thử giải mã bằng Recovery
                            try
                            {
                                string recKey = DecryptRecoveryBlock(fileBytes);
                                SaveVaultFile(vaultPath, recKey);
                                _cachedPassphrase = recKey;
                                return _cachedPassphrase;
                            }
                            catch
                            {
                                throw new SecurityException(
                                    "Tệp khóa cơ sở dữ liệu (vault.dat) không tương thích hoặc đã bị can thiệp trái phép!");
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not SecurityException)
                {
                    throw new SecurityException(
                        "Không thể nạp khóa bảo mật CSDL SQLCipher:\n" + ex.Message, ex);
                }
            }
        }

        private static bool IsMagicHeader(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return false;
            return bytes[0] == MagicHeader[0] &&
                   bytes[1] == MagicHeader[1] &&
                   bytes[2] == MagicHeader[2] &&
                   bytes[3] == MagicHeader[3];
        }

        private static void SaveVaultFile(string vaultPath, string rawKey)
        {
            byte[] dpapiBlock = EncryptKeyWithDpapi(rawKey);
            byte[] recoveryBlock = EncryptRecoveryBlock(rawKey);

            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(MagicHeader);
                writer.Write(dpapiBlock.Length);
                writer.Write(dpapiBlock);
                writer.Write(recoveryBlock.Length);
                writer.Write(recoveryBlock);
            }

            File.WriteAllBytes(vaultPath, ms.ToArray());
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
                throw new SecurityException("Chữ ký phần cứng máy tính không khớp!");
            }

            return payload.Substring(0, payload.Length - expectedSuffix.Length);
        }

        private static byte[] EncryptRecoveryBlock(string rawKey)
        {
            using var aes = Aes.Create();
            aes.Key = RecoveryKey;
            aes.GenerateIV();

            byte[] plain = Encoding.UTF8.GetBytes(rawKey);
            using var encryptor = aes.CreateEncryptor();
            byte[] cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            byte[] result = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
            return result;
        }

        private static string DecryptRecoveryBlock(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = RecoveryKey;

            byte[] iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            aes.IV = iv;

            int cipherLen = data.Length - 16;
            byte[] cipher = new byte[cipherLen];
            Buffer.BlockCopy(data, 16, cipher, 0, cipherLen);

            using var decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
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
