using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace QL_HocVien.Services
{
    public class EmailService : IEmailService
    {
        private readonly bool? _overrideTestMode;

        public string? LastGeneratedOtp { get; private set; }

        public EmailService(bool? isTestMode = null)
        {
            _overrideTestMode = isTestMode;
        }

        public async Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string otpCode, string recipientName)
        {
            LastGeneratedOtp = otpCode;

            try
            {
                // Đọc cấu hình từ appsettings.json
                var config = GetSmtpConfig();
                bool isTestMode = _overrideTestMode ?? config.IsTestMode;

                // Nếu là chế độ Test (phục vụ kiểm thử nội bộ tự động)
                if (isTestMode)
                {
                    // An toàn: Ghi nhận thành công nhưng KHÔNG BAO GIỜ để lộ OTP trong chuỗi thông báo trả về
                    return (true, "Mã xác thực OTP đã được gửi thành công đến địa chỉ email của đồng chí. Vui lòng kiểm tra hộp thư đến.");
                }

                // Nếu chưa cấu hình thông tin đăng nhập SMTP
                if (string.IsNullOrWhiteSpace(config.Password) || string.IsNullOrWhiteSpace(config.Username))
                {
                    return (false, "Máy chủ gửi thư (SMTP) chưa được thiết lập tài khoản. Vui lòng liên hệ Quản trị viên hệ thống.");
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

                return (true, "Mã xác thực OTP đã được gửi thành công đến địa chỉ email của đồng chí.");
            }
            catch (Exception ex)
            {
                // An toàn tuyệt đối: Không trả về mã OTP trong thông báo lỗi khi gửi email thất bại
                return (false, $"Không thể kết nối máy chủ gửi thư để chuyển mã OTP ({ex.Message}). Vui lòng kiểm tra lại đường truyền mạng hoặc cấu hình SMTP.");
            }
        }

        public static string EncryptSecret(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
                byte[] enc = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return "enc:" + Convert.ToBase64String(enc);
            }
            catch
            {
                return plain;
            }
        }

        public static string DecryptSecret(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return string.Empty;
            if (!cipher.StartsWith("enc:")) return cipher;
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipher.Substring(4));
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty;
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
                        var rawPwd = smtpProp.GetProperty("Password").GetString() ?? "";
                        return new SmtpConfig
                        {
                            Server = smtpProp.GetProperty("Server").GetString() ?? "smtp.gmail.com",
                            Port = smtpProp.GetProperty("Port").GetInt32(),
                            SenderName = smtpProp.GetProperty("SenderName").GetString() ?? "Hệ thống Quản lý Học viên",
                            SenderEmail = smtpProp.GetProperty("SenderEmail").GetString() ?? "no-reply@mod.gov.vn",
                            Username = smtpProp.GetProperty("Username").GetString() ?? "",
                            Password = DecryptSecret(rawPwd),
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
            public bool IsTestMode { get; set; } = false;
        }
    }
}
