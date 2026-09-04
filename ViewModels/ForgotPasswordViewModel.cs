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
        private string _identifier = string.Empty; // Email hoặc Username hoặc Số điện thoại

        [ObservableProperty]
        private string _targetEmail = string.Empty;

        [ObservableProperty]
        private string _otpCode = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmNewPassword = string.Empty;

        [ObservableProperty]
        private bool _isOtpSent;

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
        private async Task SendOtpAsync()
        {
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Identifier))
            {
                ErrorMessage = "Vui lòng nhập Email, Tên tài khoản hoặc Số điện thoại.";
                return;
            }

            IsBusy = true;

            try
            {
                var result = await _authService.RequestPasswordResetOtpAsync(Identifier);
                if (result.Success)
                {
                    IsOtpSent = true;
                    InfoMessage = result.Message;
                    if (Identifier.Contains("@"))
                    {
                        TargetEmail = Identifier.Trim();
                    }
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

            if (string.IsNullOrWhiteSpace(TargetEmail))
            {
                ErrorMessage = "Vui lòng nhập địa chỉ Email nhận mã xác thực.";
                return;
            }

            if (string.IsNullOrWhiteSpace(OtpCode))
            {
                ErrorMessage = "Vui lòng nhập mã xác thực OTP.";
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
                var result = await _authService.ResetPasswordWithOtpAsync(TargetEmail, OtpCode, NewPassword);
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
        private void GoToLogin()
        {
            OnNavigateToLogin?.Invoke();
        }
    }
}
