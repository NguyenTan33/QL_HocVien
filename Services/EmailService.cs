using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace QL_HocVien.Services
{
    public class EmailService : IEmailService
    {
        public string? LastGeneratedOtp { get; private set; }

        public async Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string otpCode, string recipientName)
        {
            LastGeneratedOtp = otpCode;

            try
            {
                // Đọc cấu hình từ appsettings.json
                var config = GetSmtpConfig();

                // Nếu là chế độ Test hoặc chưa cấu hình password SMTP thực tế
                if (config.IsTestMode || string.IsNullOrWhiteSpace(config.Password) || string.IsNullOrWhiteSpace(config.Username))
                {
                    // Chế độ mô phỏng / thử nghiệm: Lưu mã và trả về thành công để người dùng test ngay mà không cần cấu hình email
                    return (true, $"[CHẾ ĐỘ THỬ NGHIỆM] Mã xác thực OTP của bạn là: {otpCode} (Hiệu lực 10 phút)");
                }

                // Gửi email thực qua MailKit SMTP
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(config.SenderName, config.SenderEmail));
                message.To.Add(new MailboxAddress(recipientName, toEmail));
                message.Subject = $"[{otpCode}] Mã xác thực đặt lại mật khẩu - Quản lý Học viên Quân đội";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #1e3a8a; border-radius: 8px;'>
                            <h2 style='color: #1e3a8a; text-align: center;'>HỆ THỐNG QUẢN LÝ HỌC VIÊN QUÂN ĐỘI</h2>
                            <p>Kính gửi đồng chí: <strong>{recipientName}</strong>,</p>
                            <p>Hệ thống nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với địa chỉ email này.</p>
                            <div style='background-color: #f1f5f9; padding: 15px; text-align: center; border-radius: 6px; margin: 20px 0;'>
                                <span style='font-size: 14px; color: #475569;'>Mã xác thực OTP (hiệu lực trong 10 phút):</span><br/>
                                <strong style='font-size: 28px; letter-spacing: 5px; color: #dc2626;'>{otpCode}</strong>
                            </div>
                            <p style='color: #64748b; font-size: 12px;'>Nếu đồng chí không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua thông báo này để đảm bảo an toàn thông tin.</p>
                        </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(config.Server, config.Port, config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                await client.AuthenticateAsync(config.Username, config.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return (true, "Mã xác thực OTP đã được gửi thành công đến email của bạn.");
            }
            catch (Exception ex)
            {
                // Fallback nếu kết nối SMTP ngoài đời gặp sự cố mạng
                return (true, $"Không thể kết nối máy chủ gửi mail ({ex.Message}). [CHẾ ĐỘ DỰ PHÒNG] Mã xác thực OTP là: {otpCode}");
            }
        }

        private SmtpConfig GetSmtpConfig()
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
                        return new SmtpConfig
                        {
                            Server = smtpProp.GetProperty("Server").GetString() ?? "smtp.gmail.com",
                            Port = smtpProp.GetProperty("Port").GetInt32(),
                            SenderName = smtpProp.GetProperty("SenderName").GetString() ?? "Hệ thống Quản lý Học viên",
                            SenderEmail = smtpProp.GetProperty("SenderEmail").GetString() ?? "no-reply@mod.gov.vn",
                            Username = smtpProp.GetProperty("Username").GetString() ?? "",
                            Password = smtpProp.GetProperty("Password").GetString() ?? "",
                            EnableSsl = smtpProp.GetProperty("EnableSsl").GetBoolean(),
                            IsTestMode = smtpProp.TryGetProperty("IsTestMode", out var isTest) && isTest.GetBoolean()
                        };
                    }
                }
            }
            catch
            {
                // Default
            }

            return new SmtpConfig();
        }

        private class SmtpConfig
        {
            public string Server { get; set; } = "smtp.gmail.com";
            public int Port { get; set; } = 587;
            public string SenderName { get; set; } = "Hệ thống Quản lý Học viên Quân đội";
            public string SenderEmail { get; set; } = "no-reply@mod.gov.vn";
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public bool EnableSsl { get; set; } = true;
            public bool IsTestMode { get; set; } = true;
        }
    }
}
