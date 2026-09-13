using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Services;
using QL_HocVien.Services.Calculators;
using QL_HocVien.ViewModels;
using QL_HocVien.Views.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SQLitePCL;

namespace QL_HocVien
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Nạp engine mã hóa SQLCipher AES-256 trước khi bất kỳ kết nối SQLite nào được mở
            Batteries_V2.Init();

            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            var culture = new System.Globalization.CultureInfo("vi-VN");
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), 
                new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            DispatcherUnhandledException += (sender, args) =>
            {
                try
                {
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system_error.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Dispatcher] Lỗi: {args.Exception.Message}\nChi tiết:\n{args.Exception}\n-----------------------------------\n");
                }
                catch { }

                MessageBox.Show("Đã xảy ra sự cố không mong muốn trong quá trình thực thi.\nThông tin lỗi đã được ghi lại an toàn vào tệp nhật ký.",
                                "Lỗi Hệ Thống QL_HocVien", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                try
                {
                    var ex = args.ExceptionObject as Exception;
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system_error.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [AppDomain] Lỗi: {ex?.Message}\nChi tiết:\n{ex}\n-----------------------------------\n");
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                try
                {
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system_error.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [UnobservedTask] Lỗi: {args.Exception.Message}\nChi tiết:\n{args.Exception}\n-----------------------------------\n");
                }
                catch { }
                args.SetObserved();
            };

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();

                // Khởi tạo và seed CSDL SQLite tự động
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    DbInitializer.Initialize(dbContext);
                }

                // Khởi tạo giao diện Chế độ Tác chiến (Combat Command Center) theo promt.txt & DemoUI.png
                ServiceProvider.GetRequiredService<IThemeService>().ApplyTheme(true);

                // Hỗ trợ kiểm thử chụp ảnh màn hình tự động (--screenshot)
                if (Array.Exists(e.Args, a => a == "--screenshot"))
                {
                    string outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "app_screenshot.png");
                    int idx = Array.IndexOf(e.Args, "--screenshot");
                    if (idx >= 0 && idx < e.Args.Length - 1 && !e.Args[idx + 1].StartsWith("-"))
                    {
                        outPath = e.Args[idx + 1];
                    }

                    var authService = ServiceProvider.GetRequiredService<IAuthService>();
                    authService.LoginAsync("admin", "Admin@123").GetAwaiter().GetResult();

                    if (Array.Exists(e.Args, a => a == "--theme"))
                    {
                        int tIdx = Array.IndexOf(e.Args, "--theme");
                        if (tIdx >= 0 && tIdx < e.Args.Length - 1)
                        {
                            string themeArg = e.Args[tIdx + 1].ToLowerInvariant();
                            var themeService = ServiceProvider.GetRequiredService<IThemeService>();
                            if (themeArg.Contains("admin") || themeArg.Contains("light") || themeArg.Contains("basic"))
                            {
                                themeService.ApplyTheme(false);
                            }
                            else
                            {
                                themeService.ApplyTheme(true);
                            }
                        }
                    }

                    var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                    mainWindow.Width = 1600;
                    mainWindow.Height = 900;

                    if (Array.Exists(e.Args, a => a == "--page"))
                    {
                        int pIdx = Array.IndexOf(e.Args, "--page");
                        if (pIdx >= 0 && pIdx < e.Args.Length - 1)
                        {
                            string target = e.Args[pIdx + 1].ToLowerInvariant();
                            if (target.Contains("login") || target.Contains("register") || target.Contains("forgot"))
                            {
                                var testLoginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                                if (target.Contains("register"))
                                {
                                    testLoginWindow.ShowRegisterForm();
                                }
                                else if (target.Contains("forgot"))
                                {
                                    testLoginWindow.ShowForgotPasswordForm();
                                }
                                testLoginWindow.Show();
                                testLoginWindow.UpdateLayout();
                                testLoginWindow.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                                System.Threading.Thread.Sleep(500);
                                testLoginWindow.UpdateLayout();
                                int lw = (int)Math.Max(1000, testLoginWindow.ActualWidth > 0 ? testLoginWindow.ActualWidth : 1000);
                                int lh = (int)Math.Max(650, testLoginWindow.ActualHeight > 0 ? testLoginWindow.ActualHeight : 650);
                                var lrtb = new RenderTargetBitmap(lw, lh, 96, 96, PixelFormats.Pbgra32);
                                lrtb.Render(testLoginWindow);
                                var ldir = Path.GetDirectoryName(outPath);
                                if (!string.IsNullOrEmpty(ldir) && !Directory.Exists(ldir)) Directory.CreateDirectory(ldir);
                                var lencoder = new PngBitmapEncoder();
                                lencoder.Frames.Add(BitmapFrame.Create(lrtb));
                                using (var fs = File.Create(outPath)) { lencoder.Save(fs); }
                                Shutdown(0);
                                return;
                            }
                            else if (mainWindow.DataContext is MainViewModel mainVm)
                            {
                                if (target.Contains("officer")) mainVm.NavigateToOfficerManagement();
                                else if (target.Contains("credit") || target.Contains("subject")) mainVm.NavigateToCreditSubjectManagement();
                                else if (target.Contains("timeline") || target.Contains("calendar")) mainVm.NavigateToTrainingTimeline();
                                else if (target.Contains("academic")) mainVm.NavigateToAcademicAnalytics();
                                else if (target.Contains("exam")) mainVm.NavigateToExamAnalytics();
                                else if (target.Contains("setting")) mainVm.NavigateToSettings();
                                else if (target.Contains("cadet")) mainVm.NavigateToCadetManagement();
                                else if (target.Contains("class")) mainVm.NavigateToClassManagement();
                                else if (target.Contains("catalog") || target.Contains("unit") || target.Contains("tree"))
                                {
                                    mainVm.NavigateToCatalogManagement();
                                    if (target.Contains("unit") || target.Contains("tree") || target.Contains("daidoi"))
                                    {
                                        if (mainVm.CurrentView is CatalogManagementViewModel catVm)
                                        {
                                            catVm.SelectedTabIndex = 2;
                                            if (target.Contains("collapse"))
                                            {
                                                catVm.CollapseAllTree();
                                            }
                                        }
                                    }
                                }
                                else if (target.Contains("dashboard")) mainVm.NavigateToDashboard();
                            }

                            if (Array.Exists(e.Args, a => a == "--modal") && mainWindow.DataContext is MainViewModel mv)
                            {
                                if (mv.CurrentView is OfficerManagementViewModel offVm)
                                {
                                    offVm.OpenAddFormCommand.Execute(null);
                                }
                                else if (mv.CurrentView is CreditSubjectManagementViewModel credVm)
                                {
                                    credVm.OpenAddSubjectFormCommand.Execute(null);
                                }
                                else if (mv.CurrentView is ClassManagementViewModel clsVm)
                                {
                                    clsVm.OpenAddFormCommand.Execute(null);
                                }
                            }
                        }
                    }

                    mainWindow.Show();
                    mainWindow.UpdateLayout();
                    mainWindow.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    System.Threading.Thread.Sleep(500);
                    mainWindow.UpdateLayout();
                    mainWindow.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                    int w = (int)Math.Max(1600, mainWindow.ActualWidth);
                    int h = (int)Math.Max(900, mainWindow.ActualHeight);
                    var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(mainWindow);

                    var dir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using (var fs = File.Create(outPath))
                    {
                        encoder.Save(fs);
                    }

                    Shutdown(0);
                    return;
                }

                // Bỏ luồng tự động kiểm tra cập nhật khi khởi động để chạy offline mượt mà không bị làm phiền

                // Hiển thị màn hình Đăng nhập đầu tiên
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CRITICAL] Không thể khởi động ứng dụng:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n\nInner: {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
                }
                catch { }

                MessageBox.Show("Không thể khởi động ứng dụng do xảy ra sự cố nội bộ.\nChi tiết lỗi đã được ghi vào tập tin 'startup_error.log'.\nVui lòng liên hệ quản trị viên hệ thống để được hỗ trợ.",
                                "Lỗi Khởi Động", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Cấu hình chuỗi kết nối SQLite nằm cố định cùng thư mục thực thi
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ql_hocvien.db");

            // 2. Lấy Passphrase mã hóa cấp độ quân sự từ SecureKeyVault (DPAPI + Machine Fingerprint)
            string dbPassphrase = SecureKeyVault.GetPassphrase();

            // 3. Tự động kiểm tra và chuyển đổi CSDL sang dạng mã hóa SQLCipher AES-256 nếu đang là plaintext
            SqlCipherMigrator.MigrateIfPlaintext(dbPath, dbPassphrase);

            // 4. Cấu hình chuỗi kết nối có mật khẩu AES-256
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = dbPassphrase // Kích hoạt mã hóa SQLCipher AES-256
            };

            string connectionString = connectionStringBuilder.ToString();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(connectionString);
            });

            // Đăng ký Repositories (SOLID - DIP, ISP)
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IClassRepository, ClassRepository>();
            services.AddScoped<ICadetRepository, CadetRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<IPhysicalExamRepository, PhysicalExamRepository>();
            services.AddScoped<IOfficerRepository, OfficerRepository>();
            services.AddScoped<IRankRepository, RankRepository>();
            services.AddScoped<IPositionRepository, PositionRepository>();
            services.AddScoped<IUnitRepository, UnitRepository>();
            services.AddScoped<IMajorRepository, MajorRepository>();
            services.AddScoped<ITrainingEventRepository, TrainingEventRepository>();

            // Đăng ký Services (SOLID - SRP, OCP)
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPasskeyService, PasskeyService>();
            services.AddScoped<IClassService, ClassService>();
            services.AddScoped<ICohortService, CohortService>();
            services.AddScoped<ICadetService, CadetService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IEvaluationService, EvaluationService>();
            services.AddScoped<IPhysicalExamService, PhysicalExamService>();
            services.AddScoped<IOfficerService, OfficerService>();
            services.AddScoped<ICatalogService, CatalogService>();
            services.AddSingleton<IUnitHierarchyService, UnitHierarchyService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddScoped<IExcelService, ExcelService>();
            services.AddScoped<ITrainingEventService, TrainingEventService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<ITrainingRecommendationService, TrainingRecommendationService>();
            services.AddScoped<IDashboardAnalyticsService, DashboardAnalyticsService>();
            services.AddScoped<ICreditGradeCalculator, CreditGradeCalculator>();
            services.AddScoped<ICreditSubjectService, CreditSubjectService>();
            services.AddScoped<IAcademicAnalyticsService, AcademicAnalyticsService>();
            services.AddSingleton<ISecurityDialogService, SecurityDialogService>();
            services.AddSingleton<ISecurityGateService, SecurityGateService>();
            services.AddSingleton<ISecureKeyVault, SecureKeyVault>();
            services.AddSingleton<ILoginLockoutService, LoginLockoutService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IDownloadUpdateService, DownloadUpdateService>();
            services.AddSingleton<IUpdateService, UpdateService>();

            // Đăng ký Infrastructure (Validation Factory & Security Services - OOP & SOLID)
            services.AddAppInfrastructureValidation();

            // Đăng ký ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<ForgotPasswordViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<CreditSubjectManagementViewModel>();
            services.AddTransient<AcademicAnalyticsViewModel>();
            services.AddTransient<OfficerManagementViewModel>();
            services.AddTransient<CatalogManagementViewModel>();
            services.AddTransient<ClassManagementViewModel>();
            services.AddTransient<CohortManagementViewModel>();
            services.AddTransient<CadetManagementViewModel>();
            services.AddTransient<AddCadetViewModel>();
            services.AddTransient<SubjectManagementViewModel>();
            services.AddTransient<PhysicalExamViewModel>();
            services.AddTransient<ExamAnalyticsViewModel>();
            services.AddTransient<TrainingTimelineViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Đăng ký Windows
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<UpdateWindow>();
        }
    }
}
