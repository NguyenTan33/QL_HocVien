using System;
using System.ComponentModel;
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
        private readonly ILoginLockoutService _lockoutService;

        [ObservableProperty]
        private string _usernameOrPhone = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _rememberMe;

        // Trạng thái khóa do Brute-Force
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanLogin))]
        [NotifyPropertyChangedFor(nameof(LoginButtonText))]
        [NotifyPropertyChangedFor(nameof(IsNormalErrorVisible))]
        private bool _isLockedOut;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LoginButtonText))]
        private string _lockoutRemainingText = "00:00";

        public bool CanLogin => !IsLockedOut && !IsBusy;

        public bool IsNormalErrorVisible => !string.IsNullOrWhiteSpace(ErrorMessage) && !IsLockedOut;

        public string LoginButtonText => IsLockedOut 
            ? $"ĐANG TẠM KHÓA ({LockoutRemainingText})" 
            : "ĐĂNG NHẬP VÀO HỆ THỐNG  ➔";

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

        public LoginViewModel(
            IAuthService authService, 
            IPasskeyService passkeyService,
            ILoginLockoutService lockoutService)
        {
            _authService = authService;
            _passkeyService = passkeyService;
            _lockoutService = lockoutService;
            Title = "Đăng Nhập - Hệ Thống Quản Lý Học Viên Quân Đội";

            _lockoutService.OnLockoutStateChanged += UpdateLockoutState;
            UpdateLockoutState();

            UpdateTrialStatus();
        }

        private void UpdateLockoutState()
        {
            void update()
            {
                IsLockedOut = _lockoutService.IsLockedOut;
                LockoutRemainingText = _lockoutService.FormattedRemainingTime;
                if (IsLockedOut)
                {
                    ErrorMessage = _lockoutService.LockoutMessage;
                }
                else if (ErrorMessage.StartsWith("Bạn đã nhập sai"))
                {
                    ErrorMessage = string.Empty;
                }
            }

            if (System.Windows.Application.Current?.Dispatcher != null && 
                !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(update);
            }
            else
            {
                update();
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsBusy))
            {
                OnPropertyChanged(nameof(CanLogin));
            }
            else if (e.PropertyName == nameof(ErrorMessage))
            {
                OnPropertyChanged(nameof(IsNormalErrorVisible));
            }
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
                IsPasskeyModalVisible = false;
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
            if (_lockoutService.IsLockedOut)
            {
                ErrorMessage = _lockoutService.LockoutMessage;
                return;
            }

            ErrorMessage = string.Empty;
            IsBusy = true;

            try
            {
                var result = await _authService.LoginAsync(UsernameOrPhone, Password);
                if (!result.Success)
                {
                    var (isLocked, _, message) = _lockoutService.RecordFailedAttempt();
                    ErrorMessage = isLocked ? message : result.Message;
                    return;
                }

                // Đăng nhập thành công -> Reset hoàn toàn đếm lần sai
                _lockoutService.RecordSuccessfulLogin();

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
