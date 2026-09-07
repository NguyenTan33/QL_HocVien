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

        [ObservableProperty]
        private ViewModelBase? _currentView;

        [ObservableProperty]
        private string _activeMenu = "Dashboard";

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private bool _isSecurityProtectionEnabled;

        [ObservableProperty]
        private bool _isSecurityUnlocked;

        [ObservableProperty]
        private string _securityRemainingTime = "00:00";

        [ObservableProperty]
        private string _securityStatusTooltip = string.Empty;

        public event Action? OnLogout;

        public MainViewModel(IAuthService authService, IServiceProvider serviceProvider, ISecurityGateService securityGate)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            _securityGate = securityGate;
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
            ActiveMenu = "OfficerManagement";
            CurrentView = _serviceProvider.GetRequiredService<OfficerManagementViewModel>();
        }

        [RelayCommand]
        public void NavigateToCatalogManagement()
        {
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
