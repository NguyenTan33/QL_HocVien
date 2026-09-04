using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.DTOs;

namespace QL_HocVien.Infrastructure.Security
{
    /// <summary>
    /// Giao diện kiểm duyệt an ninh chuyên sâu tập tin Excel:
    /// - Chặn toàn bộ đuôi file thực thi nguy hiểm (.exe, .bat, .cmd, .ps1, .vbs, .js, .scr, .msi, .dll, .com...)
    /// - Chống tấn công đuôi file kép (.xlsx.exe, .doc.bat...)
    /// - Kiểm tra Magic Bytes nhị phân (chống việc đổi đuôi file .exe thành .xlsx)
    /// - Giải nén OpenXML ZIP an toàn để kiểm tra Macro (vbaProject.bin) và các tập tin thực thi nhúng trong bảng tính
    /// - Kiểm soát kích thước và tỷ lệ nén chống Zip Bomb
    /// </summary>
    public interface IExcelSecurityValidator
    {
        Task<FileValidationResult> ValidateExcelFileAsync(string filePath, bool disallowMacros = true, CancellationToken ct = default);
        Task<FileValidationResult> ValidateExcelFileAsync(Stream stream, string fileName, bool disallowMacros = true, CancellationToken ct = default);
    }
}
