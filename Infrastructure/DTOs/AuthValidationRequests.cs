namespace QL_HocVien.Infrastructure.DTOs
{
    public class LoginValidationRequest
    {
        public string UsernameOrPhone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public LoginValidationRequest(string usernameOrPhone, string password)
        {
            UsernameOrPhone = usernameOrPhone;
            Password = password;
        }
    }

    public class RegisterValidationRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordValidationRequest
    {
        public string Identifier { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
