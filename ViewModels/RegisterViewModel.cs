using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _securityQuestion = "Tên trường tiểu học đầu tiên của đồng chí là gì?";

        [ObservableProperty]
        private string _securityAnswer = string.Empty;

        [ObservableProperty]
        private string _passwordHint = string.Empty;

        public System.Collections.Generic.List<string> PredefinedQuestions { get; } = new()
        {
            "Tên trường tiểu học đầu tiên của đồng chí là gì?",
            "Tên người thầy hoặc chỉ huy đầu tiên của đồng chí?",
            "Tên đơn vị quân đội/cơ quan công tác đầu tiên của đồng chí?",
            "Địa danh gắn liền với kỷ niệm tuổi thơ của đồng chí?",
            "Tên con vật nuôi hoặc thú cưng đầu tiên của đồng chí?",
            "Biệt danh thời niên thiếu của đồng chí là gì?"
        };

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _successMessage = string.Empty;

        public event Action? OnNavigateToLogin;

        public RegisterViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Đăng Ký Tài Khoản Mới";
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SecurityQuestion))
            {
                ErrorMessage = "Vui lòng chọn hoặc nhập câu hỏi bảo mật.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SecurityAnswer) || SecurityAnswer.Trim().Length < 2)
            {
                ErrorMessage = "Câu trả lời bảo mật phải có ít nhất 2 ký tự.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return;
            }

            IsBusy = true;

            try
            {
                var result = await _authService.RegisterAsync(
                    Username,
                    FullName,
                    PhoneNumber,
                    Password,
                    SecurityQuestion,
                    SecurityAnswer,
                    PasswordHint,
                    Email);

                if (result.Success)
                {
                    SuccessMessage = result.Message;
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
