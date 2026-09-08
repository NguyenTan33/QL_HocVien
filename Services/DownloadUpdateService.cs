using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public class DownloadUpdateService : IDownloadUpdateService
    {
        private static readonly HttpClient HttpClient = new(new HttpClientHandler
        {
            AllowAutoRedirect = true
        })
        {
            Timeout = TimeSpan.FromMinutes(15) // Cho phép tải file dung lượng lớn
        };

        static DownloadUpdateService()
        {
            // GitHub yêu cầu bắt buộc User-Agent hợp lệ để không bị chặn mã lỗi 403 Forbidden
            if (!HttpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                HttpClient.DefaultRequestHeaders.Add("User-Agent", "QL_HocVien-AutoUpdater/1.0");
            }
        }

        public async Task<string> DownloadInstallerAsync(
            string downloadUrl, 
            IProgress<DownloadProgressReport>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new ArgumentException("Đường dẫn tải bản cập nhật không hợp lệ.", nameof(downloadUrl));
            }

            // Xác định phần mở rộng tệp (.exe, .msi, v.v.)
            string extension = ".exe";
            try
            {
                var uri = new Uri(downloadUrl);
                string pathExt = Path.GetExtension(uri.AbsolutePath);
                if (!string.IsNullOrWhiteSpace(pathExt))
                {
                    extension = pathExt;
                }
            }
            catch { }

            string tempFileName = $"QL_HocVien_Setup_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            string destinationPath = Path.Combine(Path.GetTempPath(), tempFileName);

            using var response = await HttpClient.GetAsync(
                downloadUrl, 
                HttpCompletionOption.ResponseHeadersRead, 
                cancellationToken);

            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            long totalBytesVal = totalBytes ?? -1L;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(
                destinationPath, 
                FileMode.Create, 
                FileAccess.Write, 
                FileShare.None, 
                81920, 
                true);

            byte[] buffer = new byte[81920]; // 80 KB buffer
            long totalReadBytes = 0;
            int readBytes;

            var stopwatch = Stopwatch.StartNew();
            long lastReportBytes = 0;
            long lastReportTimeMs = 0;

            var progressReport = new DownloadProgressReport
            {
                TotalBytes = totalBytesVal
            };

            while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, readBytes, cancellationToken);
                totalReadBytes += readBytes;

                long elapsedMs = stopwatch.ElapsedMilliseconds;
                // Cập nhật giao diện mỗi ~150ms để tối ưu hiệu năng UI
                if (elapsedMs - lastReportTimeMs >= 150 || (totalBytesVal > 0 && totalReadBytes == totalBytesVal))
                {
                    double timeDiffSec = (elapsedMs - lastReportTimeMs) / 1000.0;
                    long bytesDiff = totalReadBytes - lastReportBytes;
                    double speed = timeDiffSec > 0 ? (bytesDiff / timeDiffSec) : 0;

                    int percent = totalBytesVal > 0 
                        ? (int)Math.Clamp((totalReadBytes * 100.0) / totalBytesVal, 0, 100) 
                        : 0;

                    progressReport.BytesDownloaded = totalReadBytes;
                    progressReport.Percentage = percent;
                    progressReport.BytesPerSecond = speed;

                    progress?.Report(progressReport);

                    lastReportTimeMs = elapsedMs;
                    lastReportBytes = totalReadBytes;
                }
            }

            // Đảm bảo báo cáo 100% khi kết thúc
            progressReport.BytesDownloaded = totalReadBytes;
            progressReport.Percentage = 100;
            progressReport.BytesPerSecond = 0;
            progress?.Report(progressReport);

            return destinationPath;
        }

        public void LaunchInstallerAndExit(string installerPath)
        {
            if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
            {
                throw new FileNotFoundException("Không tìm thấy tệp cài đặt để khởi chạy.", installerPath);
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(installerPath) ?? string.Empty
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể khởi chạy bộ cài đặt:\n{ex.Message}",
                                "Lỗi Cập Nhật", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Đóng ứng dụng hiện tại an toàn
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown(0);
                });
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
