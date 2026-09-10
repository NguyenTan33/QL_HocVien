using System.Threading.Tasks;

namespace QL_HocVien.Services.Interfaces
{
    public interface IEmailService
    {
        Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string otpCode, string recipientName);
        string? LastGeneratedOtp { get; } // Cho phÃ©p hiá»ƒn thá»‹ nhanh trong cháº¿ Ä‘á»™ thá»­ nghiá»‡m
    }
}

