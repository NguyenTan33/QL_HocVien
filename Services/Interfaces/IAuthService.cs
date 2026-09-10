using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Interfaces
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        Task<(bool Success, string Message, User? User)> LoginAsync(string usernameOrPhone, string password);
        
        Task<(bool Success, string Message)> RegisterAsync(
            string username, 
            string fullName, 
            string phoneNumber, 
            string password, 
            string securityQuestion, 
            string securityAnswer, 
            string? passwordHint = null, 
            string? email = null);

        // Khôi phục mật khẩu 100% Offline qua Gợi ý & Câu hỏi bảo mật (Không dùng Email OTP / SMS)
        Task<(bool Success, string Message, string? PasswordHint, string? SecurityQuestion)> GetAccountRecoveryInfoAsync(string identifier);
        Task<(bool Success, string Message)> ResetPasswordWithSecurityAnswerAsync(string identifier, string securityAnswer, string newPassword);

        Task<(bool Success, string Message)> ResetCadetPasswordAsync(int cadetId, string newPassword);
        void Logout();
    }
}
