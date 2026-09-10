using System;
using System.Text.Json.Serialization;

namespace QL_HocVien.Models.Entity
{
    /// <summary>
    /// Cáº¥u trÃºc tá»‡p version.json mÃ¡y chá»§ cung cáº¥p Ä‘á»ƒ kiá»ƒm tra phiÃªn báº£n má»›i.
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
    /// Tráº¡ng thÃ¡i káº¿t quáº£ sau khi kiá»ƒm tra phiÃªn báº£n.
    /// </summary>
    public enum UpdateStatus
    {
        /// <summary>
        /// á»¨ng dá»¥ng Ä‘Ã£ lÃ  phiÃªn báº£n má»›i nháº¥t, khÃ´ng cáº§n cáº­p nháº­t.
        /// </summary>
        UpToDate,

        /// <summary>
        /// CÃ³ phiÃªn báº£n má»›i nhÆ°ng lÃ  tÃ¹y chá»n, ngÆ°á»i dÃ¹ng cÃ³ thá»ƒ cáº­p nháº­t hoáº·c Ä‘á»ƒ sau.
        /// </summary>
        OptionalUpdateAvailable,

        /// <summary>
        /// PhiÃªn báº£n hiá»‡n táº¡i dÆ°á»›i má»©c tá»‘i thiá»ƒu hoáº·c báº£n má»›i báº¯t buá»™c cáº­p nháº­t.
        /// </summary>
        MandatoryUpdateRequired,

        /// <summary>
        /// QuÃ¡ trÃ¬nh kiá»ƒm tra phiÃªn báº£n tháº¥t báº¡i (máº¥t máº¡ng, timeout, json lá»—i).
        /// </summary>
        CheckFailed
    }

    /// <summary>
    /// ÄÃ³ng gÃ³i káº¿t quáº£ kiá»ƒm tra phiÃªn báº£n kÃ¨m thÃ´ng tin chi tiáº¿t.
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
    /// ThÃ´ng tin tiáº¿n Ä‘á»™ táº£i tá»‡p installer trong thá»i gian thá»±c.
    /// </summary>
    public class DownloadProgressReport
    {
        public int Percentage { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public double BytesPerSecond { get; set; }

        public string FormattedProgress => TotalBytes > 0 
            ? $"{BytesDownloaded / (1024.0 * 1024.0):F1} MB / {TotalBytes / (1024.0 * 1024.0):F1} MB ({Percentage}%)"
            : $"{BytesDownloaded / (1024.0 * 1024.0):F1} MB Ä‘Ã£ táº£i ({Percentage}%)";

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

