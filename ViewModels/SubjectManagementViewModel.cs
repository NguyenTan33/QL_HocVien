using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class SubjectManagementViewModel : ViewModelBase
    {
        private readonly ISubjectService _subjectService;

        public ObservableCollection<Subject> Subjects { get; } = new();
        public ObservableCollection<string> Categories { get; } = new()
        {
            "Tất cả", "Sức nhanh", "Sức mạnh", "Sức bền", "Bài tập tổng hợp", "Bơi tự do"
        };
        public ObservableCollection<string> FilterUnits { get; } = new()
        {
            "Tất cả", "lần", "giây", "phút:giây", "mét", "điểm"
        };
        public ObservableCollection<string> RuleOptions { get; } = new()
        {
            "Tất cả", "Chỉ số cao hơn tốt hơn (Lực/Lần)", "Thời gian ít hơn tốt hơn (Chạy)"
        };

        // Lọc nâng cao: Lọc theo Tên và Lọc theo ID / Mã môn
        [ObservableProperty]
        private string _filterSubjectCode = string.Empty;

        [ObservableProperty]
        private string _filterSubjectName = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "Tất cả";

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private string _selectedRule = "Tất cả";

        [ObservableProperty]
        private bool _isAdvancedFilterVisible;

        [ObservableProperty]
        private int _activeFilterCount;

        [ObservableProperty]
        private Subject? _selectedSubject;

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
                foreach (var s in Subjects)
                {
                    s.IsSelected = value;
                }
                SelectedCount = value ? Subjects.Count : 0;
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        private void Subject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Subject.IsSelected))
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
                foreach (var s in Subjects)
                {
                    if (s.IsSelected) count++;
                }
                SelectedCount = count;
                IsAllSelected = (Subjects.Count > 0 && count == Subjects.Count);
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        // Modal / Form thêm & sửa môn học
        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _formSubjectCode = string.Empty;

        [ObservableProperty]
        private string _formSubjectName = string.Empty;

        [ObservableProperty]
        private string _formCategory = "Sức mạnh";

        [ObservableProperty]
        private string _formUnit = "lần";

        [ObservableProperty]
        private string _formDescription = string.Empty;

        [ObservableProperty]
        private double _formExcellentThreshold = 20;

        [ObservableProperty]
        private double _formGoodThreshold = 16;

        [ObservableProperty]
        private double _formPassThreshold = 12;

        [ObservableProperty]
        private bool _formIsHigherBetter = true;

        [ObservableProperty]
        private string _formErrorMessage = string.Empty;

        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;

        public SubjectManagementViewModel(
            ISubjectService subjectService,
            IExcelService excelService,
            IFileDialogService fileDialogService)
        {
            _subjectService = subjectService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            Title = "Quản Lý Môn Học & Tiêu Chuẩn Thể Lực";

            _ = LoadSubjectsAsync();
        }

        [RelayCommand]
        public void ToggleAdvancedFilter()
        {
            IsAdvancedFilterVisible = !IsAdvancedFilterVisible;
        }

        [RelayCommand]
        public async Task LoadSubjectsAsync()
        {
            IsBusy = true;
            try
            {
                int count = 0;
                if (!string.IsNullOrWhiteSpace(FilterSubjectCode)) count++;
                if (!string.IsNullOrWhiteSpace(FilterSubjectName)) count++;
                if (SelectedCategory != "Tất cả") count++;
                if (SelectedUnit != "Tất cả") count++;
                if (SelectedRule != "Tất cả") count++;
                ActiveFilterCount = count;

                bool? isHigherBetter = SelectedRule.Contains("cao hơn") ? true :
                                       SelectedRule.Contains("ít hơn") ? false : null;

                var criteria = new QL_HocVien.Models.Filters.SubjectFilterCriteria
                {
                    SubjectCode = FilterSubjectCode,
                    SubjectName = FilterSubjectName,
                    Category = SelectedCategory,
                    Unit = SelectedUnit,
                    IsHigherBetter = isHigherBetter
                };

                var list = await _subjectService.SearchSubjectsAsync(criteria);

                Subjects.Clear();
                foreach (var item in list)
                {
                    item.PropertyChanged += Subject_PropertyChanged;
                    Subjects.Add(item);
                }
                UpdateSelectionCount();
                StatusMessage = $"Hiển thị {Subjects.Count} môn học {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
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
        private void OpenAddForm()
        {
            IsEditing = false;
            FormSubjectCode = string.Empty;
            FormSubjectName = string.Empty;
            FormCategory = "Sức mạnh";
            FormUnit = "lần";
            FormDescription = string.Empty;
            FormExcellentThreshold = 20;
            FormGoodThreshold = 16;
            FormPassThreshold = 12;
            FormIsHigherBetter = true;
            FormErrorMessage = string.Empty;
            IsFormVisible = true;
        }

        [RelayCommand]
        private void OpenEditForm()
        {
            if (SelectedSubject == null)
            {
                StatusMessage = "Vui lòng chọn môn học cần chỉnh sửa.";
                return;
            }

            IsEditing = true;
            FormSubjectCode = SelectedSubject.SubjectCode;
            FormSubjectName = SelectedSubject.SubjectName;
            FormCategory = SelectedSubject.Category;
            FormUnit = SelectedSubject.Unit;
            FormDescription = SelectedSubject.Description;
            FormExcellentThreshold = SelectedSubject.ExcellentThreshold;
            FormGoodThreshold = SelectedSubject.GoodThreshold;
            FormPassThreshold = SelectedSubject.PassThreshold;
            FormIsHigherBetter = SelectedSubject.IsHigherBetter;
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

            if (string.IsNullOrWhiteSpace(FormSubjectCode))
            {
                FormErrorMessage = "Vui lòng nhập Mã môn học.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FormSubjectName))
            {
                FormErrorMessage = "Vui lòng nhập Tên môn học.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditing && SelectedSubject != null)
                {
                    SelectedSubject.SubjectCode = FormSubjectCode.Trim().ToUpper();
                    SelectedSubject.SubjectName = FormSubjectName.Trim();
                    SelectedSubject.Category = FormCategory;
                    SelectedSubject.Unit = FormUnit;
                    SelectedSubject.Description = FormDescription;
                    SelectedSubject.ExcellentThreshold = FormExcellentThreshold;
                    SelectedSubject.GoodThreshold = FormGoodThreshold;
                    SelectedSubject.PassThreshold = FormPassThreshold;
                    SelectedSubject.IsHigherBetter = FormIsHigherBetter;

                    var result = await _subjectService.UpdateSubjectAsync(SelectedSubject);
                    if (result.Success)
                    {
                        IsFormVisible = false;
                        await LoadSubjectsAsync();
                        StatusMessage = result.Message;
                    }
                    else
                    {
                        FormErrorMessage = result.Message;
                    }
                }
                else
                {
                    var newSubject = new Subject
                    {
                        SubjectCode = FormSubjectCode.Trim().ToUpper(),
                        SubjectName = FormSubjectName.Trim(),
                        Category = FormCategory,
                        Unit = FormUnit,
                        Description = FormDescription,
                        ExcellentThreshold = FormExcellentThreshold,
                        GoodThreshold = FormGoodThreshold,
                        PassThreshold = FormPassThreshold,
                        IsHigherBetter = FormIsHigherBetter
                    };

                    var result = await _subjectService.AddSubjectAsync(newSubject);
                    if (result.Success)
                    {
                        IsFormVisible = false;
                        await LoadSubjectsAsync();
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
        private async Task DeleteSubjectAsync()
        {
            var selected = Subjects.Where(s => s.IsSelected).ToList();
            if (!selected.Any() && SelectedSubject != null)
            {
                selected.Add(SelectedSubject);
            }

            if (!selected.Any())
            {
                System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một môn học để xóa.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {selected.Count} môn học đã chọn không?\n\nLưu ý: Toàn bộ hồ sơ kiểm tra liên quan đến các môn này sẽ bị xóa theo.",
                "Xác nhận xóa môn học",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var result = await _subjectService.DeleteMultipleSubjectsAsync(selected.Select(s => s.Id));
                StatusMessage = result.Message;
                SelectedSubject = null;
                await LoadSubjectsAsync();
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
        private void ClearFilters()
        {
            FilterSubjectCode = string.Empty;
            FilterSubjectName = string.Empty;
            SelectedCategory = "Tất cả";
            SelectedUnit = "Tất cả";
            SelectedRule = "Tất cả";
            _ = LoadSubjectsAsync();
        }

        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            var fileName = $"DanhMuc_MonHoc_{DateTime.Today:yyyyMMdd}.xlsx";
            var filePath = _fileDialogService.ShowSaveFileDialog(fileName, "Excel Files (*.xlsx)|*.xlsx", "Xuất danh mục môn học ra Excel");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ExportSubjectsToExcelAsync(Subjects, filePath);
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
            var filePath = _fileDialogService.ShowOpenFileDialog("Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "Chọn tệp Excel danh mục môn học");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ImportSubjectsFromExcelAsync(filePath);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadSubjectsAsync();
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

        partial void OnFilterSubjectCodeChanged(string value) => _ = LoadSubjectsAsync();
        partial void OnFilterSubjectNameChanged(string value) => _ = LoadSubjectsAsync();
        partial void OnSelectedCategoryChanged(string value) => _ = LoadSubjectsAsync();
        partial void OnSelectedUnitChanged(string value) => _ = LoadSubjectsAsync();
        partial void OnSelectedRuleChanged(string value) => _ = LoadSubjectsAsync();
    }
}
