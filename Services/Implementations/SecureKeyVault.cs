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
        /// Láº¥y hoáº·c sinh má»›i chuá»—i Passphrase mÃ£ hÃ³a CSDL an toÃ n.
        /// Há»— trá»£ báº£o vá»‡ Ä‘a lá»›p: Windows DPAPI + Hardware Fingerprint + Recovery Auto-Rebind khi chuyá»ƒn mÃ¡y.
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
                    // Láº§n Ä‘áº§u tiÃªn khá»Ÿi táº¡o: Táº¡o khÃ³a ngáº«u nhiÃªn 256-bit vÃ  ghi tá»‡p cáº¥u trÃºc QLV2
                    string newRawKey = GenerateRandom256BitKey();
                    SaveVaultFile(vaultPath, newRawKey);
                    _cachedPassphrase = newRawKey;
                    return _cachedPassphrase;
                }

                try
                {
                    byte[] fileBytes = File.ReadAllBytes(vaultPath);

                    // 1. Kiá»ƒm tra cáº¥u trÃºc Ä‘á»‹nh dáº¡ng QLV2
                    if (IsMagicHeader(fileBytes))
                    {
                        using var ms = new MemoryStream(fileBytes);
                        ms.Seek(4, SeekOrigin.Begin); // Bá» qua 4 byte Magic

                        using var reader = new BinaryReader(ms);
                        int dpapiLen = reader.ReadInt32();
                        byte[] dpapiBytes = reader.ReadBytes(dpapiLen);
                        int recoveryLen = reader.ReadInt32();
                        byte[] recoveryBytes = reader.ReadBytes(recoveryLen);

                        // Thá»­ giáº£i mÃ£ khá»‘i DPAPI (Æ°u tiÃªn cao nháº¥t trÃªn mÃ¡y hiá»‡n táº¡i)
                        try
                        {
                            string key = DecryptKeyWithDpapi(dpapiBytes);
                            _cachedPassphrase = key;
                            return _cachedPassphrase;
                        }
                        catch
                        {
                            // Náº¿u DPAPI tháº¥t báº¡i (á»©ng dá»¥ng Ä‘Æ°á»£c copy sang mÃ¡y má»›i hoáº·c user khÃ¡c):
                            // Thá»­ giáº£i mÃ£ qua khá»‘i Recovery Ä‘á»ƒ tá»± Ä‘á»™ng liÃªn káº¿t láº¡i (Auto-Rebind) sang mÃ¡y má»›i
                            string recoveredKey = DecryptRecoveryBlock(recoveryBytes);

                            // Tá»± Ä‘á»™ng mÃ£ hÃ³a láº¡i DPAPI cho thiáº¿t bá»‹ má»›i vÃ  lÆ°u láº¡i tá»‡p
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
                        // Äá»‹nh dáº¡ng cÅ© (single DPAPI block)
                        try
                        {
                            string oldKey = DecryptKeyWithDpapi(fileBytes);
                            // NÃ¢ng cáº¥p lÃªn cáº¥u trÃºc QLV2
                            try { SaveVaultFile(vaultPath, oldKey); } catch { }
                            _cachedPassphrase = oldKey;
                            return _cachedPassphrase;
                        }
                        catch
                        {
                            // Thá»­ giáº£i mÃ£ báº±ng Recovery
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
                                    "Tá»‡p khÃ³a cÆ¡ sá»Ÿ dá»¯ liá»‡u (vault.dat) khÃ´ng tÆ°Æ¡ng thÃ­ch hoáº·c Ä‘Ã£ bá»‹ can thiá»‡p trÃ¡i phÃ©p!");
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not SecurityException)
                {
                    throw new SecurityException(
                        "KhÃ´ng thá»ƒ náº¡p khÃ³a báº£o máº­t CSDL SQLCipher:\n" + ex.Message, ex);
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
                throw new SecurityException("Chá»¯ kÃ½ pháº§n cá»©ng mÃ¡y tÃ­nh khÃ´ng khá»›p!");
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
        /// Táº¡o chá»¯ kÃ½ pháº§n cá»©ng duy nháº¥t cho thiáº¿t bá»‹ cháº¡y á»©ng dá»¥ng
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

