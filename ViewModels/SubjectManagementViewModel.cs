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

        // Lọc nâng cao: Lọc theo Tên và Lọc theo ID / Mã môn
        [ObservableProperty]
        private string _filterSubjectCode = string.Empty;

        [ObservableProperty]
        private string _filterSubjectName = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "Tất cả";

        [ObservableProperty]
        private Subject? _selectedSubject;

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

        public SubjectManagementViewModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
            Title = "Quản Lý Môn Học & Tiêu Chuẩn Thể Lực";

            _ = LoadSubjectsAsync();
        }

        [RelayCommand]
        public async Task LoadSubjectsAsync()
        {
            IsBusy = true;
            try
            {
                var all = await _subjectService.GetAllSubjectsAsync();
                var filtered = all.AsEnumerable();

                // Lọc nâng cao theo Mã/ID môn
                if (!string.IsNullOrWhiteSpace(FilterSubjectCode))
                {
                    var code = FilterSubjectCode.Trim().ToLower();
                    filtered = filtered.Where(s => s.SubjectCode.ToLower().Contains(code) || s.Id.ToString() == code);
                }

                // Lọc nâng cao theo Tên môn
                if (!string.IsNullOrWhiteSpace(FilterSubjectName))
                {
                    var name = FilterSubjectName.Trim().ToLower();
                    filtered = filtered.Where(s => s.SubjectName.ToLower().Contains(name));
                }

                // Lọc theo thể loại
                if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "Tất cả")
                {
                    filtered = filtered.Where(s => s.Category == SelectedCategory);
                }

                Subjects.Clear();
                foreach (var item in filtered)
                {
                    Subjects.Add(item);
                }
                StatusMessage = $"Hiển thị {Subjects.Count} môn học.";
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
            if (SelectedSubject == null)
            {
                StatusMessage = "Vui lòng chọn môn học cần xóa.";
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _subjectService.DeleteSubjectAsync(SelectedSubject.Id);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await LoadSubjectsAsync();
                    SelectedSubject = null;
                }
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
        private void ClearFilters()
        {
            FilterSubjectCode = string.Empty;
            FilterSubjectName = string.Empty;
            SelectedCategory = "Tất cả";
            _ = LoadSubjectsAsync();
        }

        partial void OnFilterSubjectCodeChanged(string value) => _ = LoadSubjectsAsync();
        partial void OnFilterSubjectNameChanged(string value) => _ = LoadSubjectsAsync();
        partial void OnSelectedCategoryChanged(string value) => _ = LoadSubjectsAsync();
    }
}
