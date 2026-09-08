using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QL_HocVien.Models;
using QL_HocVien.Services;

namespace QL_HocVien.Views.Windows
{
    public partial class UpdateWindow : Window
    {
        private readonly IDownloadUpdateService _downloadUpdateService;
        private UpdateCheckResult? _updateResult;
        private CancellationTokenSource? _downloadCts;
        private bool _isDownloading;

        public UpdateWindow(IDownloadUpdateService downloadUpdateService)
        {
            InitializeComponent();
            _downloadUpdateService = downloadUpdateService;

            MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    DragMove();
                }
            };
        }

        public void Initialize(UpdateCheckResult updateResult)
        {
            _updateResult = updateResult;

            txtCurrentVersion.Text = $"v{updateResult.CurrentVersion}";
            txtLatestVersion.Text = $"v{updateResult.LatestVersion}";

            string notes = updateResult.UpdateInfo?.ReleaseNotes ?? string.Empty;
            txtReleaseNotes.Text = string.IsNullOrWhiteSpace(notes) 
                ? "Bản cập nhật bao gồm các sửa lỗi, nâng cấp bảo mật và cải tiến hiệu năng tổng thể." 
                : notes;

            if (updateResult.IsMandatory)
            {
                txtBadgeText.Text = "BẮT BUỘC";
                bdMandatoryBadge.Background = new SolidColorBrush(Color.FromRgb(127, 29, 29));
                bdMandatoryBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                txtBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(254, 226, 226));

                btnSecondary.Content = "Thoát ứng dụng";
                btnCloseHeader.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtBadgeText.Text = "TÙY CHỌN";
                bdMandatoryBadge.Background = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                bdMandatoryBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                txtBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(219, 234, 254));

                btnSecondary.Content = "Để sau";
                btnCloseHeader.Visibility = Visibility.Visible;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            HandleCloseOrExit();
        }

        private void BtnSecondary_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
            {
                // Hủy tải
                _downloadCts?.Cancel();
                _isDownloading = false;
                spDownloadProgress.Visibility = Visibility.Collapsed;
                btnUpdate.IsEnabled = true;
                btnSecondary.Content = _updateResult?.IsMandatory == true ? "Thoát ứng dụng" : "Để sau";
                txtErrorMessage.Text = "Đã hủy tiến trình tải bản cập nhật.";
                return;
            }

            HandleCloseOrExit();
        }

        private void HandleCloseOrExit()
        {
            if (_updateResult?.IsMandatory == true)
            {
                DialogResult = false;
                if (Application.Current != null)
                {
                    Application.Current.Shutdown(0);
                }
                else
                {
                    Environment.Exit(0);
                }
                return;
            }

            DialogResult = false;
            Close();
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_updateResult?.UpdateInfo == null || string.IsNullOrWhiteSpace(_updateResult.UpdateInfo.DownloadUrl))
            {
                txtErrorMessage.Text = "Không tìm thấy đường dẫn tải bản cài đặt.";
                return;
            }

            txtErrorMessage.Text = string.Empty;
            _isDownloading = true;
            btnUpdate.IsEnabled = false;
            btnSecondary.Content = "Hủy tải";

            spDownloadProgress.Visibility = Visibility.Visible;
            pbDownload.Value = 0;
            txtDownloadStatus.Text = "Đang kết nối máy chủ tải bản cập nhật...";
            txtDownloadSpeed.Text = "-- MB/s";
            txtDownloadSize.Text = "0 MB / 0 MB (0%)";

            _downloadCts = new CancellationTokenSource();

            var progress = new Progress<DownloadProgressReport>(report =>
            {
                pbDownload.Value = report.Percentage;
                txtDownloadSize.Text = report.FormattedProgress;
                txtDownloadSpeed.Text = report.FormattedSpeed;
                txtDownloadStatus.Text = $"Đang tải gói cập nhật ({report.Percentage}%)...";
            });

            try
            {
                string installerPath = await _downloadUpdateService.DownloadInstallerAsync(
                    _updateResult.UpdateInfo.DownloadUrl, 
                    progress, 
                    _downloadCts.Token);

                txtDownloadStatus.Text = "Tải hoàn tất! Đang chuẩn bị khởi chạy bộ cài đặt...";
                pbDownload.Value = 100;
                await Task.Delay(1000);

                DialogResult = true;
                _downloadUpdateService.LaunchInstallerAndExit(installerPath);
            }
            catch (OperationCanceledException)
            {
                txtErrorMessage.Text = "Đã hủy tải bản cập nhật.";
                spDownloadProgress.Visibility = Visibility.Collapsed;
                btnUpdate.IsEnabled = true;
                btnSecondary.Content = _updateResult?.IsMandatory == true ? "Thoát ứng dụng" : "Để sau";
                _isDownloading = false;
            }
            catch (Exception ex)
            {
                txtErrorMessage.Text = $"Lỗi khi tải bản cập nhật: {ex.Message}";
                spDownloadProgress.Visibility = Visibility.Collapsed;
                btnUpdate.IsEnabled = true;
                btnSecondary.Content = _updateResult?.IsMandatory == true ? "Thoát ứng dụng" : "Để sau";
                _isDownloading = false;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // Với bản cập nhật bắt buộc: Nếu cửa sổ bị đóng mà chưa bấm cập nhật, lập tức thoát app
            if (_updateResult?.IsMandatory == true && DialogResult != true)
            {
                if (Application.Current != null)
                {
                    Application.Current.Shutdown(0);
                }
            }
        }
    }
}
