namespace QL_HocVien.Services
{
    public interface ISecureKeyVault
    {
        /// <summary>
        /// Lấy chuỗi Passphrase bí mật dùng để mã hóa và giải mã CSDL SQLCipher AES-256.
        /// Chuỗi được giải mã động bằng Windows DPAPI và ràng buộc phần cứng máy tính.
        /// </summary>
        string GetDatabasePassphrase();
    }
}
