using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace QL_HocVien.Services.Implementations
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
                // Äá»c cáº¥u hÃ¬nh tá»« appsettings.json
                var config = GetSmtpConfig();
                bool isTestMode = _overrideTestMode ?? config.IsTestMode;

                // Náº¿u lÃ  cháº¿ Ä‘á»™ Test (phá»¥c vá»¥ kiá»ƒm thá»­ ná»™i bá»™ tá»± Ä‘á»™ng)
                if (isTestMode)
                {
                    // An toÃ n: Ghi nháº­n thÃ nh cÃ´ng nhÆ°ng KHÃ”NG BAO GIá»œ Ä‘á»ƒ lá»™ OTP trong chuá»—i thÃ´ng bÃ¡o tráº£ vá»
                    return (true, "MÃ£ xÃ¡c thá»±c OTP Ä‘Ã£ Ä‘Æ°á»£c gá»­i thÃ nh cÃ´ng Ä‘áº¿n Ä‘á»‹a chá»‰ email cá»§a Ä‘á»“ng chÃ­. Vui lÃ²ng kiá»ƒm tra há»™p thÆ° Ä‘áº¿n.");
                }

                // Náº¿u chÆ°a cáº¥u hÃ¬nh thÃ´ng tin Ä‘Äƒng nháº­p SMTP
                if (string.IsNullOrWhiteSpace(config.Password) || string.IsNullOrWhiteSpace(config.Username))
                {
                    return (false, "MÃ¡y chá»§ gá»­i thÆ° (SMTP) chÆ°a Ä‘Æ°á»£c thiáº¿t láº­p tÃ i khoáº£n. Vui lÃ²ng liÃªn há»‡ Quáº£n trá»‹ viÃªn há»‡ thá»‘ng.");
                }

                // Gá»­i email thá»±c qua MailKit SMTP
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(config.SenderName, config.SenderEmail));
                message.To.Add(new MailboxAddress(recipientName, toEmail));
                message.Subject = $"[{otpCode}] MÃ£ xÃ¡c thá»±c Ä‘áº·t láº¡i máº­t kháº©u - Quáº£n lÃ½ Há»c viÃªn QuÃ¢n Ä‘á»™i";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #1e3a8a; border-radius: 8px;'>
                            <h2 style='color: #1e3a8a; text-align: center;'>Há»† THá»NG QUáº¢N LÃ Há»ŒC VIÃŠN QUÃ‚N Äá»˜I</h2>
                            <p>KÃ­nh gá»­i Ä‘á»“ng chÃ­: <strong>{recipientName}</strong>,</p>
                            <p>Há»‡ thá»‘ng nháº­n Ä‘Æ°á»£c yÃªu cáº§u Ä‘áº·t láº¡i máº­t kháº©u cho tÃ i khoáº£n liÃªn káº¿t vá»›i Ä‘á»‹a chá»‰ email nÃ y.</p>
                            <div style='background-color: #f1f5f9; padding: 15px; text-align: center; border-radius: 6px; margin: 20px 0;'>
                                <span style='font-size: 14px; color: #475569;'>MÃ£ xÃ¡c thá»±c OTP (hiá»‡u lá»±c trong 10 phÃºt):</span><br/>
                                <strong style='font-size: 28px; letter-spacing: 5px; color: #dc2626;'>{otpCode}</strong>
                            </div>
                            <p style='color: #64748b; font-size: 12px;'>Náº¿u Ä‘á»“ng chÃ­ khÃ´ng yÃªu cáº§u Ä‘áº·t láº¡i máº­t kháº©u, vui lÃ²ng bá» qua thÃ´ng bÃ¡o nÃ y Ä‘á»ƒ Ä‘áº£m báº£o an toÃ n thÃ´ng tin.</p>
                        </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(config.Server, config.Port, config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                await client.AuthenticateAsync(config.Username, config.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return (true, "MÃ£ xÃ¡c thá»±c OTP Ä‘Ã£ Ä‘Æ°á»£c gá»­i thÃ nh cÃ´ng Ä‘áº¿n Ä‘á»‹a chá»‰ email cá»§a Ä‘á»“ng chÃ­.");
            }
            catch (Exception ex)
            {
                // An toÃ n tuyá»‡t Ä‘á»‘i: KhÃ´ng tráº£ vá» mÃ£ OTP trong thÃ´ng bÃ¡o lá»—i khi gá»­i email tháº¥t báº¡i
                return (false, $"KhÃ´ng thá»ƒ káº¿t ná»‘i mÃ¡y chá»§ gá»­i thÆ° Ä‘á»ƒ chuyá»ƒn mÃ£ OTP ({ex.Message}). Vui lÃ²ng kiá»ƒm tra láº¡i Ä‘Æ°á»ng truyá»n máº¡ng hoáº·c cáº¥u hÃ¬nh SMTP.");
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
                            SenderName = smtpProp.GetProperty("SenderName").GetString() ?? "Há»‡ thá»‘ng Quáº£n lÃ½ Há»c viÃªn",
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
            public string SenderName { get; set; } = "Há»‡ thá»‘ng Quáº£n lÃ½ Há»c viÃªn QuÃ¢n Ä‘á»™i";
            public string SenderEmail { get; set; } = "no-reply@mod.gov.vn";
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public bool EnableSsl { get; set; } = true;
            public bool IsTestMode { get; set; } = false;
        }
    }
}

