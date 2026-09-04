using System.Threading.Tasks;

namespace QL_HocVien.Services
{
    public interface IEmailService
    {
        Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string otpCode, string recipientName);
        string? LastGeneratedOtp { get; } // Cho phép hiển thị nhanh trong chế độ thử nghiệm
    }
}
