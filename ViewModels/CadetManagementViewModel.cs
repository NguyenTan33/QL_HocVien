using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class CadetManagementViewModel : ViewModelBase
    {
        private readonly ICadetService _cadetService;
        private readonly IAuthService _authService;

        public ObservableCollection<Cadet> Cadets { get; } = new();
        public ObservableCollection<string> RankList { get; } = new()
        {
            "Tất cả", "Binh nhì", "Binh nhất", "Hạ sĩ", "Trung sĩ", "Thượng sĩ", "Chuẩn úy", "Thiếu úy", "Trung úy", "Thượng úy", "Đại úy"
        };
        public ObservableCollection<string> UnitList { get; } = new()
        {
            "Tất cả", "Đại đội 1", "Đại đội 2", "Đại đội 3", "Trung đội 1", "Trung đội 2", "Trung đội 3"
        };

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedRank = "Tất cả";

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private Cadet? _selectedCadet;

        // Cho việc sửa nhanh trực tiếp
        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editFullName = string.Empty;

        [ObservableProperty]
        private string _editRank = "Binh nhì";

        [ObservableProperty]
        private string _editPosition = "Học viên";

        [ObservableProperty]
        private string _editUnit = "Đại đội 1";

        [ObservableProperty]
        private string _editClassName = string.Empty;

        [ObservableProperty]
        private string _editPhone = string.Empty;

        [ObservableProperty]
        private string _editEmail = string.Empty;

        // Cho đặt lại mật khẩu học viên
        [ObservableProperty]
        private bool _isResetPasswordDialogVisible;

        [ObservableProperty]
        private string _newCadetPassword = string.Empty;

        public event Action? OnRequestAddCadet;

        public CadetManagementViewModel(ICadetService cadetService, IAuthService authService)
        {
            _cadetService = cadetService;
            _authService = authService;
            Title = "Quản Lý Danh Sách Học Viên";

            _ = LoadCadetsAsync();
        }

        [RelayCommand]
        public async Task LoadCadetsAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _cadetService.SearchCadetsAsync(SearchKeyword, SelectedRank, SelectedUnit, null);
                Cadets.Clear();
                foreach (var cadet in list)
                {
                    Cadets.Add(cadet);
                }
                StatusMessage = $"Đã tải {Cadets.Count} học viên.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void RequestAddNew()
        {
            OnRequestAddCadet?.Invoke();
        }

        [RelayCommand]
        private void StartEdit()
        {
            if (SelectedCadet == null)
            {
                StatusMessage = "Vui lòng chọn học viên cần chỉnh sửa.";
                return;
            }

            EditFullName = SelectedCadet.FullName;
            EditRank = SelectedCadet.Rank;
            EditPosition = SelectedCadet.Position;
            EditUnit = SelectedCadet.Unit;
            EditClassName = SelectedCadet.ClassName;
            EditPhone = SelectedCadet.PhoneNumber;
            EditEmail = SelectedCadet.Email;
            IsEditing = true;
        }

        [RelayCommand]
        private async Task SaveEditAsync()
        {
            if (SelectedCadet == null) return;

            SelectedCadet.FullName = EditFullName;
            SelectedCadet.Rank = EditRank;
            SelectedCadet.Position = EditPosition;
            SelectedCadet.Unit = EditUnit;
            SelectedCadet.ClassName = EditClassName;
            SelectedCadet.PhoneNumber = EditPhone;
            SelectedCadet.Email = EditEmail;

            IsBusy = true;
            try
            {
                var result = await _cadetService.UpdateCadetAsync(SelectedCadet);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    IsEditing = false;
                    await LoadCadetsAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi cập nhật: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
        }

        [RelayCommand]
        private async Task DeleteCadetAsync()
        {
            if (SelectedCadet == null)
            {
                StatusMessage = "Vui lòng chọn học viên cần xóa.";
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _cadetService.DeleteCadetAsync(SelectedCadet.Id);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadCadetsAsync();
                    SelectedCadet = null;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi xóa: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenResetPassword()
        {
            if (SelectedCadet == null)
            {
                StatusMessage = "Vui lòng chọn một học viên để đặt lại mật khẩu.";
                return;
            }

            NewCadetPassword = "Hocvien@123"; // Gợi ý mặc định dễ nhớ
            IsResetPasswordDialogVisible = true;
        }

        [RelayCommand]
        private void CloseResetPassword()
        {
            IsResetPasswordDialogVisible = false;
        }

        [RelayCommand]
        private async Task ConfirmResetPasswordAsync()
        {
            if (SelectedCadet == null) return;

            IsBusy = true;
            try
            {
                var result = await _authService.ResetCadetPasswordAsync(SelectedCadet.Id, NewCadetPassword);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    IsResetPasswordDialogVisible = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi đặt lại mật khẩu: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSearchKeywordChanged(string value) => _ = LoadCadetsAsync();
        partial void OnSelectedRankChanged(string value) => _ = LoadCadetsAsync();
        partial void OnSelectedUnitChanged(string value) => _ = LoadCadetsAsync();
    }
}
