using System;
using System.Threading.Tasks;
using System.Windows;
using QL_HocVien.Views.Windows;

namespace QL_HocVien.Services
{
    /// <summary>
    /// Triển khai ISecurityDialogService hiển thị modal dialog trên UI Dispatcher
    /// </summary>
    public class SecurityDialogService : ISecurityDialogService
    {
        public Task<bool> ShowPasswordVerificationDialogAsync(string actionDescription, Func<string, bool> verifier)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (Application.Current == null)
            {
                tcs.SetResult(false);
                return tcs.Task;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new SecurityPasswordDialog(actionDescription, verifier);
                    if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                    {
                        dialog.Owner = Application.Current.MainWindow;
                    }
                    var result = dialog.ShowDialog();
                    tcs.SetResult(result == true && dialog.IsVerified);
                }
                catch
                {
                    tcs.SetResult(false);
                }
            });

            return tcs.Task;
        }
    }
}
