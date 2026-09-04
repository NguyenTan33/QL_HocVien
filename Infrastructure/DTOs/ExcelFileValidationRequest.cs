using System.IO;

namespace QL_HocVien.Infrastructure.DTOs
{
    /// <summary>
    /// DTO yêu cầu kiểm duyệt bảo mật tập tin Excel trước khi xử lý nhập dữ liệu.
    /// </summary>
    public class ExcelFileValidationRequest
    {
        public string FilePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public Stream? FileStream { get; set; }
        public long MaxSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB mặc định
        public bool DisallowMacros { get; set; } = true; // Mặc định từ chối các file chứa Macro VBA

        public ExcelFileValidationRequest(string filePath, bool disallowMacros = true)
        {
            FilePath = filePath;
            OriginalFileName = Path.GetFileName(filePath);
            DisallowMacros = disallowMacros;
        }

        public ExcelFileValidationRequest(Stream fileStream, string fileName, bool disallowMacros = true)
        {
            FileStream = fileStream;
            OriginalFileName = fileName;
            FilePath = fileName;
            DisallowMacros = disallowMacros;
        }
    }
}
