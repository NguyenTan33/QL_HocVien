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
    public partial class PhysicalExamViewModel : ViewModelBase
    {
        private readonly IPhysicalExamService _examService;
        private readonly ICadetService _cadetService;
        private readonly ISubjectService _subjectService;
        private readonly IEvaluationService _evaluationService;

        public ObservableCollection<PhysicalExamRecord> ExamRecords { get; } = new();
        public ObservableCollection<Cadet> Cadets { get; } = new();
        public ObservableCollection<Subject> Subjects { get; } = new();
        public ObservableCollection<string> GradeFilters { get; } = new()
        {
            "Tất cả", "Xuất sắc", "Giỏi", "Khá", "Đạt", "Không đạt"
        };
        public ObservableCollection<string> SubjectFilters { get; } = new() { "Tất cả các môn" };
        public ObservableCollection<string> SessionFilters { get; } = new() { "Tất cả" };
        public ObservableCollection<string> UnitFilters { get; } = new() { "Tất cả" };
        public ObservableCollection<string> ClassFilters { get; } = new() { "Tất cả" };

        [ObservableProperty]
        private string _searchCadetKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedGradeFilter = "Tất cả";

        [ObservableProperty]
        private string _selectedSubjectFilter = "Tất cả các môn";

        [ObservableProperty]
        private string _selectedSessionFilter = "Tất cả";

        [ObservableProperty]
        private string _selectedUnitFilter = "Tất cả";

        [ObservableProperty]
        private string _selectedClassFilter = "Tất cả";

        [ObservableProperty]
        private DateTime? _filterFromDate;

        [ObservableProperty]
        private DateTime? _filterToDate;

        [ObservableProperty]
        private bool _isAdvancedFilterVisible;

        [ObservableProperty]
        private int _activeFilterCount;

        [ObservableProperty]
        private PhysicalExamRecord? _selectedRecord;

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
                foreach (var r in ExamRecords)
                {
                    r.IsSelected = value;
                }
                SelectedCount = value ? ExamRecords.Count : 0;
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        private void ExamRecord_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PhysicalExamRecord.IsSelected))
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
                foreach (var r in ExamRecords)
                {
                    if (r.IsSelected) count++;
                }
                SelectedCount = count;
                IsAllSelected = (ExamRecords.Count > 0 && count == ExamRecords.Count);
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        // Form nhập kết quả kiểm tra
        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private Cadet? _formSelectedCadet;

        [ObservableProperty]
        private Subject? _formSelectedSubject;

        [ObservableProperty]
        private double _formScoreValue;

        [ObservableProperty]
        private string _formPreviewGrade = "Chưa xác định";

        [ObservableProperty]
        private string _formExamSession = "Kiểm tra Quý 3/2026";

        [ObservableProperty]
        private DateTime _formExamDate = DateTime.Today;

        [ObservableProperty]
        private string _formNotes = string.Empty;

        [ObservableProperty]
        private string _formErrorMessage = string.Empty;

        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;
        private readonly IClassService? _classService;
        private readonly ICatalogService? _catalogService;

        public PhysicalExamViewModel(
            IPhysicalExamService examService,
            ICadetService cadetService,
            ISubjectService subjectService,
            IEvaluationService evaluationService,
            IExcelService excelService,
            IFileDialogService fileDialogService,
            IClassService? classService = null,
            ICatalogService? catalogService = null)
        {
            _examService = examService;
            _cadetService = cadetService;
            _subjectService = subjectService;
            _evaluationService = evaluationService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            _classService = classService;
            _catalogService = catalogService;
            Title = "Kiểm Tra Rèn Luyện Thể Lực";

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadLookupsAsync();
            await LoadRecordsAsync();
        }

        public async Task LoadLookupsAsync()
        {
            var cadetList = await _cadetService.GetAllCadetsAsync();
            Cadets.Clear();
            foreach (var c in cadetList) Cadets.Add(c);

            var subjectList = await _subjectService.GetAllSubjectsAsync();
            Subjects.Clear();
            SubjectFilters.Clear();
            SubjectFilters.Add("Tất cả các môn");
            foreach (var s in subjectList)
            {
                Subjects.Add(s);
                SubjectFilters.Add(s.SubjectName);
            }

            try
            {
                var classes = await _cadetService.GetDistinctClassesAsync();
                ClassFilters.Clear();
                ClassFilters.Add("Tất cả");
                if (classes.Any())
                {
                    foreach (var c in classes) ClassFilters.Add(c);
                }
                else if (_classService != null)
                {
                    var fallbackClasses = await _classService.GetAllClassesAsync();
                    foreach (var c in fallbackClasses) ClassFilters.Add(c.ClassName);
                }
            }
            catch { }

            try
            {
                var units = await _cadetService.GetDistinctUnitsAsync();
                UnitFilters.Clear();
                UnitFilters.Add("Tất cả");
                if (units.Any())
                {
                    foreach (var u in units) UnitFilters.Add(u);
                }
                else if (_catalogService != null)
                {
                    var fallbackUnits = await _catalogService.GetUnitDropdownAsync();
                    foreach (var u in fallbackUnits) UnitFilters.Add(u);
                }
            }
            catch { }

            try
            {
                var allRecords = await _examService.GetAllRecordsAsync();
                var sessions = allRecords
                    .Select(r => r.ExamSession)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .OrderBy(s => s);
                SessionFilters.Clear();
                SessionFilters.Add("Tất cả");
                foreach (var s in sessions) SessionFilters.Add(s);
            }
            catch { }

            if (Cadets.Count > 0) FormSelectedCadet = Cadets[0];
            if (Subjects.Count > 0) FormSelectedSubject = Subjects[0];
        }

        [RelayCommand]
        public void ToggleAdvancedFilter()
        {
            IsAdvancedFilterVisible = !IsAdvancedFilterVisible;
        }

        [RelayCommand]
        public void ResetFilters()
        {
            SearchCadetKeyword = string.Empty;
            SelectedGradeFilter = "Tất cả";
            SelectedSubjectFilter = "Tất cả các môn";
            SelectedSessionFilter = "Tất cả";
            SelectedUnitFilter = "Tất cả";
            SelectedClassFilter = "Tất cả";
            FilterFromDate = null;
            FilterToDate = null;
            _ = LoadRecordsAsync();
        }

        [RelayCommand]
        public async Task LoadRecordsAsync()
        {
            IsBusy = true;
            try
            {
                int count = 0;
                if (!string.IsNullOrWhiteSpace(SearchCadetKeyword)) count++;
                if (SelectedGradeFilter != "Tất cả") count++;
                if (SelectedSubjectFilter != "Tất cả các môn") count++;
                if (SelectedSessionFilter != "Tất cả") count++;
                if (SelectedUnitFilter != "Tất cả") count++;
                if (SelectedClassFilter != "Tất cả") count++;
                if (FilterFromDate.HasValue || FilterToDate.HasValue) count++;
                ActiveFilterCount = count;

                int? subjectId = null;
                if (SelectedSubjectFilter != "Tất cả các môn")
                {
                    var s = Subjects.FirstOrDefault(x => x.SubjectName == SelectedSubjectFilter);
                    if (s != null) subjectId = s.Id;
                }

                var criteria = new QL_HocVien.Models.Filters.PhysicalExamFilterCriteria
                {
                    CadetKeyword = SearchCadetKeyword,
                    SubjectId = subjectId,
                    Grade = SelectedGradeFilter,
                    ExamSession = SelectedSessionFilter,
                    Unit = SelectedUnitFilter,
                    ClassName = SelectedClassFilter,
                    FromDate = FilterFromDate,
                    ToDate = FilterToDate
                };

                var list = await _examService.SearchRecordsAsync(criteria);
                ExamRecords.Clear();
                foreach (var r in list)
                {
                    r.PropertyChanged += ExamRecord_PropertyChanged;
                    ExamRecords.Add(r);
                }
                UpdateSelectionCount();
                StatusMessage = $"Đã tải {ExamRecords.Count} lượt kiểm tra {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
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
            if (Cadets.Count == 0 || Subjects.Count == 0)
            {
                StatusMessage = "Cần có ít nhất 1 học viên và 1 môn học để nhập điểm kiểm tra.";
                return;
            }

            FormSelectedCadet = Cadets.FirstOrDefault();
            FormSelectedSubject = Subjects.FirstOrDefault();
            FormScoreValue = 0;
            FormExamSession = "Kiểm tra Quý 3/2026";
            FormExamDate = DateTime.Today;
            FormNotes = string.Empty;
            FormErrorMessage = string.Empty;
            UpdatePreviewGrade();
            IsFormVisible = true;
        }

        [RelayCommand]
        private void CloseForm()
        {
            IsFormVisible = false;
        }

        [RelayCommand]
        private async Task SaveRecordAsync()
        {
            FormErrorMessage = string.Empty;

            if (FormSelectedCadet == null)
            {
                FormErrorMessage = "Vui lòng chọn học viên.";
                return;
            }

            if (FormSelectedSubject == null)
            {
                FormErrorMessage = "Vui lòng chọn môn kiểm tra.";
                return;
            }

            var record = new PhysicalExamRecord
            {
                CadetId = FormSelectedCadet.Id,
                SubjectId = FormSelectedSubject.Id,
                ScoreValue = FormScoreValue,
                ExamSession = FormExamSession,
                ExamDate = FormExamDate,
                Notes = FormNotes
            };

            IsBusy = true;
            try
            {
                var result = await _examService.AddExamRecordAsync(record);
                if (result.Success)
                {
                    IsFormVisible = false;
                    await LoadRecordsAsync();
                    StatusMessage = result.Message;
                }
                else
                {
                    FormErrorMessage = result.Message;
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
        private async Task DeleteRecordAsync()
        {
            var selected = ExamRecords.Where(r => r.IsSelected).ToList();
            if (!selected.Any() && SelectedRecord != null)
            {
                selected.Add(SelectedRecord);
            }

            if (!selected.Any())
            {
                System.Windows.MessageBox.Show("Vui lòng chọn ít nhất một kết quả kiểm tra để xóa.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {selected.Count} kết quả kiểm tra thể lực đã chọn không?\n\nHành động này không thể hoàn tác!",
                "Xác nhận xóa kết quả",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var result = await _examService.DeleteMultipleExamRecordsAsync(selected.Select(r => r.Id));
                StatusMessage = result.Message;
                SelectedRecord = null;
                await LoadRecordsAsync();
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
        private async Task ExportExcelAsync()
        {
            var fileName = $"KetQua_KiemTraTheLuc_{DateTime.Today:yyyyMMdd}.xlsx";
            var filePath = _fileDialogService.ShowSaveFileDialog(fileName, "Excel Files (*.xlsx)|*.xlsx", "Xuất kết quả kiểm tra thể lực ra Excel");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ExportExamRecordsToExcelAsync(ExamRecords, filePath);
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
            var filePath = _fileDialogService.ShowOpenFileDialog("Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "Chọn tệp Excel kết quả kiểm tra thể lực");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ImportExamRecordsFromExcelAsync(filePath);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadRecordsAsync();
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

        private void UpdatePreviewGrade()
        {
            if (FormSelectedSubject != null)
            {
                FormPreviewGrade = _evaluationService.EvaluateGrade(FormSelectedSubject, FormScoreValue);
            }
            else
            {
                FormPreviewGrade = "Chưa xác định";
            }
        }

        partial void OnFormSelectedSubjectChanged(Subject? value) => UpdatePreviewGrade();
        partial void OnFormScoreValueChanged(double value) => UpdatePreviewGrade();
        partial void OnSearchCadetKeywordChanged(string value) => _ = LoadRecordsAsync();
        partial void OnSelectedGradeFilterChanged(string value) => _ = LoadRecordsAsync();
        partial void OnSelectedSubjectFilterChanged(string value) => _ = LoadRecordsAsync();
        partial void OnSelectedSessionFilterChanged(string value) => _ = LoadRecordsAsync();
        partial void OnSelectedUnitFilterChanged(string value) => _ = LoadRecordsAsync();
        partial void OnSelectedClassFilterChanged(string value) => _ = LoadRecordsAsync();
        partial void OnFilterFromDateChanged(DateTime? value) => _ = LoadRecordsAsync();
        partial void OnFilterToDateChanged(DateTime? value) => _ = LoadRecordsAsync();
    }
}
