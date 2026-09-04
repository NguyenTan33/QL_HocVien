using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.ViewModels;

namespace QL_HocVien.Views.Windows
{
    public partial class MainWindow : Window
    {
        public MainViewModel Vm { get; }

        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            Vm = vm;
            DataContext = Vm;

            Vm.OnLogout += OnLogout;
        }

        private void OnLogout()
        {
            var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                BtnMax.Content = "☐";
            }
            else
            {
                WindowState = WindowState.Maximized;
                BtnMax.Content = "❐";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
