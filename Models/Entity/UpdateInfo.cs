using System;
using System.Text.Json.Serialization;

namespace QL_HocVien.Models.Entity
{
    /// <summary>
    /// Cấu trúc tệp version.json máy chủ cung cấp để kiểm tra phiên bản mới.
    /// </summary>
    public class UpdateInfo
    {
        [JsonPropertyName("latestVersion")]
        public string LatestVersion { get; set; } = string.Empty;

        [JsonPropertyName("minimumVersion")]
        public string MinimumVersion { get; set; } = string.Empty;

        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("releaseNotes")]
        public string ReleaseNotes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trạng thái kết quả sau khi kiểm tra phiên bản.
    /// </summary>
    public enum UpdateStatus
    {
        /// <summary>
        /// Ứng dụng đã là phiên bản mới nhất, không cần cập nhật.
        /// </summary>
        UpToDate,

        /// <summary>
        /// Có phiên bản mới nhưng là tùy chọn, người dùng có thể cập nhật hoặc để sau.
        /// </summary>
        OptionalUpdateAvailable,

        /// <summary>
        /// Phiên bản hiện tại dưới mức tối thiểu hoặc bản mới bắt buộc cập nhật.
        /// </summary>
        MandatoryUpdateRequired,

        /// <summary>
        /// Quá trình kiểm tra phiên bản thất bại (mất mạng, timeout, json lỗi).
        /// </summary>
        CheckFailed
    }

    /// <summary>
    /// Đóng gói kết quả kiểm tra phiên bản kèm thông tin chi tiết.
    /// </summary>
    public class UpdateCheckResult
    {
        public UpdateStatus Status { get; set; }
        public Version CurrentVersion { get; set; } = new Version(1, 0, 0, 0);
        public Version? LatestVersion { get; set; }
        public Version? MinimumVersion { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
        public string? ErrorMessage { get; set; }

        public bool HasUpdate => Status == UpdateStatus.OptionalUpdateAvailable || Status == UpdateStatus.MandatoryUpdateRequired;
        public bool IsMandatory => Status == UpdateStatus.MandatoryUpdateRequired;
    }

    /// <summary>
    /// Thông tin tiến độ tải tệp installer trong thời gian thực.
    /// </summary>
    public class DownloadProgressReport
    {
        public int Percentage { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public double BytesPerSecond { get; set; }

        public string FormattedProgress => TotalBytes > 0 
            ? $"{BytesDownloaded / (1024.0 * 1024.0):F1} MB / {TotalBytes / (1024.0 * 1024.0):F1} MB ({Percentage}%)"
            : $"{BytesDownloaded / (1024.0 * 1024.0):F1} MB đã tải ({Percentage}%)";

        public string FormattedSpeed
        {
            get
            {
                if (BytesPerSecond >= 1024 * 1024)
                    return $"{BytesPerSecond / (1024 * 1024):F1} MB/s";
                if (BytesPerSecond >= 1024)
                    return $"{BytesPerSecond / 1024:F0} KB/s";
                return $"{BytesPerSecond:F0} B/s";
            }
        }
    }
}
