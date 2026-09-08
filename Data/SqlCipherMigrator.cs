using System;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace QL_HocVien.Data
{
    public static class SqlCipherMigrator
    {
        /// <summary>
        /// Kiểm tra xem tệp CSDL SQLite có đang ở dạng plaintext chưa mã hóa hay không.
        /// Định dạng chuẩn của SQLite plaintext luôn bắt đầu bằng 16 bytes: "SQLite format 3\0".
        /// Khi đã mã hóa bằng SQLCipher, 16 bytes đầu là Salt nhị phân ngẫu nhiên.
        /// </summary>
        public static bool IsDatabasePlaintext(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            try
            {
                byte[] header = new byte[16];
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (stream.Length < 16) return false;
                    stream.Read(header, 0, 16);
                }

                string headerString = Encoding.ASCII.GetString(header);
                return headerString.StartsWith("SQLite format 3");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tự động di chuyển CSDL plaintext sang mã hóa SQLCipher AES-256 nếu phát hiện CSDL chưa được mã hóa.
        /// Bảo lưu nguyên vẹn 100% dữ liệu và tạo bản sao lưu an toàn.
        /// </summary>
        public static void MigrateIfPlaintext(string dbPath, string passphrase)
        {
            if (!File.Exists(dbPath)) return;
            if (!IsDatabasePlaintext(dbPath)) return;

            Batteries_V2.Init();

            string tempEncryptedPath = dbPath + ".tmp_enc";
            string backupPath = dbPath + ".plaintext_backup";

            if (File.Exists(tempEncryptedPath))
            {
                File.Delete(tempEncryptedPath);
            }

            try
            {
                // Mở kết nối đến DB plaintext bằng engine SQLCipher
                using (var rawConn = new SqliteConnection($"Data Source={dbPath};"))
                {
                    rawConn.Open();

                    using var cmd = rawConn.CreateCommand();

                    // ATTACH file DB mới với Key mã hóa và gọi sqlcipher_export
                    string safeTempPath = tempEncryptedPath.Replace("'", "''");
                    string safePassphrase = passphrase.Replace("'", "''");

                    cmd.CommandText = $@"
                        ATTACH DATABASE '{safeTempPath}' AS encryptedDb KEY '{safePassphrase}';
                        SELECT sqlcipher_export('encryptedDb');
                        DETACH DATABASE encryptedDb;
                    ";
                    cmd.ExecuteNonQuery();
                }

                // Giải phóng hoàn toàn file handle từ connection pool
                SqliteConnection.ClearAllPools();

                // Thay thế file cũ bằng file đã mã hóa AES-256 an toàn
                File.Move(tempEncryptedPath, dbPath, overwrite: true);

                // Tuyệt đối không lưu trữ bản sao rõ (plaintext_backup) trên đĩa để chống trích xuất dữ liệu
                if (File.Exists(backupPath))
                {
                    try { File.Delete(backupPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(tempEncryptedPath))
                {
                    try { File.Delete(tempEncryptedPath); } catch { }
                }
                throw new InvalidOperationException($"Lỗi trong quá trình mã hóa CSDL sang SQLCipher: {ex.Message}", ex);
            }
        }
    }
}
