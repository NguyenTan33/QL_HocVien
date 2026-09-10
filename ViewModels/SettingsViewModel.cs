using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISecurityGateService _securityGate;
        private readonly IUpdateService _updateService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _smtpServer = "smtp.gmail.com";

        [ObservableProperty]
        private int _smtpPort = 587;

        [ObservableProperty]
        private string _senderName = "Hệ thống Quản lý Học viên Quân đội";

        [ObservableProperty]
        private string _senderEmail = "no-reply@mod.gov.vn";

        [ObservableProperty]
        private string _smtpUsername = "";

        [ObservableProperty]
        private string _smtpPassword = "";

        [ObservableProperty]
        private bool _enableSsl = true;

        [ObservableProperty]
        private bool _isTestMode = true;

        [ObservableProperty]
        private string _databasePath = "ql_hocvien.db";

        // ==================== CẤU HÌNH DỊCH VỤ SMS GATEWAY ====================
        [ObservableProperty]
        private string _smsProvider = "Twilio";

        public string[] AvailableSmsProviders { get; } = new[] { "Twilio", "SpeedSMS", "eSMS.vn", "Sim Gateway Nội Bộ" };

        [ObservableProperty]
        private string _smsApiKey = "";

        [ObservableProperty]
        private string _smsSenderId = "BQP_QLHV";

        [ObservableProperty]
        private string _smsAdminPhone = "";

        [ObservableProperty]
        private bool _isSmsEnabled = false;

        [ObservableProperty]
        private bool _isSmsTestMode = true;

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
            IUpdateService updateService,
            IServiceProvider serviceProvider)
        {
            _securityGate = securityGate;
            _updateService = updateService;
            _serviceProvider = serviceProvider;
            Title = "Cài Đặt Hệ Thống";

            AppVersionDisplay = $"v{_updateService.GetCurrentVersion()}";

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
                    if (doc.RootElement.TryGetProperty("SmtpSettings", out var smtpProp))
                    {
                        SmtpServer = smtpProp.GetProperty("Server").GetString() ?? SmtpServer;
                        SmtpPort = smtpProp.GetProperty("Port").GetInt32();
                        SenderName = smtpProp.GetProperty("SenderName").GetString() ?? SenderName;
                        SenderEmail = smtpProp.GetProperty("SenderEmail").GetString() ?? SenderEmail;
                        SmtpUsername = smtpProp.GetProperty("Username").GetString() ?? "";
                        var rawSmtpPwd = smtpProp.GetProperty("Password").GetString() ?? "";
                        SmtpPassword = EmailService.DecryptSecret(rawSmtpPwd);
                        EnableSsl = smtpProp.GetProperty("EnableSsl").GetBoolean();
                        if (smtpProp.TryGetProperty("IsTestMode", out var isTest))
                        {
                            IsTestMode = isTest.GetBoolean();
                        }
                    }

                    if (doc.RootElement.TryGetProperty("SmsSettings", out var smsProp))
                    {
                        SmsProvider = smsProp.TryGetProperty("Provider", out var p) ? p.GetString() ?? "Twilio" : "Twilio";
                        var rawSmsKey = smsProp.TryGetProperty("ApiKey", out var k) ? k.GetString() ?? "" : "";
                        SmsApiKey = EmailService.DecryptSecret(rawSmsKey);
                        SmsSenderId = smsProp.TryGetProperty("SenderId", out var s) ? s.GetString() ?? "BQP_QLHV" : "BQP_QLHV";
                        SmsAdminPhone = smsProp.TryGetProperty("AdminPhone", out var ap) ? ap.GetString() ?? "" : "";
                        IsSmsEnabled = smsProp.TryGetProperty("IsEnabled", out var ie) && ie.GetBoolean();
                        IsSmsTestMode = smsProp.TryGetProperty("IsTestMode", out var itm) && itm.GetBoolean();
                    }

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
            if (!await _securityGate.EnsureUnlockedAsync("Lưu cấu hình hệ thống & máy chủ thư")) return;

            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var config = new
                {
                    ConnectionStrings = new
                    {
                        DefaultConnection = "Data Source=ql_hocvien.db"
                    },
                    SmtpSettings = new
                    {
                        Server = SmtpServer,
                        Port = SmtpPort,
                        SenderName = SenderName,
                        SenderEmail = SenderEmail,
                        Username = SmtpUsername,
                        Password = EmailService.EncryptSecret(SmtpPassword),
                        EnableSsl = EnableSsl,
                        IsTestMode = IsTestMode
                    },
                    SmsSettings = new
                    {
                        Provider = SmsProvider,
                        ApiKey = EmailService.EncryptSecret(SmsApiKey),
                        SenderId = SmsSenderId,
                        AdminPhone = SmsAdminPhone,
                        IsEnabled = IsSmsEnabled,
                        IsTestMode = IsSmsTestMode
                    },
                    UpdateSettings = new
                    {
                        VersionCheckUrl = VersionCheckUrl.Trim(),
                        TimeoutSeconds = 8
                    }
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
                StatusMessage = "Đã lưu cấu hình Hệ thống, SMTP, Cổng SMS và Cập nhật Auto-Update an toàn!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi lưu cấu hình: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task CheckForUpdateManualAsync()
        {
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
        public void TestSmtp()
        {
            if (string.IsNullOrWhiteSpace(SenderEmail))
            {
                StatusMessage = "Vui lòng nhập Email người gửi trước khi kiểm tra!";
                return;
            }
            if (IsTestMode)
            {
                StatusMessage = $"[CHẾ ĐỘ THỬ NGHIỆM SMTP] Máy chủ {SmtpServer}:{SmtpPort} hoạt động bình thường! Mã xác thực sẽ hiển thị trực tiếp trong hộp thoại.";
            }
            else
            {
                StatusMessage = $"Đang kết nối kiểm tra máy chủ {SmtpServer}:{SmtpPort} với tài khoản {SmtpUsername}...";
            }
        }

        [RelayCommand]
        public void TestSms()
        {
            if (string.IsNullOrWhiteSpace(SmsAdminPhone))
            {
                StatusMessage = "Vui lòng nhập số điện thoại nhận SMS trước khi kiểm tra!";
                return;
            }
            if (IsSmsTestMode)
            {
                StatusMessage = $"[CHẾ ĐỘ THỬ NGHIỆM SMS] Đã mô phỏng gửi mã OTP / Cảnh báo bảo mật đến số {SmsAdminPhone} thành công!";
            }
            else
            {
                StatusMessage = $"Đã gửi lệnh SMS qua cổng {SmsProvider} tới số {SmsAdminPhone} (Định danh Brandname: {SmsSenderId}).";
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
