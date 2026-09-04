using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class OfficerManagementViewModel : ViewModelBase
    {
        private readonly IOfficerService _officerService;
        private readonly ICatalogService _catalogService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;

        public ObservableCollection<Officer> Officers { get; } = new();
        public ObservableCollection<string> FilterRanks { get; } = new() { "Tất cả" };
        public ObservableCollection<string> FilterUnits { get; } = new() { "Tất cả" };
        public ObservableCollection<string> FilterPositions { get; } = new() { "Tất cả" };
        public ObservableCollection<string> HasAccountList { get; } = new() { "Tất cả", "Đã cấp tài khoản", "Chưa cấp tài khoản" };
        public ObservableCollection<string> HasAssignedClassesList { get; } = new() { "Tất cả", "Đang chủ nhiệm lớp", "Chưa chủ nhiệm lớp" };

        public ObservableCollection<string> FormRanks { get; } = new();
        public ObservableCollection<string> FormUnits { get; } = new();
        public ObservableCollection<string> FormPositions { get; } = new();

        // Bộ lọc & Tìm kiếm
        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedFilterRank = "Tất cả";

        [ObservableProperty]
        private string _selectedFilterUnit = "Tất cả";

        [ObservableProperty]
        private string _selectedFilterPosition = "Tất cả";

        [ObservableProperty]
        private string _filterSpecialty = string.Empty;

        [ObservableProperty]
        private string _selectedHasAccount = "Tất cả";

        [ObservableProperty]
        private string _selectedHasAssignedClasses = "Tất cả";

        [ObservableProperty]
        private bool _isAdvancedFilterVisible;

        [ObservableProperty]
        private int _activeFilterCount;

        [ObservableProperty]
        private Officer? _selectedOfficer;

        // Modal Form Thêm / Sửa Cán Bộ
        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _formTitle = string.Empty;

        [ObservableProperty]
        private string _formOfficerCode = string.Empty;

        [ObservableProperty]
        private string _formFullName = string.Empty;

        [ObservableProperty]
        private string _formRank = string.Empty;

        [ObservableProperty]
        private string _formPosition = string.Empty;

        [ObservableProperty]
        private string _formUnit = string.Empty;

        [ObservableProperty]
        private string _formPhoneNumber = string.Empty;

        [ObservableProperty]
        private string _formEmail = string.Empty;

        [ObservableProperty]
        private string _formSpecialty = string.Empty;

        [ObservableProperty]
        private DateTime? _formDateOfBirth = new DateTime(1990, 1, 1);

        [ObservableProperty]
        private DateTime? _formEnlistmentDate = new DateTime(2010, 9, 1);

        [ObservableProperty]
        private string _formNotes = string.Empty;

        // Tùy chọn tạo tài khoản đăng nhập khi thêm mới
        [ObservableProperty]
        private bool _formCreateLoginAccount = false;

        [ObservableProperty]
        private string _formUsername = string.Empty;

        [ObservableProperty]
        private string _formPassword = string.Empty;

        // Modal Đặt Lại Mật Khẩu Cho Cán Bộ
        [ObservableProperty]
        private bool _isResetPasswordDialogVisible;

        [ObservableProperty]
        private string _resetOfficerName = string.Empty;

        [ObservableProperty]
        private string _resetNewPassword = string.Empty;

        public OfficerManagementViewModel(
            IOfficerService officerService,
            ICatalogService catalogService,
            IExcelService excelService,
            IFileDialogService fileDialogService)
        {
            _officerService = officerService;
            _catalogService = catalogService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            Title = "Quản Lý Cán Bộ Quân Sự";

            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                // 1. Tải danh mục dropdowns động từ ICatalogService
                var ranks = await _catalogService.GetRankDropdownAsync();
                var units = await _catalogService.GetUnitDropdownAsync();
                var positions = await _catalogService.GetPositionDropdownAsync();

                FilterRanks.Clear();
                FilterRanks.Add("Tất cả");
                FormRanks.Clear();
                foreach (var r in ranks)
                {
                    FilterRanks.Add(r);
                    FormRanks.Add(r);
                }

                FilterUnits.Clear();
                FilterUnits.Add("Tất cả");
                FormUnits.Clear();
                foreach (var u in units)
                {
                    FilterUnits.Add(u);
                    FormUnits.Add(u);
                }

                FilterPositions.Clear();
                FilterPositions.Add("Tất cả");
                FormPositions.Clear();
                foreach (var p in positions)
                {
                    FilterPositions.Add(p);
                    FormPositions.Add(p);
                }

                // 2. Tải danh sách cán bộ
                await SearchAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu cán bộ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void ToggleAdvancedFilter()
        {
            IsAdvancedFilterVisible = !IsAdvancedFilterVisible;
        }

        [RelayCommand]
        public void ResetFilters()
        {
            SearchKeyword = string.Empty;
            SelectedFilterRank = "Tất cả";
            SelectedFilterUnit = "Tất cả";
            SelectedFilterPosition = "Tất cả";
            FilterSpecialty = string.Empty;
            SelectedHasAccount = "Tất cả";
            SelectedHasAssignedClasses = "Tất cả";
            _ = SearchAsync();
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            IsBusy = true;
            try
            {
                int count = 0;
                if (!string.IsNullOrWhiteSpace(SearchKeyword)) count++;
                if (SelectedFilterRank != "Tất cả") count++;
                if (SelectedFilterUnit != "Tất cả") count++;
                if (SelectedFilterPosition != "Tất cả") count++;
                if (!string.IsNullOrWhiteSpace(FilterSpecialty)) count++;
                if (SelectedHasAccount != "Tất cả") count++;
                if (SelectedHasAssignedClasses != "Tất cả") count++;
                ActiveFilterCount = count;

                bool? hasAccount = SelectedHasAccount == "Đã cấp tài khoản" ? true :
                                   SelectedHasAccount == "Chưa cấp tài khoản" ? false : null;

                bool? hasAssignedClasses = SelectedHasAssignedClasses == "Đang chủ nhiệm lớp" ? true :
                                           SelectedHasAssignedClasses == "Chưa chủ nhiệm lớp" ? false : null;

                var criteria = new QL_HocVien.Models.Filters.OfficerFilterCriteria
                {
                    Keyword = SearchKeyword,
                    Rank = SelectedFilterRank,
                    Position = SelectedFilterPosition,
                    Unit = SelectedFilterUnit,
                    Specialty = FilterSpecialty,
                    HasAccount = hasAccount,
                    HasAssignedClasses = hasAssignedClasses
                };

                var list = await _officerService.SearchOfficersAsync(criteria);

                Officers.Clear();
                foreach (var off in list)
                {
                    Officers.Add(off);
                }

                StatusMessage = $"Tìm thấy {Officers.Count} cán bộ quân sự {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tìm kiếm cán bộ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task OpenAddFormAsync()
        {
            IsEditing = false;
            ClearForm();
            FormTitle = "Thêm Cán Bộ Quản Lý Mới";

            // Sinh mã cán bộ tự động
            FormOfficerCode = await _officerService.GenerateNextOfficerCodeAsync();
            FormRank = FormRanks.FirstOrDefault() ?? "Đại úy";
            FormPosition = FormPositions.FirstOrDefault() ?? "Đại đội trưởng";
            FormUnit = FormUnits.FirstOrDefault() ?? "Đại đội 1";
            FormCreateLoginAccount = false;
            FormUsername = FormOfficerCode.ToLower();
            FormPassword = "Password123@";

            IsFormVisible = true;
        }

        [RelayCommand]
        public void OpenEditForm()
        {
            if (SelectedOfficer == null)
            {
                MessageBox.Show("Vui lòng chọn một cán bộ để chỉnh sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsEditing = true;
            ClearForm();
            FormTitle = $"Chỉnh Sửa Thông Tin Cán Bộ: {SelectedOfficer.FullName}";

            FormOfficerCode = SelectedOfficer.OfficerCode;
            FormFullName = SelectedOfficer.FullName;
            FormRank = SelectedOfficer.Rank;
            FormPosition = SelectedOfficer.Position;
            FormUnit = SelectedOfficer.Unit;
            FormPhoneNumber = SelectedOfficer.PhoneNumber;
            FormEmail = SelectedOfficer.Email;
            FormSpecialty = SelectedOfficer.Specialty;
            FormDateOfBirth = SelectedOfficer.DateOfBirth;
            FormEnlistmentDate = SelectedOfficer.EnlistmentDate;
            FormNotes = SelectedOfficer.Notes ?? string.Empty;
            FormCreateLoginAccount = false;

            IsFormVisible = true;
        }

        [RelayCommand]
        public void CloseForm()
        {
            IsFormVisible = false;
            ClearForm();
        }

        [RelayCommand]
        public async Task SaveFormAsync()
        {
            if (string.IsNullOrWhiteSpace(FormOfficerCode) || string.IsNullOrWhiteSpace(FormFullName))
            {
                MessageBox.Show("Mã cán bộ và Họ tên không được để trống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditing)
                {
                    if (SelectedOfficer == null) return;
                    SelectedOfficer.FullName = FormFullName.Trim();
                    SelectedOfficer.Rank = FormRank;
                    SelectedOfficer.Position = FormPosition;
                    SelectedOfficer.Unit = FormUnit;
                    SelectedOfficer.PhoneNumber = FormPhoneNumber?.Trim() ?? string.Empty;
                    SelectedOfficer.Email = FormEmail?.Trim() ?? string.Empty;
                    SelectedOfficer.Specialty = FormSpecialty?.Trim() ?? string.Empty;
                    SelectedOfficer.DateOfBirth = FormDateOfBirth;
                    SelectedOfficer.EnlistmentDate = FormEnlistmentDate;
                    SelectedOfficer.Notes = FormNotes?.Trim();

                    var result = await _officerService.UpdateOfficerAsync(SelectedOfficer);
                    StatusMessage = result.Message;
                    if (!result.Success)
                    {
                        MessageBox.Show(result.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    var newOfficer = new Officer
                    {
                        OfficerCode = FormOfficerCode.Trim(),
                        FullName = FormFullName.Trim(),
                        Rank = FormRank,
                        Position = FormPosition,
                        Unit = FormUnit,
                        PhoneNumber = FormPhoneNumber?.Trim() ?? string.Empty,
                        Email = FormEmail?.Trim() ?? string.Empty,
                        Specialty = FormSpecialty?.Trim() ?? string.Empty,
                        DateOfBirth = FormDateOfBirth,
                        EnlistmentDate = FormEnlistmentDate,
                        Notes = FormNotes?.Trim()
                    };

                    string? initialPass = FormCreateLoginAccount ? FormPassword : null;
                    var result = await _officerService.CreateOfficerAsync(newOfficer, FormCreateLoginAccount, initialPass);
                    StatusMessage = result.Message;
                    if (!result.Success)
                    {
                        MessageBox.Show(result.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                IsFormVisible = false;
                await SearchAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeleteOfficerAsync()
        {
            if (SelectedOfficer == null)
            {
                MessageBox.Show("Vui lòng chọn cán bộ cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa cán bộ {SelectedOfficer.FullName} ({SelectedOfficer.OfficerCode}) không?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var result = await _officerService.DeleteOfficerAsync(SelectedOfficer.Id);
                StatusMessage = result.Message;
                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    await SearchAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa cán bộ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void OpenResetPasswordDialog()
        {
            if (SelectedOfficer == null)
            {
                MessageBox.Show("Vui lòng chọn cán bộ cần đặt lại mật khẩu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ResetOfficerName = $"{SelectedOfficer.FullName} ({SelectedOfficer.OfficerCode})";
            ResetNewPassword = "Password123@";
            IsResetPasswordDialogVisible = true;
        }

        [RelayCommand]
        public void CloseResetPasswordDialog()
        {
            IsResetPasswordDialogVisible = false;
            ResetNewPassword = string.Empty;
        }

        [RelayCommand]
        public async Task SaveResetPasswordAsync()
        {
            if (SelectedOfficer == null || string.IsNullOrWhiteSpace(ResetNewPassword))
            {
                MessageBox.Show("Mật khẩu mới không được để trống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _officerService.ResetOfficerPasswordAsync(SelectedOfficer.Id, ResetNewPassword);
                StatusMessage = result.Message;
                MessageBox.Show(result.Message, result.Success ? "Thành công" : "Thông báo", MessageBoxButton.OK,
                                result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (result.Success)
                {
                    IsResetPasswordDialogVisible = false;
                    await SearchAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đặt lại mật khẩu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ExportExcelAsync()
        {
            var filePath = _fileDialogService.ShowSaveFileDialog("Excel Files (*.xlsx)|*.xlsx", "Danh_Sach_Can_Bo_Quan_Su.xlsx");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ExportOfficersToExcelAsync(Officers, filePath);
                StatusMessage = result.Message;
                MessageBox.Show(result.Message, result.Success ? "Thành công" : "Lỗi", MessageBoxButton.OK,
                                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất file Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ImportExcelAsync()
        {
            var filePath = _fileDialogService.ShowOpenFileDialog("Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "Chọn file Excel danh sách cán bộ");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ImportOfficersFromExcelAsync(filePath);
                StatusMessage = result.Message;
                MessageBox.Show(result.Message, result.Success ? "Thành công" : "Lỗi", MessageBoxButton.OK,
                                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (result.Success)
                {
                    await SearchAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nhập file Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearForm()
        {
            FormOfficerCode = string.Empty;
            FormFullName = string.Empty;
            FormRank = FormRanks.FirstOrDefault() ?? "Đại úy";
            FormPosition = FormPositions.FirstOrDefault() ?? "Đại đội trưởng";
            FormUnit = FormUnits.FirstOrDefault() ?? "Đại đội 1";
            FormPhoneNumber = string.Empty;
            FormEmail = string.Empty;
            FormSpecialty = string.Empty;
            FormDateOfBirth = new DateTime(1990, 1, 1);
            FormEnlistmentDate = new DateTime(2010, 9, 1);
            FormNotes = string.Empty;
            FormCreateLoginAccount = false;
            FormUsername = string.Empty;
            FormPassword = string.Empty;
        }

        partial void OnSearchKeywordChanged(string value) => _ = SearchAsync();
        partial void OnSelectedFilterRankChanged(string value) => _ = SearchAsync();
        partial void OnSelectedFilterPositionChanged(string value) => _ = SearchAsync();
        partial void OnSelectedFilterUnitChanged(string value) => _ = SearchAsync();
        partial void OnFilterSpecialtyChanged(string value) => _ = SearchAsync();
        partial void OnSelectedHasAccountChanged(string value) => _ = SearchAsync();
        partial void OnSelectedHasAssignedClassesChanged(string value) => _ = SearchAsync();
    }
}
