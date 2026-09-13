using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QL_HocVien.Tests
{
    public static class WpfTestHelper
    {
        private static readonly TaskCompletionSource<Dispatcher> _dispatcherReady = new();
        private static readonly Thread _staThread;

        static WpfTestHelper()
        {
            _staThread = new Thread(() =>
            {
                try
                {
                    if (System.Windows.Application.Current == null)
                    {
                        var app = new System.Windows.Application();
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/QL_HocVien;component/Styles/MilitaryTheme.xaml", UriKind.Absolute)
                        });
                    }
                }
                catch { }

                _dispatcherReady.SetResult(Dispatcher.CurrentDispatcher);
                Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = "WpfTestHelperSTA"
            };
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.Start();
        }

        public static void Run(Action action, int timeoutMs = 15000)
        {
            var dispatcher = _dispatcherReady.Task.GetAwaiter().GetResult();
            var task = dispatcher.InvokeAsync(action).Task;
            if (!task.Wait(timeoutMs))
            {
                throw new TimeoutException($"WPF STA test operation timed out after {timeoutMs}ms");
            }
        }
    }
}
