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
        private readonly IPasskeyService _passkeyService;

        [ObservableProperty]
        private string _usernameOrPhone = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _rememberMe;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // Trạng thái dùng thử & Passkey
        [ObservableProperty]
        private string _trialStatusText = string.Empty;

        [ObservableProperty]
        private bool _isTrialExpired;

        [ObservableProperty]
        private bool _isPasskeyModalVisible;

        [ObservableProperty]
        private string _passkeyUsername = string.Empty;

        [ObservableProperty]
        private string _passkeyInput = string.Empty;

        [ObservableProperty]
        private string _passkeyStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isPasskeySuccess;

        public event Action? OnLoginSuccess;
        public event Action? OnNavigateToRegister;
        public event Action? OnNavigateToForgotPassword;

        public LoginViewModel(IAuthService authService, IPasskeyService passkeyService)
        {
            _authService = authService;
            _passkeyService = passkeyService;
            Title = "Đăng Nhập - Hệ Thống Quản Lý Học Viên Quân Đội";

            UpdateTrialStatus();
        }

        public void UpdateTrialStatus()
        {
            if (_passkeyService.IsTrialActive)
            {
                var remaining = _passkeyService.TrialExpirationDate - DateTime.Now;
                int days = Math.Max(0, (int)remaining.TotalDays);
                int hours = Math.Max(0, remaining.Hours);
                TrialStatusText = $"★ Dùng thử miễn phí đến 23:59 ngày 12/09/2026 (Còn {days} ngày {hours} giờ)";
                IsTrialExpired = false;
            }
            else
            {
                TrialStatusText = "⚠️ Đã hết hạn trải nghiệm miễn phí (12/09/2026). Yêu cầu Passkey kích hoạt.";
                IsTrialExpired = true;
            }
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;

            try
            {
                var result = await _authService.LoginAsync(UsernameOrPhone, Password);
                if (!result.Success)
                {
                    ErrorMessage = result.Message;
                    return;
                }

                var user = result.User!;

                // Kiểm tra điều kiện bản quyền sau ngày 12/09/2026
                if (!_passkeyService.IsTrialActive && !user.HasPasskeyActivated)
                {
                    PasskeyUsername = user.Username;
                    PasskeyInput = string.Empty;
                    PasskeyStatusMessage = "Hạn trải nghiệm miễn phí đã kết thúc vào ngày 12/09/2026. Đồng chí vui lòng nhập Passkey bản quyền để kích hoạt tài khoản truy cập.";
                    IsPasskeySuccess = false;
                    IsPasskeyModalVisible = true;
                    return;
                }

                OnLoginSuccess?.Invoke();
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
        private void OpenPasskeyModal()
        {
            PasskeyUsername = string.IsNullOrWhiteSpace(UsernameOrPhone) ? "admin" : UsernameOrPhone.Trim();
            PasskeyInput = string.Empty;
            PasskeyStatusMessage = "Mỗi Passkey chỉ kích hoạt cho 01 tài khoản duy nhất để bảo đảm an toàn bảo mật quân sự.";
            IsPasskeySuccess = false;
            IsPasskeyModalVisible = true;
        }

        [RelayCommand]
        private void ClosePasskeyModal()
        {
            IsPasskeyModalVisible = false;
            PasskeyStatusMessage = string.Empty;
        }

        [RelayCommand]
        private async Task ActivatePasskeyAsync()
        {
            if (string.IsNullOrWhiteSpace(PasskeyUsername))
            {
                PasskeyStatusMessage = "Vui lòng nhập tên tài khoản cần kích hoạt.";
                IsPasskeySuccess = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(PasskeyInput))
            {
                PasskeyStatusMessage = "Vui lòng nhập mã Passkey gồm 16 ký tự (VD: QD-2026-HQ88-K01A).";
                IsPasskeySuccess = false;
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _passkeyService.ActivatePasskeyAsync(PasskeyUsername, PasskeyInput);
                PasskeyStatusMessage = result.Message;
                IsPasskeySuccess = result.Success;

                if (result.Success)
                {
                    ErrorMessage = string.Empty;
                    // Sau 1.5s tự đóng modal nếu người dùng vừa đăng nhập
                    await Task.Delay(1500);
                    IsPasskeyModalVisible = false;

                    // Nếu đã đăng nhập trước đó và đợi kích hoạt
                    if (!string.IsNullOrWhiteSpace(Password))
                    {
                        await LoginAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                PasskeyStatusMessage = $"Lỗi kích hoạt: {ex.Message}";
                IsPasskeySuccess = false;
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
