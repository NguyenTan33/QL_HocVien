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

        [ObservableProperty]
        private string _searchCadetKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedGradeFilter = "Tất cả";

        [ObservableProperty]
        private PhysicalExamRecord? _selectedRecord;

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

        public PhysicalExamViewModel(
            IPhysicalExamService examService,
            ICadetService cadetService,
            ISubjectService subjectService,
            IEvaluationService evaluationService)
        {
            _examService = examService;
            _cadetService = cadetService;
            _subjectService = subjectService;
            _evaluationService = evaluationService;
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
            foreach (var s in subjectList) Subjects.Add(s);

            if (Cadets.Count > 0) FormSelectedCadet = Cadets[0];
            if (Subjects.Count > 0) FormSelectedSubject = Subjects[0];
        }

        [RelayCommand]
        public async Task LoadRecordsAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _examService.SearchRecordsAsync(SearchCadetKeyword, null, SelectedGradeFilter, null);
                ExamRecords.Clear();
                foreach (var r in list)
                {
                    ExamRecords.Add(r);
                }
                StatusMessage = $"Đã tải {ExamRecords.Count} lượt kiểm tra.";
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
            if (SelectedRecord == null)
            {
                StatusMessage = "Vui lòng chọn bản ghi cần xóa.";
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _examService.DeleteExamRecordAsync(SelectedRecord.Id);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadRecordsAsync();
                    SelectedRecord = null;
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
    }
}
