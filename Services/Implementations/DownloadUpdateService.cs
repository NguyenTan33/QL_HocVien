using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
{
    public class DownloadUpdateService : IDownloadUpdateService
    {
        private static readonly HttpClient HttpClient = new(new HttpClientHandler
        {
            AllowAutoRedirect = true
        })
        {
            Timeout = TimeSpan.FromMinutes(15) // Cho phÃ©p táº£i file dung lÆ°á»£ng lá»›n
        };

        static DownloadUpdateService()
        {
            // GitHub yÃªu cáº§u báº¯t buá»™c User-Agent há»£p lá»‡ Ä‘á»ƒ khÃ´ng bá»‹ cháº·n mÃ£ lá»—i 403 Forbidden
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
                throw new ArgumentException("ÄÆ°á»ng dáº«n táº£i báº£n cáº­p nháº­t khÃ´ng há»£p lá»‡.", nameof(downloadUrl));
            }

            // Láº¥y tÃªn tá»‡p gá»‘c tá»« URL táº£i vá»
            string originalFileName = "update.exe";
            string extension = ".exe";
            try
            {
                var uri = new Uri(downloadUrl);
                string pathFileName = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrWhiteSpace(pathFileName) && pathFileName.Contains('.'))
                {
                    originalFileName = pathFileName;
                    extension = Path.GetExtension(pathFileName);
                }
            }
            catch { }

            string tempFileName = $"QLHV_{DateTime.Now:yyyyMMdd_HHmmss}_{originalFileName}";
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
                // Cáº­p nháº­t giao diá»‡n má»—i ~150ms Ä‘á»ƒ tá»‘i Æ°u hiá»‡u nÄƒng UI
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

            // Äáº£m báº£o bÃ¡o cÃ¡o 100% khi káº¿t thÃºc
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
                throw new FileNotFoundException("KhÃ´ng tÃ¬m tháº¥y tá»‡p cÃ i Ä‘áº·t Ä‘á»ƒ khá»Ÿi cháº¡y.", installerPath);
            }

            string currentExePath = Environment.ProcessPath 
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QL_HocVien.exe");

            string fileName = Path.GetFileName(installerPath);
            bool isInstaller = fileName.Contains("setup", StringComparison.OrdinalIgnoreCase) 
                               || fileName.Contains("install", StringComparison.OrdinalIgnoreCase)
                               || fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase);

            bool isDirectExeReplacement = fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !isInstaller;

            try
            {
                if (isDirectExeReplacement && File.Exists(currentExePath))
                {
                    // TrÆ°á»ng há»£p cáº­p nháº­t file .exe cháº¡y trá»±c tiáº¿p (Portable / Single-File)
                    // Windows khÃ³a tá»‡p .exe Ä‘ang cháº¡y, do Ä‘Ã³ dÃ¹ng script batch ngáº§m Ä‘á»ƒ copy Ä‘Ã¨ sau khi á»©ng dá»¥ng Ä‘Ã³ng
                    string currentProcessName = Path.GetFileNameWithoutExtension(currentExePath);
                    string batchScriptPath = Path.Combine(Path.GetTempPath(), $"update_qlhv_{DateTime.Now:yyyyMMddHHmmss}.bat");
                    string batchContent = $@"@echo off
setlocal
chcp 65001 > NUL

:: Äá»£i tiáº¿n trÃ¬nh cÅ© Ä‘Ã³ng háº³n
timeout /t 1 /nobreak > NUL

set count=0
:wait_loop
tasklist /fi ""imagename eq {currentProcessName}.exe"" 2>NUL | findstr /i ""{currentProcessName}.exe"" > NUL
if %errorlevel% equ 0 (
    timeout /t 1 /nobreak > NUL
    set /a count+=1
    if %count% leq 10 goto wait_loop
)

:: Copy Ä‘Ã¨ file má»›i
copy /y ""{installerPath}"" ""{currentExePath}"" > NUL
if exist ""{installerPath}"" del /f /q ""{installerPath}"" > NUL

:: Khá»Ÿi Ä‘á»™ng á»©ng dá»¥ng má»›i
start """" ""{currentExePath}""

:: Tá»± xÃ³a file batch
del /f /q ""%~f0"" > NUL
";
                    File.WriteAllText(batchScriptPath, batchContent);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"\"{batchScriptPath}\"\"",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
                else
                {
                    // TrÆ°á»ng há»£p tá»‡p lÃ  bá»™ cÃ i Ä‘áº·t (Installer/Setup/MSI)
                    var psi = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(installerPath) ?? string.Empty
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"KhÃ´ng thá»ƒ khá»Ÿi cháº¡y báº£n cáº­p nháº­t:\n{ex.Message}",
                                "Lá»—i Cáº­p Nháº­t", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ÄÃ³ng á»©ng dá»¥ng hiá»‡n táº¡i an toÃ n
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

