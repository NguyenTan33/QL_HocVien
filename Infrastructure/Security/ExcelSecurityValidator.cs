using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.DTOs;

namespace QL_HocVien.Infrastructure.Security
{
    /// <summary>
    /// Triển khai kiểm duyệt an ninh tập tin Excel toàn diện.
    /// Tuân thủ nguyên lý Single Responsibility (chuyên trách kiểm tra an toàn tập tin).
    /// </summary>
    public class ExcelSecurityValidator : IExcelSecurityValidator
    {
        // 1. Danh sách các đuôi file thực thi độc hại tuyệt đối bị cấm
        private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".vbe", ".js", ".jse",
            ".wsf", ".wsh", ".msc", ".msi", ".msp", ".scr", ".pif", ".hta", ".cpl",
            ".jar", ".reg", ".inf", ".ins", ".sct", ".sh", ".bash", ".bin", ".app",
            ".deb", ".rpm", ".apk", ".gadget", ".theme", ".dll", ".com", ".sys"
        };

        // 2. Danh sách đuôi file macro Excel
        private static readonly HashSet<string> MacroExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".xlsm", ".xltm", ".xlsb", ".xlam"
        };

        // 3. Chữ ký nhị phân (Magic Bytes)
        // ZIP Header (OpenXML .xlsx)
        private static readonly byte[] ZipMagicHeader = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        // Windows PE Executable (.exe, .dll, .scr)
        private static readonly byte[] PeExecutableMagic = new byte[] { 0x4D, 0x5A }; // "MZ"
        // Linux ELF
        private static readonly byte[] ElfExecutableMagic = new byte[] { 0x7F, 0x45, 0x4C, 0x46 };
        // OLE2 Compound Document (Legacy .xls)
        private static readonly byte[] Ole2MagicHeader = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        public async Task<FileValidationResult> ValidateExcelFileAsync(string filePath, bool disallowMacros = true, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return FileValidationResult.Failure("unknown", "Đường dẫn tập tin không được để trống.");
            }

            if (!File.Exists(filePath))
            {
                return FileValidationResult.Failure(Path.GetFileName(filePath), "Tập tin không tồn tại trên hệ thống đĩa.");
            }

            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);

            // Kiểm tra kích thước file
            if (fileInfo.Length == 0)
            {
                return FileValidationResult.Failure(fileName, "Tập tin rỗng (dung lượng 0 bytes).");
            }

            const long maxSizeBytes = 50 * 1024 * 1024; // 50MB
            if (fileInfo.Length > maxSizeBytes)
            {
                return FileValidationResult.Failure(fileName, $"Dung lượng tập tin ({fileInfo.Length / 1024 / 1024}MB) vượt quá giới hạn cho phép (50MB).");
            }

            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await ValidateExcelStreamInternalAsync(stream, fileName, fileInfo.Length, disallowMacros, ct);
        }

        public async Task<FileValidationResult> ValidateExcelFileAsync(Stream stream, string fileName, bool disallowMacros = true, CancellationToken ct = default)
        {
            if (stream == null || !stream.CanRead)
            {
                return FileValidationResult.Failure(fileName, "Luồng dữ liệu tập tin (Stream) không thể đọc.");
            }

            long streamLength = stream.CanSeek ? stream.Length : 0;
            return await ValidateExcelStreamInternalAsync(stream, fileName, streamLength, disallowMacros, ct);
        }

        private async Task<FileValidationResult> ValidateExcelStreamInternalAsync(
            Stream stream, 
            string fileName, 
            long fileLength, 
            bool disallowMacros, 
            CancellationToken ct)
        {
            // BƯỚC 1: KIỂM TRA TÊN FILE & ĐUÔI FILE KÉP
            // Chống ký tự đổi chiều hiển thị Unicode RLO (\u202E)
            if (fileName.Contains('\u202E') || fileName.Contains('\u202D'))
            {
                return FileValidationResult.Failure(fileName, 
                    "Phát hiện ký tự ẩn Unicode RLO đổi hướng tên file nguy hiểm (Kỹ thuật đánh lừa đuôi file thực thi).");
            }

            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            // Chặn ngay các đuôi thực thi
            if (ExecutableExtensions.Contains(ext))
            {
                return FileValidationResult.Failure(fileName, 
                    $"Định dạng tập tin '{ext}' là tệp thực thi bị cấm hoàn toàn vì lý do an ninh hệ thống!");
            }

            // Chống tấn công đuôi file kép: ví dụ "bang_diem.xlsx.exe", "ds.doc.bat"
            var fileNameWithoutFinalExt = Path.GetFileNameWithoutExtension(fileName);
            var innerExt = Path.GetExtension(fileNameWithoutFinalExt).ToLowerInvariant();
            if (!string.IsNullOrEmpty(innerExt) && ExecutableExtensions.Contains(innerExt))
            {
                return FileValidationResult.Failure(fileName, 
                    $"Phát hiện dấu hiệu tấn công đuôi kép giả mạo: '{fileName}' chứa phần mở rộng thực thi độc hại '{innerExt}'!");
            }

            // Kiểm tra đuôi file có thuộc nhóm cho phép hay không
            if (ext != ".xlsx" && ext != ".xls")
            {
                if (MacroExtensions.Contains(ext))
                {
                    return FileValidationResult.Failure(fileName, 
                        $"Định dạng '{ext}' chứa Macro tiềm ẩn nguy cơ bảo mật. Hệ thống chỉ chấp nhận định dạng an toàn '.xlsx' hoặc '.xls'.");
                }

                return FileValidationResult.Failure(fileName, 
                    $"Định dạng tập tin '{ext}' không hợp lệ. Chỉ chấp nhận tập tin Excel chuẩn (.xlsx hoặc .xls).");
            }

            // BƯỚC 2: KIỂM TRA CHỮ KÝ NHỊ PHÂN (MAGIC BYTES)
            byte[] headerBytes = new byte[8];
            long originalPos = stream.CanSeek ? stream.Position : 0;
            int bytesRead = await stream.ReadAsync(headerBytes.AsMemory(0, 8), ct);

            if (bytesRead < 4)
            {
                return FileValidationResult.Failure(fileName, "Tập tin bị lỗi hoặc không đủ độ dài header để xác thực nhị phân.");
            }

            // Kiểm tra xem có phải file thực thi Windows MZ (0x4D 0x5A) bị đổi tên sang .xlsx không
            if (headerBytes[0] == PeExecutableMagic[0] && headerBytes[1] == PeExecutableMagic[1])
            {
                return FileValidationResult.Failure(fileName, 
                    "CẢNH BÁO NGUY HIỂM: Tập tin có chữ ký nhị phân 'MZ' của chương trình thực thi (Windows Executable PE / .EXE)! " +
                    "Đây là hành vi giả mạo đuôi .xlsx để vượt qua tường lửa.");
            }

            // Kiểm tra chữ ký Linux ELF
            if (headerBytes[0] == ElfExecutableMagic[0] && headerBytes[1] == ElfExecutableMagic[1] &&
                headerBytes[2] == ElfExecutableMagic[2] && headerBytes[3] == ElfExecutableMagic[3])
            {
                return FileValidationResult.Failure(fileName, 
                    "CẢNH BÁO NGUY HIỂM: Tập tin có chữ ký nhị phân thực thi ELF (Linux Executable)! Bị từ chối ngay lập tức.");
            }

            // Xác thực chữ ký đối với file .xlsx (Bắt buộc là ZIP OpenXML: PK\x03\x04)
            if (ext == ".xlsx")
            {
                bool isZip = headerBytes[0] == ZipMagicHeader[0] &&
                             headerBytes[1] == ZipMagicHeader[1] &&
                             headerBytes[2] == ZipMagicHeader[2] &&
                             headerBytes[3] == ZipMagicHeader[3];

                if (!isZip)
                {
                    return FileValidationResult.Failure(fileName, 
                        "Tập tin mang phần mở rộng .xlsx nhưng nội dung nhị phân không phải định dạng OpenXML ZIP hợp lệ.");
                }
            }

            // Xác thực chữ ký đối với file .xls cũ (OLE2: D0 CF 11 E0)
            if (ext == ".xls")
            {
                bool isOle2 = headerBytes.Take(8).SequenceEqual(Ole2MagicHeader);
                if (!isOle2)
                {
                    return FileValidationResult.Failure(fileName, 
                        "Tập tin mang phần mở rộng .xls nhưng nội dung không phải định dạng nhị phân Excel OLE2 hợp lệ.");
                }
            }

            // BƯỚC 3: PHÂN TÍCH GÓI ZIP OPENXML ĐỐI VỚI .XLSX (KIỂM TRA MACRO VÀ TỆP NHÚNG THỰC THI)
            if (ext == ".xlsx" && stream.CanSeek)
            {
                stream.Position = originalPos; // Reset con trỏ luồng để đọc ZIP
                try
                {
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                    
                    long totalUncompressedSize = 0;
                    int entryCount = 0;

                    foreach (var entry in archive.Entries)
                    {
                        ct.ThrowIfCancellationRequested();
                        entryCount++;

                        if (entryCount > 500)
                        {
                            return FileValidationResult.Failure(fileName, "Tập tin chứa quá nhiều tệp con bất thường (> 500 entries), nghi vấn tấn công nén.");
                        }

                        totalUncompressedSize += entry.Length;
                        if (totalUncompressedSize > 100 * 1024 * 1024) // 100MB giới hạn giải nén chống Zip Bomb
                        {
                            return FileValidationResult.Failure(fileName, "Phát hiện dung lượng giải nén quá lớn (>100MB), từ chối để chống tấn công Zip Bomb.");
                        }

                        var entryName = entry.FullName.ToLowerInvariant();
                        var entryExt = Path.GetExtension(entryName);

                        // Kiểm tra Macro VBA nhúng
                        if (disallowMacros && (entryName.Contains("vbaproject.bin") || entryName.Contains("vbadat.bin") || entryName.EndsWith(".bin")))
                        {
                            return FileValidationResult.Failure(fileName, 
                                "Tập tin chứa mã kịch bản Macro VBA tự động ('vbaProject.bin'). Hệ thống chặn toàn bộ Macro để bảo vệ máy trạm.");
                        }

                        // Kiểm tra tệp thực thi nhúng bên trong (ví dụ trong thư mục xl/embeddings/)
                        if (ExecutableExtensions.Contains(entryExt))
                        {
                            return FileValidationResult.Failure(fileName, 
                                $"Phát hiện tập tin thực thi nhúng độc hại '{entry.FullName}' bên trong bảng tính Excel!");
                        }
                    }
                }
                catch (InvalidDataException)
                {
                    return FileValidationResult.Failure(fileName, "Tập tin Excel bị hỏng hoặc cấu trúc gói OpenXML không hợp lệ.");
                }
                finally
                {
                    stream.Position = originalPos;
                }
            }

            return FileValidationResult.Success(fileName, fileLength, "Tập tin Excel hợp lệ, đã vượt qua tất cả các lớp kiểm duyệt an ninh.");
        }
    }
}
