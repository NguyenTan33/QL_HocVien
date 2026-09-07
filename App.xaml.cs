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
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log"),
                    $"[ERROR] {ex.Message}\n\n{ex.StackTrace}\n\nInner: {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
                MessageBox.Show($"Không thể khởi động ứng dụng:\n{ex.Message}\n\n{ex.StackTrace}",
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
            services.AddSingleton<IEmailService, EmailService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPasskeyService, PasskeyService>();
            services.AddScoped<IClassService, ClassService>();
            services.AddScoped<ICadetService, CadetService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IEvaluationService, EvaluationService>();
            services.AddScoped<IPhysicalExamService, PhysicalExamService>();
            services.AddScoped<IOfficerService, OfficerService>();
            services.AddScoped<ICatalogService, CatalogService>();
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
        }
    }
}
