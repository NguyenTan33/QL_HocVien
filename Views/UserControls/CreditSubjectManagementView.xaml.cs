using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QL_HocVien.Views.UserControls
{
    public partial class CreditSubjectManagementView : UserControl
    {
        public CreditSubjectManagementView()
        {
            InitializeComponent();
        }

        private void ScoreTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(new System.Action(() => tb.SelectAll()));
            }
        }

        private void ScoreTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
            {
                tb.Focus();
                e.Handled = true;
            }
        }

        private void ScoreTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox tb)
                {
                    var binding = tb.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();
                    tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                    e.Handled = true;
                }
            }
        }
    }
}
