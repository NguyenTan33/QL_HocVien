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
    public partial class ClassManagementViewModel : ViewModelBase
    {
        private readonly IClassService _classService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;
        private readonly ICatalogService _catalogService;
        private readonly IOfficerService _officerService;

        public ObservableCollection<MilitaryClass> Classes { get; } = new();

        public ObservableCollection<string> Units { get; } = new()
        {
            "Tất cả", "Đại đội 1", "Đại đội 2", "Đại đội 3", "Đại đội 4", "Tiểu đoàn 1"
        };

        public ObservableCollection<string> Majors { get; } = new()
        {
            "Tất cả", "Chỉ huy Tham mưu", "Hậu cần Quân sự", "Kỹ thuật Quân sự", "Trinh sát đặc nhiệm", "Thông tin liên lạc"
        };

        public ObservableCollection<string> AcademicYears { get; } = new()
        {
            "Tất cả", "2021 - 2025", "2022 - 2026", "2023 - 2027", "2024 - 2028", "2025 - 2029"
        };

        public ObservableCollection<string> HasOfficerList { get; } = new()
        {
            "Tất cả", "Đã phân công cán bộ", "Chưa phân công cán bộ"
        };

        public ObservableCollection<string> AvailableOfficers { get; } = new();

        // Tìm kiếm và bộ lọc
        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private string _selectedMajor = "Tất cả";

        [ObservableProperty]
        private string _selectedAcademicYear = "Tất cả";

        [ObservableProperty]
        private string _selectedHasOfficer = "Tất cả";

        [ObservableProperty]
        private int? _filterMinCadets;

        [ObservableProperty]
        private int? _filterMaxCadets;

        [ObservableProperty]
        private bool _isAdvancedFilterVisible;

        [ObservableProperty]
        private int _activeFilterCount;

        [ObservableProperty]
        private MilitaryClass? _selectedClass;

        [ObservableProperty]
        private bool _isAllSelected;

        [ObservableProperty]
        private int _selectedCount;

        private bool _isUpdatingSelection;

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
                foreach (var c in Classes)
                {
                    c.IsSelected = value;
                }
                SelectedCount = value ? Classes.Count : 0;
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        private void Class_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MilitaryClass.IsSelected))
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
                foreach (var c in Classes)
                {
                    if (c.IsSelected) count++;
                }
                SelectedCount = count;
                IsAllSelected = (Classes.Count > 0 && count == Classes.Count);
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        // Form Modal Thêm / Sửa
        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _formClassCode = string.Empty;

        [ObservableProperty]
        private string _formClassName = string.Empty;

        [ObservableProperty]
        private string _formUnit = "Đại đội 1";

        [ObservableProperty]
        private string _formMajor = "Chỉ huy Tham mưu";

        [ObservableProperty]
        private string _formOfficerInCharge = string.Empty;

        [ObservableProperty]
        private string _formAcademicYear = "2023 - 2027";

        [ObservableProperty]
        private string _formDescription = string.Empty;

        [ObservableProperty]
        private string _formErrorMessage = string.Empty;

        // Modal Danh sách học viên thuộc lớp
        [ObservableProperty]
        private bool _isCadetListVisible;

        [ObservableProperty]
        private MilitaryClass? _viewingClass;

        public ObservableCollection<Cadet> ClassCadets { get; } = new();

        public ClassManagementViewModel(
            IClassService classService,
            IExcelService excelService,
            IFileDialogService fileDialogService,
            ICatalogService catalogService,
            IOfficerService officerService)
        {
            _classService = classService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            _catalogService = catalogService;
            _officerService = officerService;
            Title = "Quản Lý Lớp Học Quân Đội";

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            await LoadDropdownsAsync();
            await LoadClassesAsync();
        }

        public async Task LoadDropdownsAsync()
        {
            try
            {
                var units = await _catalogService.GetUnitDropdownAsync();
                if (units.Any())
                {
                    Units.Clear();
                    Units.Add("Tất cả");
                    foreach (var u in units) Units.Add(u);
                }

                var majors = await _catalogService.GetMajorDropdownAsync();
                if (majors.Any())
                {
                    Majors.Clear();
                    Majors.Add("Tất cả");
                    foreach (var m in majors) Majors.Add(m);
                }

                var officers = await _officerService.GetAllOfficersAsync();
                AvailableOfficers.Clear();
                foreach (var off in officers)
                {
                    AvailableOfficers.Add($"{off.Rank} {off.FullName}");
                }
            }
            catch
            {
                // Fallback to defaults if any exception
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
            SelectedUnit = "Tất cả";
            SelectedMajor = "Tất cả";
            SelectedAcademicYear = "Tất cả";
            SelectedHasOfficer = "Tất cả";
            FilterMinCadets = null;
            FilterMaxCadets = null;
            _ = LoadClassesAsync();
        }

        [RelayCommand]
        public async Task LoadClassesAsync()
        {
            IsBusy = true;
            try
            {
                int count = 0;
                if (!string.IsNullOrWhiteSpace(SearchKeyword)) count++;
                if (SelectedUnit != "Tất cả") count++;
                if (SelectedMajor != "Tất cả") count++;
                if (SelectedAcademicYear != "Tất cả") count++;
                if (SelectedHasOfficer != "Tất cả") count++;
                if (FilterMinCadets.HasValue || FilterMaxCadets.HasValue) count++;
                ActiveFilterCount = count;

                bool? hasOfficer = SelectedHasOfficer == "Đã phân công cán bộ" ? true :
                                  SelectedHasOfficer == "Chưa phân công cán bộ" ? false : null;

                var criteria = new QL_HocVien.Models.Filters.ClassFilterCriteria
                {
                    Keyword = SearchKeyword,
                    Unit = SelectedUnit,
                    Major = SelectedMajor,
                    AcademicYear = SelectedAcademicYear,
                    HasOfficerAssigned = hasOfficer,
                    MinCadets = FilterMinCadets,
                    MaxCadets = FilterMaxCadets
                };

                var list = await _classService.SearchClassesAsync(criteria);
                Classes.Clear();
                foreach (var c in list)
                {
                    c.PropertyChanged += Class_PropertyChanged;
                    Classes.Add(c);
                }
                UpdateSelectionCount();
                StatusMessage = $"Đang hiển thị {Classes.Count} lớp học {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu lớp học: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenAddForm()
        {
            IsEditing = false;
            FormClassCode = string.Empty;
            FormClassName = string.Empty;
            FormUnit = "Đại đội 1";
            FormMajor = "Chỉ huy Tham mưu";
            FormOfficerInCharge = string.Empty;
            FormAcademicYear = "2023 - 2027";
            FormDescription = string.Empty;
            FormErrorMessage = string.Empty;
            IsFormVisible = true;
        }

        [RelayCommand]
        private void OpenEditForm(MilitaryClass? targetClass = null)
        {
            var c = targetClass ?? SelectedClass;
            if (c == null)
            {
                StatusMessage = "Vui lòng chọn lớp học cần chỉnh sửa.";
                return;
            }

            SelectedClass = c;
            IsEditing = true;
            FormClassCode = c.ClassCode;
            FormClassName = c.ClassName;
            FormUnit = c.Unit;
            FormMajor = c.Major;
            FormOfficerInCharge = c.OfficerInCharge;
            FormAcademicYear = c.AcademicYear;
            FormDescription = c.Description;
            FormErrorMessage = string.Empty;
            IsFormVisible = true;
        }

        [RelayCommand]
        private void CloseForm()
        {
            IsFormVisible = false;
        }

        [RelayCommand]
        private async Task SaveFormAsync()
        {
            FormErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormClassCode))
            {
                FormErrorMessage = "Vui lòng nhập Mã lớp học.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FormClassName))
            {
                FormErrorMessage = "Vui lòng nhập Tên lớp học.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditing && SelectedClass != null)
                {
                    SelectedClass.ClassCode = FormClassCode.Trim().ToUpper();
                    SelectedClass.ClassName = FormClassName.Trim();
                    SelectedClass.Unit = FormUnit;
                    SelectedClass.Major = FormMajor;
                    SelectedClass.OfficerInCharge = FormOfficerInCharge.Trim();
                    SelectedClass.AcademicYear = FormAcademicYear.Trim();
                    SelectedClass.Description = FormDescription.Trim();

                    var result = await _classService.UpdateClassAsync(SelectedClass);
                    if (result.Success)
                    {
                        IsFormVisible = false;
                        await LoadClassesAsync();
                        StatusMessage = result.Message;
                    }
                    else
                    {
                        FormErrorMessage = result.Message;
                    }
                }
                else
                {
                    var newClass = new MilitaryClass
                    {
                        ClassCode = FormClassCode.Trim().ToUpper(),
                        ClassName = FormClassName.Trim(),
                        Unit = FormUnit,
                        Major = FormMajor,
                        OfficerInCharge = FormOfficerInCharge.Trim(),
                        AcademicYear = FormAcademicYear.Trim(),
                        Description = FormDescription.Trim()
                    };

                    var result = await _classService.AddClassAsync(newClass);
                    if (result.Success)
                    {
                        IsFormVisible = false;
                        await LoadClassesAsync();
                        StatusMessage = result.Message;
                    }
                    else
                    {
                        FormErrorMessage = result.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                FormErrorMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteClassAsync(MilitaryClass? targetClass = null)
        {
            if (targetClass != null)
            {
                var confirmSingle = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa lớp học '{targetClass.ClassName}' (Mã: {targetClass.ClassCode})?\n" +
                    $"Quân số hiện tại: {targetClass.Cadets.Count} học viên.\n" +
                    "Các học viên sẽ không bị xóa mà được chuyển trạng thái lớp tự do.",
                    "Xác nhận xóa lớp học",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmSingle != MessageBoxResult.Yes) return;

                IsBusy = true;
                try
                {
                    var result = await _classService.DeleteClassAsync(targetClass.Id);
                    StatusMessage = result.Message;
                    SelectedClass = null;
                    await LoadClassesAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi khi xóa lớp: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                }
                return;
            }

            var selected = Classes.Where(cl => cl.IsSelected).ToList();
            if (!selected.Any() && SelectedClass != null)
            {
                selected.Add(SelectedClass);
            }

            if (!selected.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một lớp học để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {selected.Count} lớp học đã chọn không?\n\nCác học viên thuộc các lớp này sẽ không bị xóa mà được chuyển trạng thái lớp tự do.",
                "Xác nhận xóa lớp học",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            IsBusy = true;
            try
            {
                var result = await _classService.DeleteMultipleClassesAsync(selected.Select(cl => cl.Id));
                StatusMessage = result.Message;
                SelectedClass = null;
                await LoadClassesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khi xóa lớp: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ViewClassCadetsAsync(MilitaryClass? targetClass = null)
        {
            var c = targetClass ?? SelectedClass;
            if (c == null)
            {
                StatusMessage = "Vui lòng chọn lớp học để xem quân số.";
                return;
            }

            IsBusy = true;
            try
            {
                var detailed = await _classService.GetClassWithCadetsAsync(c.Id);
                ViewingClass = detailed ?? c;
                ClassCadets.Clear();
                if (detailed?.Cadets != null)
                {
                    foreach (var cadet in detailed.Cadets)
                    {
                        ClassCadets.Add(cadet);
                    }
                }
                IsCadetListVisible = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải học viên của lớp: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void CloseCadetList()
        {
            IsCadetListVisible = false;
            ViewingClass = null;
            ClassCadets.Clear();
        }

        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            var fileName = $"DanhSach_LopHoc_{DateTime.Today:yyyyMMdd}.xlsx";
            var filePath = _fileDialogService.ShowSaveFileDialog(fileName, "Excel Files (*.xlsx)|*.xlsx", "Xuất danh sách lớp học ra Excel");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var list = await _classService.GetAllClassesAsync();
                var result = await _excelService.ExportClassesToExcelAsync(list, filePath);
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
            var filePath = _fileDialogService.ShowOpenFileDialog("Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "Chọn tệp Excel danh sách lớp học");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ImportClassesFromExcelAsync(filePath);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadClassesAsync();
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

        partial void OnSearchKeywordChanged(string value) => _ = LoadClassesAsync();
        partial void OnSelectedUnitChanged(string value) => _ = LoadClassesAsync();
        partial void OnSelectedMajorChanged(string value) => _ = LoadClassesAsync();
        partial void OnSelectedAcademicYearChanged(string value) => _ = LoadClassesAsync();
        partial void OnSelectedHasOfficerChanged(string value) => _ = LoadClassesAsync();
        partial void OnFilterMinCadetsChanged(int? value) => _ = LoadClassesAsync();
        partial void OnFilterMaxCadetsChanged(int? value) => _ = LoadClassesAsync();
    }
}
