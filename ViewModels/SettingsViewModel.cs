using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace QL_HocVien.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
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

        public SettingsViewModel()
        {
            Title = "Cài Đặt Hệ Thống";
            LoadSettings();
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
                        SmtpPassword = smtpProp.GetProperty("Password").GetString() ?? "";
                        EnableSsl = smtpProp.GetProperty("EnableSsl").GetBoolean();
                        if (smtpProp.TryGetProperty("IsTestMode", out var isTest))
                        {
                            IsTestMode = isTest.GetBoolean();
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
        public void SaveSettings()
        {
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
                        Password = SmtpPassword,
                        EnableSsl = EnableSsl,
                        IsTestMode = IsTestMode
                    }
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
                StatusMessage = "Lưu cấu hình hệ thống thành công!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi lưu cấu hình: {ex.Message}";
            }
        }
    }
}
