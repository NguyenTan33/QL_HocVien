using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Services;
using QL_HocVien.ViewModels;
using QL_HocVien.Views.Windows;

namespace QL_HocVien
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"Đã xảy ra lỗi không mong muốn:\n{args.Exception.Message}\n\nChi tiết:\n{args.Exception}",
                                "Lỗi Hệ Thống QL_HocVien", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
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

                // Hiển thị màn hình Đăng nhập đầu tiên
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể khởi động ứng dụng:\n{ex.Message}\n\n{ex.StackTrace}",
                                "Lỗi Khởi Động", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Cấu hình chuỗi kết nối SQLite nằm cố định cùng thư mục thực thi
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ql_hocvien.db");
            string connectionString = $"Data Source={dbPath}";
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connProp) &&
                        connProp.TryGetProperty("DefaultConnection", out var defaultConn))
                    {
                        var configured = defaultConn.GetString();
                        if (!string.IsNullOrWhiteSpace(configured))
                        {
                            connectionString = configured;
                        }
                    }
                }
            }
            catch
            {
                // Giữ mặc định nếu lỗi đọc file cấu hình
            }

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

            // Đăng ký Services (SOLID - SRP, OCP)
            services.AddSingleton<IEmailService, EmailService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IClassService, ClassService>();
            services.AddScoped<ICadetService, CadetService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IEvaluationService, EvaluationService>();
            services.AddScoped<IPhysicalExamService, PhysicalExamService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddScoped<IExcelService, ExcelService>();

            // Đăng ký ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<ForgotPasswordViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ClassManagementViewModel>();
            services.AddTransient<CadetManagementViewModel>();
            services.AddTransient<AddCadetViewModel>();
            services.AddTransient<SubjectManagementViewModel>();
            services.AddTransient<PhysicalExamViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Đăng ký Windows
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
        }
    }
}
