using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISecurityGateService _securityGate;
        private readonly IUpdateService? _updateService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthService? _authService;

        [ObservableProperty]
        private string _databasePath = "ql_hocvien.db";

        // ==================== ĐỔI MẬT KHẨU TÀI KHOẢN ĐĂNG NHẬP (OFFLINE) ====================
        public string CurrentUsername => _authService?.CurrentUser?.Username ?? "admin";
        public string CurrentFullName => _authService?.CurrentUser?.FullName ?? "Quản trị viên";
        public string CurrentRole => _authService?.CurrentUser?.Role ?? "Admin";

        [ObservableProperty]
        private string _accountOldPassword = string.Empty;

        [ObservableProperty]
        private string _accountNewPassword = string.Empty;

        [ObservableProperty]
        private string _accountConfirmPassword = string.Empty;

        [ObservableProperty]
        private string _accountPasswordMessage = string.Empty;

        [ObservableProperty]
        private bool _isAccountPasswordSuccess;

        [RelayCommand]
        public async Task ChangeAccountPasswordAsync()
        {
            AccountPasswordMessage = string.Empty;
            if (_authService == null)
            {
                IsAccountPasswordSuccess = false;
                AccountPasswordMessage = "Dịch vụ xác thực tài khoản không khả dụng.";
                return;
            }

            if (string.IsNullOrWhiteSpace(AccountOldPassword))
            {
                IsAccountPasswordSuccess = false;
                AccountPasswordMessage = "Vui lòng nhập mật khẩu hiện tại của tài khoản!";
                return;
            }

            if (string.IsNullOrWhiteSpace(AccountNewPassword))
            {
                IsAccountPasswordSuccess = false;
                AccountPasswordMessage = "Vui lòng nhập mật khẩu mới!";
                return;
            }

            if (AccountNewPassword.Length < 6)
            {
                IsAccountPasswordSuccess = false;
                AccountPasswordMessage = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return;
            }

            if (AccountNewPassword != AccountConfirmPassword)
            {
                IsAccountPasswordSuccess = false;
                AccountPasswordMessage = "Xác nhận mật khẩu mới không khớp!";
                return;
            }

            var result = await _authService.ChangePasswordAsync(AccountOldPassword, AccountNewPassword);
            IsAccountPasswordSuccess = result.Success;
            AccountPasswordMessage = result.Message;

            if (result.Success)
            {
                AccountOldPassword = string.Empty;
                AccountNewPassword = string.Empty;
                AccountConfirmPassword = string.Empty;
            }
        }

        // ==================== CẤU HÌNH TỰ ĐỘNG CẬP NHẬT (AUTO-UPDATE) ====================
        [ObservableProperty]
        private string _appVersionDisplay = "v1.0.0";

        [ObservableProperty]
        private string _versionCheckUrl = "https://raw.githubusercontent.com/NguyenTan33/QL_HocVien/main/version.json";

        [ObservableProperty]
        private string _updateStatusText = string.Empty;

        [ObservableProperty]
        private bool _isCheckingUpdate;

        // ==================== BẢO MẬT CẤP 2 (KHÓA RƯƠNG) ====================
        [ObservableProperty]
        private bool _isProtectionEnabled;

        [ObservableProperty]
        private bool _isUnlocked;

        [ObservableProperty]
        private string _securityStatusDisplay = string.Empty;

        [ObservableProperty]
        private string _enablePassword = string.Empty;

        [ObservableProperty]
        private string _enableConfirmPassword = string.Empty;

        [ObservableProperty]
        private string _disablePassword = string.Empty;

        [ObservableProperty]
        private string _changeOldPassword = string.Empty;

        [ObservableProperty]
        private string _changeNewPassword = string.Empty;

        [ObservableProperty]
        private string _changeConfirmPassword = string.Empty;

        [ObservableProperty]
        private string _securityMessage = string.Empty;

        [ObservableProperty]
        private bool _isSecuritySuccess;

        public SettingsViewModel(
            ISecurityGateService securityGate,
            IUpdateService? updateService,
            IServiceProvider serviceProvider,
            IAuthService? authService = null)
        {
            _securityGate = securityGate;
            _updateService = updateService;
            _serviceProvider = serviceProvider;
            _authService = authService;
            Title = "Cài Đặt Hệ Thống";

            AppVersionDisplay = _updateService != null ? $"v{_updateService.GetCurrentVersion()}" : "v1.0.0";

            LoadSettings();

            _securityGate.OnSecurityStateChanged += RefreshSecurityState;
            RefreshSecurityState();
        }

        private void RefreshSecurityState()
        {
            IsProtectionEnabled = _securityGate.IsProtectionEnabled;
            IsUnlocked = _securityGate.IsUnlocked;

            if (!IsProtectionEnabled)
            {
                SecurityStatusDisplay = "⚪ Khóa bảo mật Cấp 2 đang TẮT (Cho phép mọi thao tác)";
            }
            else if (IsUnlocked)
            {
                SecurityStatusDisplay = $"🔓 Đang MỞ KHÓA tạm thời (Thời gian tự do còn lại: {_securityGate.FormattedRemainingTime})";
            }
            else
            {
                SecurityStatusDisplay = "🔒 Đang KHÓA BẢO VỆ (Chỉ xem - Thao tác thêm/sửa/xóa/xuất cần mật khẩu)";
            }
        }

        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("UpdateSettings", out var updateProp))
                    {
                        if (updateProp.TryGetProperty("VersionCheckUrl", out var urlProp) && !string.IsNullOrWhiteSpace(urlProp.GetString()))
                        {
                            VersionCheckUrl = urlProp.GetString()!;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Không thể tải cấu hình: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task SaveSettingsAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Lưu cấu hình hệ thống")) return;

            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var config = new
                {
                    ConnectionStrings = new
                    {
                        DefaultConnection = "Data Source=ql_hocvien.db"
                    },
                    UpdateSettings = new
                    {
                        VersionCheckUrl = VersionCheckUrl.Trim(),
                        TimeoutSeconds = 8
                    }
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
                StatusMessage = "Đã lưu cấu hình Hệ thống và Cập nhật Auto-Update an toàn!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi lưu cấu hình: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task CheckForUpdateManualAsync()
        {
            if (_updateService == null)
            {
                UpdateStatusText = "Dịch vụ cập nhật không khả dụng.";
                return;
            }

            IsCheckingUpdate = true;
            UpdateStatusText = "Đang kết nối máy chủ để kiểm tra bản cập nhật mới...";

            try
            {
                var result = await _updateService.CheckForUpdateAsync();
                if (result.HasUpdate)
                {
                    UpdateStatusText = $"Phát hiện phiên bản mới: v{result.LatestVersion} (Hiện tại: {AppVersionDisplay})";
                    var updateWindow = (Views.Windows.UpdateWindow)_serviceProvider.GetService(typeof(Views.Windows.UpdateWindow))!;
                    if (updateWindow != null)
                    {
                        updateWindow.Initialize(result);
                        updateWindow.ShowDialog();
                    }
                }
                else if (result.Status == UpdateStatus.UpToDate)
                {
                    UpdateStatusText = $"Hệ thống đang hoạt động ở phiên bản mới nhất ({AppVersionDisplay}). Không có bản cập nhật nào.";
                }
                else
                {
                    UpdateStatusText = $"Không thể kiểm tra: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText = $"Lỗi kiểm tra cập nhật: {ex.Message}";
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        }

        [RelayCommand]
        public void EnableProtection()
        {
            SecurityMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(EnablePassword))
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Vui lòng nhập mật khẩu muốn cài đặt!";
                return;
            }

            if (EnablePassword.Length < 4)
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Mật khẩu bảo mật phải có ít nhất 4 ký tự!";
                return;
            }

            if (EnablePassword != EnableConfirmPassword)
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Mật khẩu xác nhận không khớp!";
                return;
            }

            var ok = _securityGate.EnableProtection(EnablePassword);
            if (ok)
            {
                IsSecuritySuccess = true;
                SecurityMessage = "Đã kích hoạt Khóa bảo mật cấp 2 thành công! Hệ thống được mở khóa tự do trong 5 phút.";
                EnablePassword = string.Empty;
                EnableConfirmPassword = string.Empty;
                RefreshSecurityState();
            }
            else
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Không thể kích hoạt bảo mật. Vui lòng thử lại!";
            }
        }

        [RelayCommand]
        public void DisableProtection()
        {
            SecurityMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(DisablePassword))
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Vui lòng nhập mật khẩu hiện tại để tắt bảo mật!";
                return;
            }

            var ok = _securityGate.DisableProtection(DisablePassword);
            if (ok)
            {
                IsSecuritySuccess = true;
                SecurityMessage = "Đã tắt Khóa bảo mật cấp 2. Hệ thống hiện không yêu cầu mật khẩu thao tác.";
                DisablePassword = string.Empty;
                RefreshSecurityState();
            }
            else
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Mật khẩu hiện tại không chính xác!";
            }
        }

        [RelayCommand]
        public void ChangePassword()
        {
            SecurityMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(ChangeOldPassword))
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Vui lòng nhập mật khẩu hiện tại!";
                return;
            }

            if (string.IsNullOrWhiteSpace(ChangeNewPassword))
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Vui lòng nhập mật khẩu mới!";
                return;
            }

            if (ChangeNewPassword.Length < 4)
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Mật khẩu mới phải có ít nhất 4 ký tự!";
                return;
            }

            if (ChangeNewPassword != ChangeConfirmPassword)
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Xác nhận mật khẩu mới không khớp!";
                return;
            }

            var ok = _securityGate.ChangePassword(ChangeOldPassword, ChangeNewPassword);
            if (ok)
            {
                IsSecuritySuccess = true;
                SecurityMessage = "Đổi mật khẩu cấp 2 thành công! Bạn có 5 phút thao tác tự do.";
                ChangeOldPassword = string.Empty;
                ChangeNewPassword = string.Empty;
                ChangeConfirmPassword = string.Empty;
                RefreshSecurityState();
            }
            else
            {
                IsSecuritySuccess = false;
                SecurityMessage = "Mật khẩu hiện tại không chính xác!";
            }
        }

        [RelayCommand]
        public void LockNow()
        {
            _securityGate.LockNow();
            IsSecuritySuccess = true;
            SecurityMessage = "Đã khóa bảo mật ngay lập tức! Thao tác tiếp theo sẽ yêu cầu mật khẩu.";
            RefreshSecurityState();
        }
    }
}
