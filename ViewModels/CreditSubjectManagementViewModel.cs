using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class CreditSubjectManagementViewModel : ViewModelBase
    {
        private readonly ICreditSubjectService _creditService;
        private readonly ICadetService _cadetService;
        private readonly ICatalogService _catalogService;
        private readonly IClassService _classService;
        private readonly IFileDialogService _fileDialogService;

        #region PROPERTIES & COLLECTIONS
        public ObservableCollection<CreditSubject> Subjects { get; } = new();
        public ObservableCollection<CadetAcademicSummaryDto> CadetSummaries { get; } = new();
        public ObservableCollection<Cadet> AllCadets { get; } = new();

        public ObservableCollection<string> UnitOptions { get; } = new();
        public ObservableCollection<string> ClassOptions { get; } = new();
        public ObservableCollection<string> AssessmentTypes { get; } = new()
        {
            "Kiểm tra và thi",
            "Kiểm tra thường xuyên"
        };

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private string _selectedClass = "Tất cả";

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private int _totalStudentsCount;

        [ObservableProperty]
        private int _totalSubjectsCount;

        [ObservableProperty]
        private double _averageOverallGpa;

        [ObservableProperty]
        private int _excellentStudentsCount;

        [ObservableProperty]
        private int _selectedTabIndex = 0; // 0: Bảng điểm học viên, 1: Danh mục môn tín chỉ
        #endregion

        #region FORM QUẢN LÝ MÔN HỌC TÍN CHỈ
        [ObservableProperty]
        private bool _isSubjectFormVisible;

        [ObservableProperty]
        private bool _isEditingSubject;

        [ObservableProperty]
        private int _editingSubjectId;

        [ObservableProperty]
        private string _subjectCode = string.Empty;

        [ObservableProperty]
        private string _subjectName = string.Empty;

        [ObservableProperty]
        private int _credits = 2;

        [ObservableProperty]
        private string _assessmentType = "Kiểm tra và thi";

        [ObservableProperty]
        private string _subjectDescription = string.Empty;
        #endregion

        #region FORM NHẬP / CẬP NHẬT ĐIỂM
        [ObservableProperty]
        private bool _isScoreFormVisible;

        [ObservableProperty]
        private Cadet? _selectedCadetForScore;

        [ObservableProperty]
        private CreditSubject? _selectedSubjectForScore;

        [ObservableProperty]
        private double? _inputRegularScore;

        [ObservableProperty]
        private double? _inputExamScore;

        [ObservableProperty]
        private double _inputFinalScore;

        [ObservableProperty]
        private string _inputExamSession = "Học kỳ 1";

        [ObservableProperty]
        private DateTime _inputExamDate = DateTime.Today;

        [ObservableProperty]
        private string _inputScoreNotes = string.Empty;
        #endregion

        public CreditSubjectManagementViewModel(
            ICreditSubjectService creditService,
            ICadetService cadetService,
            ICatalogService catalogService,
            IClassService classService,
            IFileDialogService fileDialogService)
        {
            _creditService = creditService;
            _cadetService = cadetService;
            _catalogService = catalogService;
            _classService = classService;
            _fileDialogService = fileDialogService;

            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                // Nạp đơn vị thực tế từ học viên
                var units = await _cadetService.GetDistinctUnitsAsync();
                UnitOptions.Clear();
                UnitOptions.Add("Tất cả");
                if (units.Any())
                {
                    foreach (var u in units) UnitOptions.Add(u);
                }
                else
                {
                    var fallbackUnits = await _catalogService.GetAllUnitsAsync();
                    foreach (var u in fallbackUnits.OrderBy(u => u.UnitName))
                        UnitOptions.Add(u.UnitName);
                }

                // Nạp lớp thực tế từ học viên
                var classes = await _cadetService.GetDistinctClassesAsync();
                ClassOptions.Clear();
                ClassOptions.Add("Tất cả");
                if (classes.Any())
                {
                    foreach (var c in classes) ClassOptions.Add(c);
                }
                else
                {
                    var fallbackClasses = await _classService.GetAllClassesAsync();
                    foreach (var c in fallbackClasses.OrderBy(c => c.ClassName))
                        ClassOptions.Add(c.ClassName);
                }

                // Nạp học viên
                var cadets = await _cadetService.GetAllCadetsAsync();
                AllCadets.Clear();
                foreach (var c in cadets.OrderBy(c => c.FullName))
                    AllCadets.Add(c);

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khởi tạo dữ liệu tín chỉ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                // 1. Tải danh mục môn học tín chỉ
                var subjs = await _creditService.GetAllSubjectsAsync();
                Subjects.Clear();
                foreach (var s in subjs) Subjects.Add(s);
                TotalSubjectsCount = Subjects.Count;

                // 2. Tải bảng điểm học viên
                var summaries = await _creditService.GetCadetAcademicSummariesAsync(
                    SelectedUnit, SelectedClass, SearchKeyword);

                CadetSummaries.Clear();
                foreach (var sum in summaries) CadetSummaries.Add(sum);

                TotalStudentsCount = CadetSummaries.Count;
                AverageOverallGpa = CadetSummaries.Any(c => c.TotalCreditsEarned > 0)
                    ? Math.Round(CadetSummaries.Where(c => c.TotalCreditsEarned > 0).Average(c => c.Gpa), 2)
                    : 0;

                ExcellentStudentsCount = CadetSummaries.Count(c => c.Gpa >= 8.5 && c.TotalCreditsEarned > 0);

                StatusMessage = $"Đã tải thành công {TotalStudentsCount} học viên, {TotalSubjectsCount} môn học tín chỉ.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi nạp dữ liệu: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task FilterAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task ResetFilterAsync()
        {
            SelectedUnit = "Tất cả";
            SelectedClass = "Tất cả";
            SearchKeyword = string.Empty;
            await LoadDataAsync();
        }

        #region SUBJECT ACTIONS
        [RelayCommand]
        public void OpenAddSubjectForm()
        {
            IsEditingSubject = false;
            EditingSubjectId = 0;
            SubjectCode = $"TC{DateTime.Now:yyMM}{Subjects.Count + 1:D2}";
            SubjectName = string.Empty;
            Credits = 2;
            AssessmentType = "Kiểm tra và thi";
            SubjectDescription = string.Empty;
            IsSubjectFormVisible = true;
        }

        [RelayCommand]
        public void OpenEditSubjectForm(CreditSubject? subject)
        {
            if (subject == null) return;
            IsEditingSubject = true;
            EditingSubjectId = subject.Id;
            SubjectCode = subject.SubjectCode;
            SubjectName = subject.SubjectName;
            Credits = subject.Credits;
            AssessmentType = subject.AssessmentType;
            SubjectDescription = subject.Description;
            IsSubjectFormVisible = true;
        }

        [RelayCommand]
        public void CloseSubjectForm()
        {
            IsSubjectFormVisible = false;
        }

        [RelayCommand]
        public async Task SaveSubjectFormAsync()
        {
            if (string.IsNullOrWhiteSpace(SubjectCode) || string.IsNullOrWhiteSpace(SubjectName))
            {
                StatusMessage = "Vui lòng nhập đầy đủ mã môn và tên môn học.";
                return;
            }

            if (Credits <= 0)
            {
                StatusMessage = "Số tín chỉ phải lớn hơn 0.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditingSubject)
                {
                    var updated = new CreditSubject
                    {
                        Id = EditingSubjectId,
                        SubjectCode = SubjectCode.Trim(),
                        SubjectName = SubjectName.Trim(),
                        Credits = Credits,
                        AssessmentType = AssessmentType,
                        Description = SubjectDescription?.Trim() ?? string.Empty
                    };
                    var res = await _creditService.UpdateSubjectAsync(updated);
                    StatusMessage = res.Message;
                }
                else
                {
                    var newSubj = new CreditSubject
                    {
                        SubjectCode = SubjectCode.Trim(),
                        SubjectName = SubjectName.Trim(),
                        Credits = Credits,
                        AssessmentType = AssessmentType,
                        Description = SubjectDescription?.Trim() ?? string.Empty
                    };
                    var res = await _creditService.AddSubjectAsync(newSubj);
                    StatusMessage = res.Message;
                }

                IsSubjectFormVisible = false;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi lưu môn học: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeleteSubjectAsync(CreditSubject? subject)
        {
            if (subject == null) return;
            IsBusy = true;
            try
            {
                var res = await _creditService.DeleteSubjectAsync(subject.Id);
                StatusMessage = res.Message;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi xóa môn học: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region SCORE ACTIONS
        [RelayCommand]
        public void OpenAddScoreForm(CadetAcademicSummaryDto? summary)
        {
            if (summary != null)
            {
                SelectedCadetForScore = AllCadets.FirstOrDefault(c => c.Id == summary.CadetId);
            }
            else
            {
                SelectedCadetForScore = AllCadets.FirstOrDefault();
            }

            SelectedSubjectForScore = Subjects.FirstOrDefault();
            InputRegularScore = 8.0;
            InputExamScore = 8.0;
            InputFinalScore = 8.0;
            InputExamSession = "Học kỳ 1";
            InputExamDate = DateTime.Today;
            InputScoreNotes = string.Empty;
            IsScoreFormVisible = true;
        }

        [RelayCommand]
        public void AutoCalculateFinalScore()
        {
            if (SelectedSubjectForScore == null) return;

            if (SelectedSubjectForScore.AssessmentType == "Kiểm tra thường xuyên")
            {
                InputFinalScore = InputRegularScore ?? 0;
            }
            else
            {
                // Kiểm tra và thi: 30% KTTX + 70% Điểm thi
                double reg = InputRegularScore ?? 0;
                double exam = InputExamScore ?? reg;
                InputFinalScore = Math.Round(reg * 0.3 + exam * 0.7, 1);
            }
        }

        [RelayCommand]
        public void CloseScoreForm()
        {
            IsScoreFormVisible = false;
        }

        [RelayCommand]
        public async Task SaveScoreFormAsync()
        {
            if (SelectedCadetForScore == null || SelectedSubjectForScore == null)
            {
                StatusMessage = "Vui lòng chọn học viên và môn học tín chỉ.";
                return;
            }

            if (InputFinalScore < 0 || InputFinalScore > 10)
            {
                StatusMessage = "Điểm số phải từ 0.0 đến 10.0.";
                return;
            }

            IsBusy = true;
            try
            {
                var record = new CreditScoreRecord
                {
                    CadetId = SelectedCadetForScore.Id,
                    CreditSubjectId = SelectedSubjectForScore.Id,
                    RegularScore = InputRegularScore,
                    ExamScore = SelectedSubjectForScore.AssessmentType == "Kiểm tra thường xuyên" ? null : InputExamScore,
                    FinalScore = InputFinalScore,
                    ExamSession = InputExamSession,
                    ExamDate = InputExamDate,
                    Notes = InputScoreNotes?.Trim() ?? string.Empty
                };

                var res = await _creditService.SaveScoreAsync(record);
                StatusMessage = res.Message;
                IsScoreFormVisible = false;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi lưu điểm: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region EXPORT EXCEL
        [RelayCommand]
        public async Task ExportToExcelAsync()
        {
            if (CadetSummaries.Count == 0)
            {
                StatusMessage = "Không có dữ liệu học viên để xuất báo cáo.";
                return;
            }

            string defaultFileName = $"BangDiem_TinChi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string? filePath = _fileDialogService.ShowSaveFileDialog(defaultFileName);

            if (string.IsNullOrWhiteSpace(filePath))
                return;

            IsBusy = true;
            try
            {
                var subjs = Subjects.ToList();
                var list = CadetSummaries.ToList();
                var res = await _creditService.ExportAcademicReportAsync(filePath, list, subjs);
                StatusMessage = res.Message;
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
        #endregion
    }
}
