using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly IClassService _classService;
        private readonly IAuthService _authService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;
        private readonly ICatalogService _catalogService;

        public ObservableCollection<Cadet> Cadets { get; } = new();
        public ObservableCollection<string> RankList { get; } = new()
        {
            "Tất cả", "Binh nhì", "Binh nhất", "Hạ sĩ", "Trung sĩ", "Thượng sĩ", "Chuẩn úy", "Thiếu úy", "Trung úy", "Thượng úy", "Đại úy"
        };
        public ObservableCollection<string> UnitList { get; } = new()
        {
            "Tất cả", "Đại đội 1", "Đại đội 2", "Đại đội 3", "Đại đội 4", "Trung đội 1", "Trung đội 2", "Trung đội 3"
        };
        public ObservableCollection<string> ClassList { get; } = new() { "Tất cả" };
        public ObservableCollection<string> PositionList { get; } = new()
        {
            "Tất cả", "Học viên", "Chiến sĩ", "Tiểu đội trưởng", "Lớp phó", "Lớp trưởng"
        };
        public ObservableCollection<string> GenderList { get; } = new() { "Tất cả", "Nam", "Nữ" };
        public ObservableCollection<string> HasAccountList { get; } = new() { "Tất cả", "Đã có tài khoản", "Chưa có tài khoản" };
        public ObservableCollection<string> FitnessGradeList { get; } = new()
        {
            "Tất cả", "Đạt chuẩn", "Không đạt", "Xuất sắc", "Giỏi", "Khá", "Đạt", "Chưa kiểm tra"
        };

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedRank = "Tất cả";

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private string _selectedClass = "Tất cả";

        [ObservableProperty]
        private string _selectedPosition = "Tất cả";

        [ObservableProperty]
        private string _selectedGender = "Tất cả";

        [ObservableProperty]
        private int? _filterMinAge;

        [ObservableProperty]
        private int? _filterMaxAge;

        [ObservableProperty]
        private string _selectedHasAccount = "Tất cả";

        [ObservableProperty]
        private string _selectedFitnessGrade = "Tất cả";

        [ObservableProperty]
        private bool _isAdvancedFilterVisible;

        [ObservableProperty]
        private int _activeFilterCount;

        [ObservableProperty]
        private Cadet? _selectedCadet;

        // Cho việc sửa nhanh trực tiếp
        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editCadetCode = string.Empty;

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

        // Chọn nhiều & xóa hàng loạt
        [ObservableProperty]
        private bool _isAllSelected;

        [ObservableProperty]
        private int _selectedCount;

        private bool _isUpdatingSelection;

        // Cho đặt lại mật khẩu học viên
        [ObservableProperty]
        private bool _isResetPasswordDialogVisible;

        [ObservableProperty]
        private string _newCadetPassword = string.Empty;

        [ObservableProperty]
        private int _totalFilteredCount;

        public event Action? OnRequestAddCadet;
        public event Action? OnRequestManageUnits;

        private bool _isSuppressingFilterEvents;
        private readonly System.Threading.SemaphoreSlim _loadLock = new(1, 1);

        public CadetManagementViewModel(
            ICadetService cadetService,
            IClassService classService,
            IAuthService authService,
            IExcelService excelService,
            IFileDialogService fileDialogService,
            ICatalogService catalogService)
        {
            _cadetService = cadetService;
            _classService = classService;
            _authService = authService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            _catalogService = catalogService;
            Title = "Quản Lý Danh Sách Học Viên";

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            _isSuppressingFilterEvents = true;
            try
            {
                await LoadCatalogDropdownsInternalAsync();
                await LoadClassListInternalAsync();
                EnsureFilterDefaults();
            }
            finally
            {
                _isSuppressingFilterEvents = false;
            }

            await LoadCadetsAsync();
        }

        public async Task LoadCatalogDropdownsAsync()
        {
            _isSuppressingFilterEvents = true;
            try
            {
                await LoadCatalogDropdownsInternalAsync();
                EnsureFilterDefaults();
            }
            finally
            {
                _isSuppressingFilterEvents = false;
            }
        }

        private async Task LoadCatalogDropdownsInternalAsync()
        {
            try
            {
                var distinctUnits = await _cadetService.GetDistinctUnitsAsync();
                UnitList.Clear();
                UnitList.Add("Tất cả");
                if (distinctUnits.Any())
                {
                    foreach (var u in distinctUnits) UnitList.Add(u);
                }
                else
                {
                    var units = await _catalogService.GetUnitDropdownAsync();
                    foreach (var u in units) UnitList.Add(u);
                }

                var distinctRanks = await _cadetService.GetDistinctRanksAsync();
                RankList.Clear();
                RankList.Add("Tất cả");
                if (distinctRanks.Any())
                {
                    foreach (var r in distinctRanks) RankList.Add(r);
                }
                else
                {
                    var ranks = await _catalogService.GetRankDropdownAsync();
                    foreach (var r in ranks) RankList.Add(r);
                }

                var distinctPositions = await _cadetService.GetDistinctPositionsAsync();
                PositionList.Clear();
                PositionList.Add("Tất cả");
                if (distinctPositions.Any())
                {
                    foreach (var p in distinctPositions) PositionList.Add(p);
                }
                else
                {
                    var positions = await _catalogService.GetPositionDropdownAsync();
                    foreach (var p in positions) PositionList.Add(p);
                }
            }
            catch
            {
                // Giữ mặc định
            }
        }

        public async Task LoadClassListAsync()
        {
            _isSuppressingFilterEvents = true;
            try
            {
                await LoadClassListInternalAsync();
                EnsureFilterDefaults();
            }
            finally
            {
                _isSuppressingFilterEvents = false;
            }
        }

        private async Task LoadClassListInternalAsync()
        {
            try
            {
                var distinctClasses = await _cadetService.GetDistinctClassesAsync();
                ClassList.Clear();
                ClassList.Add("Tất cả");
                if (distinctClasses.Any())
                {
                    foreach (var c in distinctClasses) ClassList.Add(c);
                }
                else
                {
                    var classes = await _classService.GetAllClassesAsync();
                    foreach (var c in classes) ClassList.Add(c.ClassName);
                }
            }
            catch
            {
                // Fallback
            }
        }

        private void EnsureFilterDefaults()
        {
            if (string.IsNullOrEmpty(SelectedUnit) || !UnitList.Contains(SelectedUnit))
                SelectedUnit = "Tất cả";
            if (string.IsNullOrEmpty(SelectedRank) || !RankList.Contains(SelectedRank))
                SelectedRank = "Tất cả";
            if (string.IsNullOrEmpty(SelectedPosition) || !PositionList.Contains(SelectedPosition))
                SelectedPosition = "Tất cả";
            if (string.IsNullOrEmpty(SelectedClass) || !ClassList.Contains(SelectedClass))
                SelectedClass = "Tất cả";
            if (string.IsNullOrEmpty(SelectedGender))
                SelectedGender = "Tất cả";
            if (string.IsNullOrEmpty(SelectedHasAccount))
                SelectedHasAccount = "Tất cả";
            if (string.IsNullOrEmpty(SelectedFitnessGrade))
                SelectedFitnessGrade = "Tất cả";
        }

        [RelayCommand]
        public void ToggleAdvancedFilter()
        {
            IsAdvancedFilterVisible = !IsAdvancedFilterVisible;
        }

        [RelayCommand]
        public async Task ResetFilters()
        {
            _isSuppressingFilterEvents = true;
            try
            {
                SearchKeyword = string.Empty;
                SelectedRank = "Tất cả";
                SelectedUnit = "Tất cả";
                SelectedClass = "Tất cả";
                SelectedPosition = "Tất cả";
                SelectedGender = "Tất cả";
                FilterMinAge = null;
                FilterMaxAge = null;
                SelectedHasAccount = "Tất cả";
                SelectedFitnessGrade = "Tất cả";
            }
            finally
            {
                _isSuppressingFilterEvents = false;
            }
            await LoadCadetsAsync();
        }

        [RelayCommand]
        public async Task LoadCadetsAsync()
        {
            if (_isSuppressingFilterEvents) return;

            await _loadLock.WaitAsync();
            IsBusy = true;
            try
            {
                var rank = string.IsNullOrWhiteSpace(SelectedRank) ? "Tất cả" : SelectedRank;
                var unit = string.IsNullOrWhiteSpace(SelectedUnit) ? "Tất cả" : SelectedUnit;
                var className = string.IsNullOrWhiteSpace(SelectedClass) ? "Tất cả" : SelectedClass;
                var position = string.IsNullOrWhiteSpace(SelectedPosition) ? "Tất cả" : SelectedPosition;
                var gender = string.IsNullOrWhiteSpace(SelectedGender) ? "Tất cả" : SelectedGender;
                var grade = string.IsNullOrWhiteSpace(SelectedFitnessGrade) ? "Tất cả" : SelectedFitnessGrade;
                var account = string.IsNullOrWhiteSpace(SelectedHasAccount) ? "Tất cả" : SelectedHasAccount;

                int count = 0;
                if (!string.IsNullOrWhiteSpace(SearchKeyword)) count++;
                if (rank != "Tất cả") count++;
                if (unit != "Tất cả") count++;
                if (className != "Tất cả") count++;
                if (position != "Tất cả") count++;
                if (gender != "Tất cả") count++;
                if (FilterMinAge.HasValue || FilterMaxAge.HasValue) count++;
                if (account != "Tất cả") count++;
                if (grade != "Tất cả") count++;
                ActiveFilterCount = count;

                bool? hasAccount = account == "Đã có tài khoản" ? true :
                                   account == "Chưa có tài khoản" ? false : null;

                var criteria = new QL_HocVien.Models.Filters.CadetFilterCriteria
                {
                    Keyword = SearchKeyword,
                    Rank = rank,
                    Unit = unit,
                    ClassName = className,
                    Position = position,
                    Gender = gender,
                    MinAge = FilterMinAge,
                    MaxAge = FilterMaxAge,
                    HasAccount = hasAccount,
                    FitnessGrade = grade
                };

                var list = await _cadetService.SearchCadetsAsync(criteria);

                foreach (var c in Cadets)
                {
                    c.PropertyChanged -= Cadet_PropertyChanged;
                }
                Cadets.Clear();
                foreach (var cadet in list)
                {
                    cadet.IsSelected = false;
                    cadet.PropertyChanged += Cadet_PropertyChanged;
                    Cadets.Add(cadet);
                }
                TotalFilteredCount = Cadets.Count;
                UpdateSelectionCount();
                StatusMessage = $"Đã tải {Cadets.Count} học viên {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                _loadLock.Release();
            }
        }

        [RelayCommand]
        private void NavigateToUnits()
        {
            OnRequestManageUnits?.Invoke();
        }

        public string SelectAllButtonText => IsAllSelected ? "⬜ Bỏ chọn" : "☑️ Chọn tất cả";

        [RelayCommand]
        public void ToggleSelectAll()
        {
            IsAllSelected = !IsAllSelected;
        }

        partial void OnIsAllSelectedChanged(bool value)
        {
            if (_isUpdatingSelection) return;
            _isUpdatingSelection = true;
            try
            {
                foreach (var c in Cadets)
                {
                    c.IsSelected = value;
                }
                SelectedCount = value ? Cadets.Count : 0;
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        private void Cadet_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Cadet.IsSelected))
            {
                UpdateSelectionCount();
            }
        }

        [RelayCommand]
        public void UpdateSelectionCount()
        {
            if (_isUpdatingSelection) return;
            _isUpdatingSelection = true;
            try
            {
                int count = 0;
                foreach (var c in Cadets)
                {
                    if (c.IsSelected) count++;
                }
                SelectedCount = count;
                IsAllSelected = (Cadets.Count > 0 && count == Cadets.Count);
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        [RelayCommand]
        public void SelectAll()
        {
            IsAllSelected = true;
        }

        [RelayCommand]
        public void DeselectAll()
        {
            IsAllSelected = false;
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

            EditCadetCode = SelectedCadet.CadetCode;
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

            if (string.IsNullOrWhiteSpace(EditCadetCode))
            {
                StatusMessage = "Mã học viên (ID) không được để trống.";
                return;
            }

            SelectedCadet.CadetCode = EditCadetCode.Trim();
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
                    await LoadCatalogDropdownsAsync();
                    await LoadClassListAsync();
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
            var selected = Cadets.Where(c => c.IsSelected).ToList();
            if (!selected.Any() && SelectedCadet != null)
            {
                selected.Add(SelectedCadet);
            }

            if (!selected.Any())
            {
                System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một học viên để xóa.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {selected.Count} học viên đã chọn?\n\nLưu ý: Tất cả hồ sơ kết quả kiểm tra thể lực và điểm môn học tín chỉ liên quan sẽ được tự động xóa theo.",
                "Xác nhận xóa học viên",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var result = await _cadetService.DeleteMultipleCadetsAsync(selected.Select(c => c.Id));
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadCatalogDropdownsAsync();
                    await LoadClassListAsync();
                    await LoadCadetsAsync();
                    SelectedCadet = null;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi xóa học viên: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteSelectedAsync()
        {
            var selectedIds = Cadets.Where(c => c.IsSelected).Select(c => c.Id).ToList();
            if (!selectedIds.Any())
            {
                StatusMessage = "Vui lòng chọn ít nhất một học viên để xóa.";
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {selectedIds.Count} học viên đã chọn?\n\nLưu ý: Tất cả hồ sơ kết quả kiểm tra thể lực và điểm môn học tín chỉ liên quan sẽ được tự động xóa theo.",
                "Xác nhận xóa học viên đã chọn",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var result = await _cadetService.DeleteMultipleCadetsAsync(selectedIds);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadCatalogDropdownsAsync();
                    await LoadClassListAsync();
                    await LoadCadetsAsync();
                    SelectedCadet = null;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi xóa nhiều học viên: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteAllFilteredAsync()
        {
            var allIds = Cadets.Select(c => c.Id).ToList();
            if (!allIds.Any())
            {
                StatusMessage = "Danh sách hiện tại không có học viên nào để xóa.";
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"CẢNH BÁO QUAN TRỌNG!\n\nBạn đang yêu cầu xóa TOÀN BỘ {allIds.Count} học viên đang hiển thị theo bộ lọc.\nToàn bộ dữ liệu điểm số, thành tích của các học viên này sẽ bị xóa vĩnh viễn!\n\nBạn có thực sự muốn xóa?",
                "Cảnh báo xóa toàn bộ học viên",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Stop);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var result = await _cadetService.DeleteMultipleCadetsAsync(allIds);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadCatalogDropdownsAsync();
                    await LoadClassListAsync();
                    await LoadCadetsAsync();
                    SelectedCadet = null;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi xóa toàn bộ học viên: {ex.Message}";
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

        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            var fileName = $"DanhSach_HocVien_{DateTime.Today:yyyyMMdd}.xlsx";
            var filePath = _fileDialogService.ShowSaveFileDialog(fileName, "Excel Files (*.xlsx)|*.xlsx", "Xuất danh sách học viên ra Excel");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ExportCadetsToExcelAsync(Cadets, filePath);
                StatusMessage = result.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi xuất Excel: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ImportExcelAsync()
        {
            var filePath = _fileDialogService.ShowOpenFileDialog("Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "Chọn tệp Excel danh sách học viên");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ImportCadetsFromExcelAsync(filePath);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadCatalogDropdownsAsync();
                    await LoadClassListAsync();
                    await LoadCadetsAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi nhập Excel: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSearchKeywordChanged(string value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnSelectedRankChanged(string value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnSelectedUnitChanged(string value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnSelectedClassChanged(string value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnSelectedPositionChanged(string value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnSelectedGenderChanged(string value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnFilterMinAgeChanged(int? value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnFilterMaxAgeChanged(int? value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnSelectedHasAccountChanged(string value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
        partial void OnSelectedFitnessGradeChanged(string value) { if (!_isSuppressingFilterEvents) _ = LoadCadetsAsync(); }
    }
}
