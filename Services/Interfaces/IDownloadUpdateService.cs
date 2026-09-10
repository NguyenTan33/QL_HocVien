using System;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Interfaces
{
    public interface IDownloadUpdateService
    {
        /// <summary>
        /// Tải tệp cài đặt cập nhật từ URL và báo cáo tiến độ theo thời gian thực.
        /// </summary>
        Task<string> DownloadInstallerAsync(
            string downloadUrl, 
            IProgress<DownloadProgressReport>? progress = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Khởi chạy bộ cài đặt đã tải về và đóng ứng dụng hiện tại.
        /// </summary>
        void LaunchInstallerAndExit(string installerPath);
    }
}
