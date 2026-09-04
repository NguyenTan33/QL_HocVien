using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.ViewModels;

namespace QL_HocVien.Views.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginViewModel LoginVm { get; }
        public RegisterViewModel RegisterVm { get; }
        public ForgotPasswordViewModel ForgotPasswordVm { get; }

        public LoginWindow(
            LoginViewModel loginVm,
            RegisterViewModel registerVm,
            ForgotPasswordViewModel forgotPasswordVm)
        {
            InitializeComponent();

            LoginVm = loginVm;
            RegisterVm = registerVm;
            ForgotPasswordVm = forgotPasswordVm;

            DataContext = this;
            LoginFormGrid.DataContext = LoginVm;

            // Wire events
            LoginVm.OnLoginSuccess += OnLoginSuccess;
            LoginVm.OnNavigateToRegister += ShowRegisterForm;
            LoginVm.OnNavigateToForgotPassword += ShowForgotPasswordForm;

            RegisterVm.OnNavigateToLogin += ShowLoginForm;
            ForgotPasswordVm.OnNavigateToLogin += ShowLoginForm;

            // Cho phép kéo thả di chuyển cửa sổ
            MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    DragMove();
                }
            };
        }

        private void OnLoginSuccess()
        {
            var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            Close();
        }

        private void ShowLoginForm()
        {
            LoginFormGrid.Visibility = Visibility.Visible;
            RegisterFormGrid.Visibility = Visibility.Collapsed;
            ForgotPasswordFormGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowRegisterForm()
        {
            LoginFormGrid.Visibility = Visibility.Collapsed;
            RegisterFormGrid.Visibility = Visibility.Visible;
            ForgotPasswordFormGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowForgotPasswordForm()
        {
            LoginFormGrid.Visibility = Visibility.Collapsed;
            RegisterFormGrid.Visibility = Visibility.Collapsed;
            ForgotPasswordFormGrid.Visibility = Visibility.Visible;
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
