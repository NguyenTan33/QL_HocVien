using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models.Entity;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class CohortManagementViewModel : ViewModelBase
    {
        private readonly ICohortService _cohortService;
        private readonly ISecurityGateService _securityGate;

        public ObservableCollection<AcademicCohort> Cohorts { get; } = new();

        // Tìm kiếm
        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private AcademicCohort? _selectedCohort;

        [ObservableProperty]
        private bool _isAllSelected;

        [ObservableProperty]
        private int _selectedCount;

        private bool _isUpdatingSelection;

        public string SelectAllButtonText => IsAllSelected ? "⬜ Bỏ chọn" : "☑️ Chọn tất cả";

        // ===== FORM THÊM / SỬA =====
        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _formCohortCode = string.Empty;

        [ObservableProperty]
        private string _formCohortName = string.Empty;

        [ObservableProperty]
        private int _formCohortNumber;

        [ObservableProperty]
        private int? _formEnrollmentYear;

        [ObservableProperty]
        private int? _formGraduationYear;

        [ObservableProperty]
        private string _formAcademicYear = string.Empty;

        [ObservableProperty]
        private string _formDescription = string.Empty;

        [ObservableProperty]
        private string _formErrorMessage = string.Empty;

        // ===== XEM DANH SÁCH HỌC VIÊN THUỘC KHÓA =====
        [ObservableProperty]
        private bool _isCadetListVisible;

        [ObservableProperty]
        private AcademicCohort? _viewingCohort;

        public ObservableCollection<Cadet> CohortCadets { get; } = new();

        // ===== ĐỒNG BỘ =====
        [ObservableProperty]
        private string _syncStatusMessage = string.Empty;

        public CohortManagementViewModel(
            ICohortService cohortService,
            ISecurityGateService securityGate)
        {
            _cohortService = cohortService;
            _securityGate = securityGate;
            Title = "Quản Lý Khóa Học";
            _ = LoadCohortsAsync();
        }

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
                foreach (var c in Cohorts) c.IsSelected = value;
                SelectedCount = value ? Cohorts.Count : 0;
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        [RelayCommand]
        public void UpdateSelectionCount()
        {
            if (_isUpdatingSelection) return;
            _isUpdatingSelection = true;
            try
            {
                int count = Cohorts.Count(c => c.IsSelected);
                SelectedCount = count;
                IsAllSelected = Cohorts.Count > 0 && count == Cohorts.Count;
            }
            finally
            {
                _isUpdatingSelection = false;
                OnPropertyChanged(nameof(SelectAllButtonText));
            }
        }

        [RelayCommand]
        public async Task LoadCohortsAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _cohortService.GetAllCohortsAsync();

                // Lọc theo từ khóa tìm kiếm
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    var kw = SearchKeyword.Trim().ToLower();
                    list = list.Where(c =>
                        c.CohortCode.ToLower().Contains(kw) ||
                        c.CohortName.ToLower().Contains(kw) ||
                        c.AcademicYear.ToLower().Contains(kw) ||
                        c.Description.ToLower().Contains(kw));
                }

                Cohorts.Clear();
                foreach (var c in list)
                {
                    c.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(AcademicCohort.IsSelected))
                            UpdateSelectionCount();
                    };
                    Cohorts.Add(c);
                }
                UpdateSelectionCount();
                StatusMessage = $"Đang hiển thị {Cohorts.Count} khóa học.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu khóa học: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task OpenAddFormAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Thêm khóa học mới")) return;

            IsEditing = false;
            FormCohortCode = string.Empty;
            FormCohortName = string.Empty;
            FormCohortNumber = 0;
            FormEnrollmentYear = DateTime.Now.Year;
            FormGraduationYear = DateTime.Now.Year + 4;
            FormAcademicYear = $"{DateTime.Now.Year} - {DateTime.Now.Year + 4}";
            FormDescription = string.Empty;
            FormErrorMessage = string.Empty;
            IsFormVisible = true;
        }

        [RelayCommand]
        private async Task OpenEditFormAsync(AcademicCohort? target = null)
        {
            var cohort = target ?? SelectedCohort;
            if (cohort == null)
            {
                StatusMessage = "Vui lòng chọn khóa học cần chỉnh sửa.";
                return;
            }

            if (!await _securityGate.EnsureUnlockedAsync($"Chỉnh sửa khóa học '{cohort.CohortName}'")) return;

            SelectedCohort = cohort;
            IsEditing = true;
            FormCohortCode = cohort.CohortCode;
            FormCohortName = cohort.CohortName;
            FormCohortNumber = cohort.CohortNumber;
            FormEnrollmentYear = cohort.EnrollmentYear;
            FormGraduationYear = cohort.GraduationYear;
            FormAcademicYear = cohort.AcademicYear;
            FormDescription = cohort.Description;
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
            if (!await _securityGate.EnsureUnlockedAsync("Lưu thông tin khóa học")) return;

            FormErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormCohortCode))
            {
                FormErrorMessage = "Vui lòng nhập Mã khóa học (ví dụ: K75, K26).";
                return;
            }
            if (string.IsNullOrWhiteSpace(FormCohortName))
            {
                FormErrorMessage = "Vui lòng nhập Tên khóa học.";
                return;
            }

            IsBusy = true;
            try
            {
                // Tự động tính CohortNumber từ mã nếu chưa nhập
                int cohortNum = FormCohortNumber;
                if (cohortNum == 0)
                {
                    var nm = System.Text.RegularExpressions.Regex.Match(FormCohortCode, @"\d+");
                    if (nm.Success) int.TryParse(nm.Value, out cohortNum);
                }

                if (IsEditing && SelectedCohort != null)
                {
                    SelectedCohort.CohortCode = FormCohortCode.Trim().ToUpper();
                    SelectedCohort.CohortName = FormCohortName.Trim();
                    SelectedCohort.CohortNumber = cohortNum;
                    SelectedCohort.EnrollmentYear = FormEnrollmentYear;
                    SelectedCohort.GraduationYear = FormGraduationYear;
                    SelectedCohort.AcademicYear = FormAcademicYear.Trim();
                    SelectedCohort.Description = FormDescription.Trim();

                    var result = await _cohortService.UpdateCohortAsync(SelectedCohort);
                    if (result.Success)
                    {
                        IsFormVisible = false;
                        await LoadCohortsAsync();
                        StatusMessage = result.Message;
                    }
                    else
                    {
                        FormErrorMessage = result.Message;
                    }
                }
                else
                {
                    var newCohort = new AcademicCohort
                    {
                        CohortCode = FormCohortCode.Trim().ToUpper(),
                        CohortName = FormCohortName.Trim(),
                        CohortNumber = cohortNum,
                        EnrollmentYear = FormEnrollmentYear,
                        GraduationYear = FormGraduationYear,
                        AcademicYear = FormAcademicYear.Trim(),
                        Description = FormDescription.Trim()
                    };

                    var result = await _cohortService.AddCohortAsync(newCohort);
                    if (result.Success)
                    {
                        IsFormVisible = false;
                        await LoadCohortsAsync();
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
        private async Task DeleteCohortAsync(AcademicCohort? target = null)
        {
            if (!await _securityGate.EnsureUnlockedAsync("Xóa khóa học")) return;

            if (target != null)
            {
                var confirmSingle = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa khóa học '{target.CohortName}' (Mã: {target.CohortCode})?\n" +
                    $"Quân số hiện tại: {target.CadetCount} học viên.\n" +
                    "Lưu ý: Chỉ có thể xóa nếu không còn học viên thuộc khóa này.",
                    "Xác nhận xóa khóa học",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmSingle != MessageBoxResult.Yes) return;

                IsBusy = true;
                try
                {
                    var result = await _cohortService.DeleteCohortAsync(target.Id);
                    StatusMessage = result.Message;
                    if (result.Success)
                    {
                        SelectedCohort = null;
                        await LoadCohortsAsync();
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi khi xóa: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                }
                return;
            }

            var selected = Cohorts.Where(c => c.IsSelected).ToList();
            if (!selected.Any() && SelectedCohort != null) selected.Add(SelectedCohort);

            if (!selected.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một khóa học để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {selected.Count} khóa học đã chọn?\n\nLưu ý: Chỉ xóa được những khóa học không còn học viên nào.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                int deleted = 0;
                var errors = new System.Text.StringBuilder();
                foreach (var c in selected)
                {
                    var result = await _cohortService.DeleteCohortAsync(c.Id);
                    if (result.Success) deleted++;
                    else errors.AppendLine($"• {c.CohortName}: {result.Message}");
                }
                await LoadCohortsAsync();
                StatusMessage = $"Đã xóa {deleted}/{selected.Count} khóa học.";
                if (errors.Length > 0)
                    MessageBox.Show($"Một số khóa học không thể xóa:\n{errors}", "Kết quả xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        /// <summary>
        /// Đồng bộ CohortId cho toàn bộ học viên dựa vào mã học viên (CadetCode).
        /// Ví dụ: "ĐH.075.299" → tìm khóa K75 → gán CohortId.
        /// </summary>
        [RelayCommand]
        private async Task SyncCohortIdsAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Đồng bộ khóa học học viên")) return;

            IsBusy = true;
            SyncStatusMessage = "Đang đồng bộ...";
            try
            {
                var (synced, skipped) = await _cohortService.SyncAllCadetCohortIdsAsync();
                SyncStatusMessage = $"✅ Đồng bộ thành công {synced} học viên. {skipped} học viên không xác định được khóa.";
                await LoadCohortsAsync();
            }
            catch (Exception ex)
            {
                SyncStatusMessage = $"❌ Lỗi đồng bộ: {ex.Message}";
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
            ViewingCohort = null;
            CohortCadets.Clear();
        }

        partial void OnSearchKeywordChanged(string value) => _ = LoadCohortsAsync();
    }
}
