using System;
using System.Collections.Generic;

namespace QL_HocVien.Infrastructure.DTOs
{
    /// <summary>
    /// Kết quả kiểm duyệt an ninh tập tin được tải lên hệ thống.
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string DetectedMimeType { get; set; } = string.Empty;
        public List<string> SecurityWarnings { get; set; } = new();

        public static FileValidationResult Success(string fileName, long size, string message = "Tập tin an toàn và hợp lệ.")
        {
            return new FileValidationResult
            {
                IsValid = true,
                FileName = fileName,
                Extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant(),
                FileSizeBytes = size,
                Message = message
            };
        }

        public static FileValidationResult Failure(string fileName, string message, string? warning = null)
        {
            var res = new FileValidationResult
            {
                IsValid = false,
                FileName = fileName,
                Extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant(),
                Message = message
            };
            if (!string.IsNullOrWhiteSpace(warning))
            {
                res.SecurityWarnings.Add(warning);
            }
            return res;
        }
    }
}
