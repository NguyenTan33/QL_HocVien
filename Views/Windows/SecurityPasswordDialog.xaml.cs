using System;
using System.Windows;
using System.Windows.Input;

namespace QL_HocVien.Views.Windows
{
    public partial class SecurityPasswordDialog : Window
    {
        private readonly Func<string, bool> _verifier;
        public bool IsVerified { get; private set; } = false;

        public SecurityPasswordDialog(string actionDescription, Func<string, bool> verifier)
        {
            InitializeComponent();
            _verifier = verifier;
            txtActionDescription.Text = actionDescription;

            Loaded += (s, e) =>
            {
                txtPassword.Focus();
            };
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryUnlock();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            TryUnlock();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TryUnlock()
        {
            var pwd = txtPassword.Password;
            if (string.IsNullOrEmpty(pwd))
            {
                txtErrorMessage.Text = "Vui lòng nhập mật khẩu bảo mật!";
                txtErrorMessage.Visibility = Visibility.Visible;
                txtPassword.Focus();
                return;
            }

            if (_verifier != null && _verifier(pwd))
            {
                IsVerified = true;
                DialogResult = true;
                Close();
            }
            else
            {
                txtErrorMessage.Text = "Mật khẩu không chính xác! Vui lòng thử lại.";
                txtErrorMessage.Visibility = Visibility.Visible;
                txtPassword.SelectAll();
                txtPassword.Focus();
            }
        }
    }
}
