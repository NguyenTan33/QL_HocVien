using System;
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

        [ObservableProperty]
        private ViewModelBase? _currentView;

        [ObservableProperty]
        private string _activeMenu = "Dashboard";

        [ObservableProperty]
        private User? _currentUser;

        public event Action? OnLogout;

        public MainViewModel(IAuthService authService, IServiceProvider serviceProvider)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            Title = "Hệ Thống Quản Lý Học Viên Quân Đội";
            CurrentUser = _authService.CurrentUser;

            // Mặc định mở màn hình Tổng quan (Dashboard)
            NavigateToDashboard();
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
            vm.OnRequestAddCadet += NavigateToAddCadet;
            CurrentView = vm;
        }

        [RelayCommand]
        public void NavigateToAddCadet()
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
