namespace QL_HocVien.Services.Interfaces
{
    public interface ISecureKeyVault
    {
        /// <summary>
        /// Láº¥y chuá»—i Passphrase bÃ­ máº­t dÃ¹ng Ä‘á»ƒ mÃ£ hÃ³a vÃ  giáº£i mÃ£ CSDL SQLCipher AES-256.
        /// Chuá»—i Ä‘Æ°á»£c giáº£i mÃ£ Ä‘á»™ng báº±ng Windows DPAPI vÃ  rÃ ng buá»™c pháº§n cá»©ng mÃ¡y tÃ­nh.
        /// </summary>
        string GetDatabasePassphrase();
    }
}

