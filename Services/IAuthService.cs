using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        Task<(bool Success, string Message, User? User)> LoginAsync(string usernameOrPhone, string password);
        Task<(bool Success, string Message)> RegisterAsync(string username, string fullName, string phoneNumber, string email, string password);
        Task<(bool Success, string Message, string? Otp)> RequestPasswordResetOtpAsync(string identifier);
        Task<(bool Success, string Message)> ResetPasswordWithOtpAsync(string email, string otpCode, string newPassword);
        Task<(bool Success, string Message)> ResetCadetPasswordAsync(int cadetId, string newPassword);
        void Logout();
    }
}
