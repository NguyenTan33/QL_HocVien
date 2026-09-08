using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.Models;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISecurityGateService _securityGate;
        private readonly IThemeService _themeService;

        [ObservableProperty]
        private ViewModelBase? _currentView;

        [ObservableProperty]
        private string _activeMenu = "Dashboard";

        [ObservableProperty]
        private User? _currentUser;

        public bool IsAdmin => string.Equals(CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        public bool IsCanBo => string.Equals(CurrentUser?.Role, "CanBo", StringComparison.OrdinalIgnoreCase);
        public bool IsOfficerOrAdmin => IsAdmin || IsCanBo;
        public bool IsHocVien => string.Equals(CurrentUser?.Role, "HocVien", StringComparison.OrdinalIgnoreCase);

        partial void OnCurrentUserChanged(User? value)
        {
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(IsCanBo));
            OnPropertyChanged(nameof(IsOfficerOrAdmin));
            OnPropertyChanged(nameof(IsHocVien));
        }

        [ObservableProperty]
        private bool _isSecurityProtectionEnabled;

        [ObservableProperty]
        private bool _isSecurityUnlocked;

        [ObservableProperty]
        private string _securityRemainingTime = "00:00";

        [ObservableProperty]
        private string _securityStatusTooltip = string.Empty;

        // DUAL-THEME: CHẾ ĐỘ TÁC CHIẾN (COMBAT COMMAND CENTER) ⮂ CHẾ ĐỘ HÀNH CHÍNH (ADMINISTRATIVE)
        [ObservableProperty]
        private bool _isCombatMode = true;

        [ObservableProperty]
        private string _themeModeButtonText = "CHẾ ĐỘ HÀNH CHÍNH";

        [ObservableProperty]
        private string _themeModeTooltip = "Bấm để chuyển sang Chế độ Giao diện Hành chính công vụ sáng";

        public event Action? OnLogout;

        public MainViewModel(IAuthService authService, IServiceProvider serviceProvider, ISecurityGateService securityGate, IThemeService themeService)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            _securityGate = securityGate;
            _themeService = themeService;
            Title = "Hệ Thống Quản Lý Học Viên Quân Đội";
            CurrentUser = _authService.CurrentUser;

            _securityGate.OnSecurityStateChanged += UpdateSecurityStatus;
            UpdateSecurityStatus();

            // Mặc định mở màn hình Tổng quan (Dashboard)
            NavigateToDashboard();
        }

        private void UpdateSecurityStatus()
        {
            IsSecurityProtectionEnabled = _securityGate.IsProtectionEnabled;
            IsSecurityUnlocked = _securityGate.IsUnlocked;
            SecurityRemainingTime = _securityGate.FormattedRemainingTime;

            if (!IsSecurityProtectionEnabled)
            {
                SecurityStatusTooltip = "Khóa bảo mật Cấp 2 đang tắt";
            }
            else if (IsSecurityUnlocked)
            {
                SecurityStatusTooltip = $"Đang mở khóa thao tác (Còn lại: {SecurityRemainingTime}). Bấm 'Khóa Ngay' để thu hồi quyền.";
            }
            else
            {
                SecurityStatusTooltip = "Hệ thống đang khóa bảo vệ (Chỉ xem). Bấm để nhập mật khẩu mở khóa thao tác.";
            }
        }

        [RelayCommand]
        public void QuickLock()
        {
            _securityGate.LockNow();
        }

        [RelayCommand]
        public async Task QuickUnlock()
        {
            await _securityGate.EnsureUnlockedAsync("mở khóa quyền thao tác");
        }

        [RelayCommand]
        public void ToggleThemeMode()
        {
            _themeService.ToggleTheme();
            IsCombatMode = _themeService.IsCombatMode;
            if (IsCombatMode)
            {
                ThemeModeButtonText = "CHẾ ĐỘ HÀNH CHÍNH";
                ThemeModeTooltip = "Bấm để chuyển sang Chế độ Giao diện Hành chính công vụ sáng";
            }
            else
            {
                ThemeModeButtonText = "CHẾ ĐỘ TÁC CHIẾN";
                ThemeModeTooltip = "Bấm để kích hoạt Trung tâm Chỉ huy Tác chiến Quân đội";
            }
        }

        [RelayCommand]
        public void NavigateToDashboard()
        {
            ActiveMenu = "Dashboard";
            CurrentView = _serviceProvider.GetRequiredService<DashboardViewModel>();
        }

        [RelayCommand]
        public void NavigateToCadetManagement()
        {
            ActiveMenu = "CadetManagement";
            var vm = _serviceProvider.GetRequiredService<CadetManagementViewModel>();
            vm.OnRequestAddCadet += OpenAddCadetView;
            vm.OnRequestManageUnits += NavigateToUnitCatalog;
            CurrentView = vm;
        }

        [RelayCommand]
        public void NavigateToUnitCatalog()
        {
            NavigateToCatalogManagement();
            if (CurrentView is CatalogManagementViewModel cvm)
            {
                cvm.SelectedTabIndex = 2; // Tab Đơn Vị Quản Lý (Đại đội, Tiểu đoàn...)
            }
        }

        [RelayCommand]
        public async Task NavigateToAddCadetAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Mở biểu mẫu Thêm Mới Học Viên")) return;
            OpenAddCadetView();
        }

        private void OpenAddCadetView()
        {
            ActiveMenu = "AddCadet";
            var vm = _serviceProvider.GetRequiredService<AddCadetViewModel>();
            vm.OnCadetSaved += NavigateToCadetManagement;
            CurrentView = vm;
        }

        [RelayCommand]
        public void NavigateToSubjectManagement()
        {
            ActiveMenu = "SubjectManagement";
            CurrentView = _serviceProvider.GetRequiredService<SubjectManagementViewModel>();
        }

        [RelayCommand]
        public void NavigateToClassManagement()
        {
            ActiveMenu = "ClassManagement";
            CurrentView = _serviceProvider.GetRequiredService<ClassManagementViewModel>();
        }

        [RelayCommand]
        public void NavigateToPhysicalExam()
        {
            ActiveMenu = "PhysicalExam";
            CurrentView = _serviceProvider.GetRequiredService<PhysicalExamViewModel>();
        }

        [RelayCommand]
        public void NavigateToOfficerManagement()
        {
            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show(
                    "CẢNH BÁO AN NINH: Đồng chí không có quyền truy cập Danh mục Cán bộ Sĩ quan!\nChức năng này chỉ dành riêng cho Quản Trị Viên (Admin).",
                    "Truy Cập Bị Từ Chối (403)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            ActiveMenu = "OfficerManagement";
            CurrentView = _serviceProvider.GetRequiredService<OfficerManagementViewModel>();
        }

        [RelayCommand]
        public void NavigateToCatalogManagement()
        {
            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show(
                    "CẢNH BÁO AN NINH: Đồng chí không có quyền chỉnh sửa Danh mục Tổ chức Quân sự!\nChức năng này chỉ dành riêng cho Quản Trị Viên (Admin).",
                    "Truy Cập Bị Từ Chối (403)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            ActiveMenu = "CatalogManagement";
            CurrentView = _serviceProvider.GetRequiredService<CatalogManagementViewModel>();
        }

        [RelayCommand]
        public void NavigateToCreditSubjectManagement()
        {
            ActiveMenu = "CreditSubjectManagement";
            CurrentView = _serviceProvider.GetRequiredService<CreditSubjectManagementViewModel>();
        }

        [RelayCommand]
        public void NavigateToExamAnalytics()
        {
            ActiveMenu = "ExamAnalytics";
            CurrentView = _serviceProvider.GetRequiredService<ExamAnalyticsViewModel>();
        }

        [RelayCommand]
        public void NavigateToAcademicAnalytics()
        {
            ActiveMenu = "AcademicAnalytics";
            CurrentView = _serviceProvider.GetRequiredService<AcademicAnalyticsViewModel>();
        }

        [RelayCommand]
        public void NavigateToTrainingTimeline()
        {
            ActiveMenu = "TrainingTimeline";
            CurrentView = _serviceProvider.GetRequiredService<TrainingTimelineViewModel>();
        }

        [RelayCommand]
        public void NavigateToSettings()
        {
            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show(
                    "CẢNH BÁO AN NINH: Cài đặt hệ thống và cấu hình máy chủ chỉ dành riêng cho Quản Trị Viên (Admin).",
                    "Truy Cập Bị Từ Chối (403)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            ActiveMenu = "Settings";
            CurrentView = _serviceProvider.GetRequiredService<SettingsViewModel>();
        }

        [RelayCommand]
        public void Logout()
        {
            _authService.Logout();
            OnLogout?.Invoke();
        }
    }
}
