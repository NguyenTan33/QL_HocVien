using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _usernameOrPhone = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _rememberMe;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public event Action? OnLoginSuccess;
        public event Action? OnNavigateToRegister;
        public event Action? OnNavigateToForgotPassword;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Đăng Nhập - Hệ Thống Quản Lý Học Viên Quân Đội";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;

            try
            {
                var result = await _authService.LoginAsync(UsernameOrPhone, Password);
                if (result.Success)
                {
                    OnLoginSuccess?.Invoke();
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
        private void GoToRegister()
        {
            OnNavigateToRegister?.Invoke();
        }

        [RelayCommand]
        private void GoToForgotPassword()
        {
            OnNavigateToForgotPassword?.Invoke();
        }
    }
}
