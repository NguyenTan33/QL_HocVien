using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class ForgotPasswordViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _identifier = string.Empty; // Username hoặc Số điện thoại

        [ObservableProperty]
        private bool _isAccountFound;

        [ObservableProperty]
        private string? _passwordHint;

        [ObservableProperty]
        private string? _securityQuestion;

        [ObservableProperty]
        private string _securityAnswer = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmNewPassword = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _infoMessage = string.Empty;

        public event Action? OnNavigateToLogin;

        public ForgotPasswordViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Khôi Phục Mật Khẩu";
        }

        [RelayCommand]
        private async Task LookupAccountAsync()
        {
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Identifier))
            {
                ErrorMessage = "Vui lòng nhập Tên tài khoản hoặc Số điện thoại.";
                return;
            }

            IsBusy = true;

            try
            {
                var result = await _authService.GetAccountRecoveryInfoAsync(Identifier);
                if (result.Success)
                {
                    IsAccountFound = true;
                    PasswordHint = result.PasswordHint;
                    SecurityQuestion = result.SecurityQuestion;
                    InfoMessage = string.IsNullOrWhiteSpace(result.PasswordHint)
                        ? "Đã tìm thấy tài khoản. Vui lòng trả lời câu hỏi bảo mật để đổi mật khẩu."
                        : "Đã tìm thấy tài khoản. Hãy xem gợi ý mật khẩu bên dưới hoặc trả lời câu hỏi bảo mật để đặt lại mật khẩu.";
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SecurityAnswer))
            {
                ErrorMessage = "Vui lòng nhập câu trả lời cho câu hỏi bảo mật.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return;
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return;
            }

            IsBusy = true;

            try
            {
                var result = await _authService.ResetPasswordWithSecurityAnswerAsync(Identifier, SecurityAnswer, NewPassword);
                if (result.Success)
                {
                    InfoMessage = result.Message;
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ResetLookup()
        {
            IsAccountFound = false;
            PasswordHint = null;
            SecurityQuestion = null;
            SecurityAnswer = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;
        }

        [RelayCommand]
        private void GoToLogin()
        {
            OnNavigateToLogin?.Invoke();
        }
    }
}
