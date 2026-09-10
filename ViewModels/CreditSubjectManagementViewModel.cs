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
        private readonly ISecurityGateService _securityGate;
        private readonly IAuthService? _authService;

        #region PROPERTIES & COLLECTIONS
        public ObservableCollection<CreditSubject> Subjects { get; } = new();
        public ObservableCollection<CadetAcademicSummaryDto> CadetSummaries { get; } = new();
        public ObservableCollection<Cadet> AllCadets { get; } = new();

        public ObservableCollection<string> UnitOptions { get; } = new();
        public ObservableCollection<string> ClassOptions { get; } = new();
        public ObservableCollection<string> StatusFilterOptions { get; } = new()
        {
            "Tất cả học viên",
            "✅ Đủ tất cả môn",
            "⚠️ Thiếu môn (Dòng vàng)"
        };

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
        private string _selectedStatusFilter = "Tất cả học viên";

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
        private int _missingSubjectsStudentCount;

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
        private double _credits = 1.0;

        [ObservableProperty]
        private string _assessmentType = "Kiểm tra và thi";

        [ObservableProperty]
        private string _subjectDescription = string.Empty;

        /// <summary>
        /// Danh sách các đợt kiểm tra / cột điểm trực thuộc môn học chính
        /// </summary>
        public ObservableCollection<SubjectComponentItemViewModel> FormComponents { get; } = new();
        #endregion

        #region MODAL XEM CHI TIẾT PHÂN RÃ THÀNH PHẦN MÔN HỌC
        [ObservableProperty]
        private bool _isBreakdownModalVisible;

        [ObservableProperty]
        private string _breakdownCadetName = string.Empty;

        [ObservableProperty]
        private string _breakdownCadetInfo = string.Empty;

        public ObservableCollection<MajorSubjectBreakdownDto> CadetBreakdowns { get; } = new();
        #endregion

        #region MA TRẬN NHẬP ĐIỂM THEO MÔN HỌC (GRADE ENTRY MATRIX)
        [ObservableProperty]
        private bool _isGradeMatrixModalVisible;

        [ObservableProperty]
        private string _searchSubjectText = string.Empty;

        public ObservableCollection<CreditSubject> FilteredSubjectsForGrading { get; } = new();

        [ObservableProperty]
        private CreditSubject? _selectedSubjectForGrading;

        [ObservableProperty]
        private string _currentSubjectInfoText = string.Empty;

        public ObservableCollection<SubjectAssessmentComponent> CurrentSubjectComponents { get; } = new();
        public ObservableCollection<CadetSubjectGradeRowDto> SubjectGradeRows { get; } = new();

        // Cấu hình tiêu đề và ẩn hiện của các cột đợt kiểm tra động (hỗ trợ hiển thị linh hoạt 1, 2, 3, 4, 5, 6 cột)
        [ObservableProperty]
        private string _col1Header = "Đợt 1";
        [ObservableProperty]
        private bool _isCol1Visible;

        [ObservableProperty]
        private string _col2Header = "Đợt 2";
        [ObservableProperty]
        private bool _isCol2Visible;

        [ObservableProperty]
        private string _col3Header = "Đợt 3";
        [ObservableProperty]
        private bool _isCol3Visible;

        [ObservableProperty]
        private string _col4Header = "Đợt 4";
        [ObservableProperty]
        private bool _isCol4Visible;

        [ObservableProperty]
        private string _col5Header = "Đợt 5";
        [ObservableProperty]
        private bool _isCol5Visible;

        [ObservableProperty]
        private string _col6Header = "Đợt 6";
        [ObservableProperty]
        private bool _isCol6Visible;

        // Tương thích ngược
        [ObservableProperty]
        private bool _isScoreFormVisible;
        #endregion

        #region MODAL NHẬP ĐIỂM THEO TỪNG HỌC VIÊN (CADET SINGLE GRADE ENTRY)
        [ObservableProperty]
        private bool _isCadetScoreModalVisible;

        [ObservableProperty]
        private CadetAcademicSummaryDto? _cadetForScoreEntry;

        [ObservableProperty]
        private string _cadetScoreModalTitle = string.Empty;

        [ObservableProperty]
        private string _cadetModalSearchSubjectText = string.Empty;

        public ObservableCollection<CreditSubject> FilteredSubjectsForCadet { get; } = new();

        [ObservableProperty]
        private CreditSubject? _selectedSubjectForCadetScore;

        public ObservableCollection<CadetSingleSubjectGradeDto> CadetComponentGradeItems { get; } = new();

        [ObservableProperty]
        private double? _cadetCalculatedSubjectScore;

        [ObservableProperty]
        private string _cadetCalculatedSubjectScoreDisplay = "--";

        [ObservableProperty]
        private string _cadetSubjectStatusIcon = "⚪";

        [ObservableProperty]
        private string _cadetSubjectStatusTooltip = "Chưa có điểm";
        #endregion

        private bool CheckCanBoOrAdminPermission(string actionDescription)
        {
            if (_authService?.CurrentUser?.Role == "HocVien")
            {
                System.Windows.MessageBox.Show(
                    $"CẢNH BÁO AN NINH: Tài khoản Học viên không có quyền {actionDescription}!\nChức năng này chỉ dành riêng cho Cán bộ Quản lý hoặc Quản trị viên.",
                    "Từ Chối Thao Tác (403)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                StatusMessage = $"[TỪ CHỐI 403] Không có quyền {actionDescription}.";
                return false;
            }
            return true;
        }

        public CreditSubjectManagementViewModel(
            ICreditSubjectService creditService,
            ICadetService cadetService,
            ICatalogService catalogService,
            IClassService classService,
            IFileDialogService fileDialogService,
            ISecurityGateService securityGate,
            IAuthService? authService = null)
        {
            _creditService = creditService;
            _cadetService = cadetService;
            _catalogService = catalogService;
            _classService = classService;
            _fileDialogService = fileDialogService;
            _securityGate = securityGate;
            _authService = authService;

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
                var allSummaries = await _creditService.GetCadetAcademicSummariesAsync(
                    SelectedUnit, SelectedClass, SearchKeyword);

                // Áp dụng bộ lọc trạng thái học tập
                var filtered = allSummaries.AsEnumerable();
                if (SelectedStatusFilter == "✅ Đủ tất cả môn")
                {
                    filtered = filtered.Where(s => !s.HasMissingSubjects);
                }
                else if (SelectedStatusFilter == "⚠️ Thiếu môn (Dòng vàng)")
                {
                    filtered = filtered.Where(s => s.HasMissingSubjects);
                }

                CadetSummaries.Clear();
                foreach (var sum in filtered) CadetSummaries.Add(sum);

                TotalStudentsCount = allSummaries.Count;
                MissingSubjectsStudentCount = allSummaries.Count(c => c.HasMissingSubjects);
                AverageOverallGpa = allSummaries.Any(c => c.TotalCreditsEarned > 0)
                    ? Math.Round(allSummaries.Where(c => c.TotalCreditsEarned > 0).Average(c => c.Gpa), 2)
                    : 0;

                ExcellentStudentsCount = allSummaries.Count(c => c.Gpa >= 8.0 && c.TotalCreditsEarned > 0);

                StatusMessage = $"Đã nạp {CadetSummaries.Count}/{TotalStudentsCount} học viên ({MissingSubjectsStudentCount} học viên thiếu môn - dòng vàng), {TotalSubjectsCount} môn tín chỉ.";
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
            SelectedStatusFilter = "Tất cả học viên";
            SearchKeyword = string.Empty;
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task FilterMissingOnlyAsync()
        {
            SelectedStatusFilter = "⚠️ Thiếu môn (Dòng vàng)";
            await LoadDataAsync();
        }

        #region SUBJECT ACTIONS
        [RelayCommand]
        public void AddComponentRow()
        {
            var newItem = new SubjectComponentItemViewModel
            {
                ComponentName = $"Đợt {FormComponents.Count + 1}",
                Credits = 1.0,
                OnCreditsChangedAction = UpdateTotalCreditsFromComponents
            };
            FormComponents.Add(newItem);
            UpdateTotalCreditsFromComponents();
        }

        [RelayCommand]
        public void RemoveComponentRow(SubjectComponentItemViewModel? item)
        {
            if (item == null) return;
            if (FormComponents.Count <= 1)
            {
                StatusMessage = "Môn học phải có ít nhất 1 đợt kiểm tra / cột điểm.";
                return;
            }
            FormComponents.Remove(item);
            UpdateTotalCreditsFromComponents();
        }

        private void UpdateTotalCreditsFromComponents()
        {
            Credits = FormComponents.Count > 0 ? Math.Round(FormComponents.Sum(c => c.Credits), 2) : 1.0;
        }

        [RelayCommand]
        public async Task OpenAddSubjectFormAsync()
        {
            if (!CheckCanBoOrAdminPermission("thêm môn học tín chỉ")) return;
            if (!await _securityGate.EnsureUnlockedAsync("Thêm môn học tín chỉ mới")) return;

            IsEditingSubject = false;
            EditingSubjectId = 0;
            SubjectCode = $"TC{DateTime.Now:yyMM}{Subjects.Count + 1:D2}";
            SubjectName = string.Empty;
            AssessmentType = "Kiểm tra và thi";
            SubjectDescription = string.Empty;

            FormComponents.Clear();
            FormComponents.Add(new SubjectComponentItemViewModel
            {
                ComponentName = "Kiểm tra thường xuyên",
                Credits = 1.0,
                OnCreditsChangedAction = UpdateTotalCreditsFromComponents
            });
            UpdateTotalCreditsFromComponents();

            IsSubjectFormVisible = true;
        }

        [RelayCommand]
        public async Task OpenEditSubjectFormAsync(CreditSubject? subject)
        {
            if (subject == null) return;
            if (!CheckCanBoOrAdminPermission("chỉnh sửa môn học tín chỉ")) return;
            if (!await _securityGate.EnsureUnlockedAsync($"Chỉnh sửa môn học '{subject.SubjectName}'")) return;

            IsEditingSubject = true;
            EditingSubjectId = subject.Id;
            SubjectCode = subject.SubjectCode;
            SubjectName = subject.SubjectName;
            AssessmentType = subject.AssessmentType;
            SubjectDescription = subject.Description ?? string.Empty;

            FormComponents.Clear();
            var comps = await _creditService.GetComponentsBySubjectIdAsync(subject.Id);
            if (comps.Any())
            {
                foreach (var c in comps)
                {
                    FormComponents.Add(new SubjectComponentItemViewModel
                    {
                        Id = c.Id,
                        ComponentName = c.ComponentName,
                        Credits = c.Credits,
                        OnCreditsChangedAction = UpdateTotalCreditsFromComponents
                    });
                }
            }
            else
            {
                FormComponents.Add(new SubjectComponentItemViewModel
                {
                    ComponentName = subject.SubjectName,
                    Credits = subject.Credits > 0 ? subject.Credits : 1.0,
                    OnCreditsChangedAction = UpdateTotalCreditsFromComponents
                });
            }

            UpdateTotalCreditsFromComponents();
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
            if (!CheckCanBoOrAdminPermission("lưu môn học tín chỉ")) return;
            if (!await _securityGate.EnsureUnlockedAsync("Lưu thông tin môn học & đợt kiểm tra"))
                return;

            if (string.IsNullOrWhiteSpace(SubjectCode) || string.IsNullOrWhiteSpace(SubjectName))
            {
                StatusMessage = "Vui lòng nhập đầy đủ mã môn và tên môn học.";
                return;
            }

            if (!FormComponents.Any())
            {
                StatusMessage = "Vui lòng thêm ít nhất 1 đợt kiểm tra cho môn học.";
                return;
            }

            foreach (var c in FormComponents)
            {
                if (string.IsNullOrWhiteSpace(c.ComponentName))
                {
                    StatusMessage = "Tên các đợt kiểm tra không được để trống.";
                    return;
                }
                if (c.Credits <= 0)
                {
                    StatusMessage = "Số tín chỉ của từng đợt kiểm tra phải lớn hơn 0.";
                    return;
                }
            }

            IsBusy = true;
            try
            {
                var subj = new CreditSubject
                {
                    Id = EditingSubjectId,
                    SubjectCode = SubjectCode.Trim(),
                    SubjectName = SubjectName.Trim(),
                    AssessmentType = AssessmentType,
                    Description = SubjectDescription?.Trim() ?? string.Empty,
                    Credits = Math.Round(FormComponents.Sum(c => c.Credits), 2)
                };

                var compEntities = FormComponents.Select((c, idx) => new SubjectAssessmentComponent
                {
                    Id = c.Id,
                    ComponentName = c.ComponentName.Trim(),
                    Credits = c.Credits,
                    OrderIndex = idx + 1
                });

                var res = await _creditService.SaveSubjectWithComponentsAsync(subj, compEntities);
                StatusMessage = res.Message;

                if (res.Success)
                {
                    IsSubjectFormVisible = false;
                    await LoadDataAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
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
            if (!CheckCanBoOrAdminPermission("xóa môn học tín chỉ")) return;
            if (!await _securityGate.EnsureUnlockedAsync($"Xóa môn học '{subject.SubjectName}'"))
                return;

            var confirm = System.Windows.MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa môn học tín chỉ '{subject.SubjectName}' ({subject.SubjectCode}) không?\n\nLưu ý: Tất cả các đợt kiểm tra và điểm số liên quan sẽ bị xóa.",
                "Xác nhận xóa môn học tín chỉ",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

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

        #region MODAL NHẬP ĐIỂM THEO TỪNG HỌC VIÊN (CADET GRADE ACTIONS)
        [RelayCommand]
        public async Task OpenAddScoreFormAsync(CadetAcademicSummaryDto? summary)
        {
            if (!await _securityGate.EnsureUnlockedAsync("Nhập điểm môn học tín chỉ")) return;

            if (summary != null)
            {
                await OpenCadetScoreModalAsync(summary);
            }
            else
            {
                await OpenGradeMatrixModalAsync(null);
            }
        }

        [RelayCommand]
        public async Task OpenCadetScoreModalAsync(CadetAcademicSummaryDto? cadet)
        {
            if (cadet == null) return;
            if (!await _securityGate.EnsureUnlockedAsync($"Nhập điểm cho học viên '{cadet.FullName}'")) return;

            CadetForScoreEntry = cadet;
            CadetScoreModalTitle = $"NHẬP ĐIỂM HỌC VIÊN: {cadet.FullName} - {cadet.CadetCode} ({cadet.ClassName} - {cadet.Unit})";
            CadetModalSearchSubjectText = string.Empty;

            FilteredSubjectsForCadet.Clear();
            foreach (var s in Subjects)
            {
                FilteredSubjectsForCadet.Add(s);
            }

            SelectedSubjectForCadetScore = Subjects.FirstOrDefault();
            IsCadetScoreModalVisible = true;

            await LoadCadetSubjectGradesAsync();
        }

        [RelayCommand]
        public void CloseCadetScoreModal()
        {
            IsCadetScoreModalVisible = false;
            CadetForScoreEntry = null;
        }

        partial void OnCadetModalSearchSubjectTextChanged(string value)
        {
            FilteredSubjectsForCadet.Clear();
            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var s in Subjects) FilteredSubjectsForCadet.Add(s);
            }
            else
            {
                var lower = value.Trim().ToLower();
                var matched = Subjects.Where(s => s.SubjectCode.ToLower().Contains(lower) || 
                                                  s.SubjectName.ToLower().Contains(lower)).ToList();
                foreach (var m in matched) FilteredSubjectsForCadet.Add(m);
            }
        }

        partial void OnSelectedSubjectForCadetScoreChanged(CreditSubject? value)
        {
            if (value != null && IsCadetScoreModalVisible)
            {
                _ = LoadCadetSubjectGradesAsync();
            }
        }

        private async Task LoadCadetSubjectGradesAsync()
        {
            if (CadetForScoreEntry == null || SelectedSubjectForCadetScore == null) return;

            IsBusy = true;
            try
            {
                var (subject, components, calcScore, hasMissing) = await _creditService.GetCadetSubjectGradesAsync(
                    CadetForScoreEntry.CadetId, SelectedSubjectForCadetScore.Id);

                CadetComponentGradeItems.Clear();
                foreach (var comp in components)
                {
                    comp.OnScoreChangedAction = UpdateCadetCalculatedScore;
                    CadetComponentGradeItems.Add(comp);
                }

                CadetCalculatedSubjectScore = calcScore;
                CadetCalculatedSubjectScoreDisplay = calcScore.HasValue ? calcScore.Value.ToString("F2") : "--";
                CadetSubjectStatusIcon = hasMissing ? "⚠️" : (calcScore.HasValue ? "✅" : "⚪");
                CadetSubjectStatusTooltip = hasMissing ? "Chưa thi (đợt thi đã có trên 20 học viên có điểm)" : (calcScore.HasValue ? "Đã hoàn thành đầy đủ các đợt thi" : "Chưa hoàn thành đủ các đợt thi");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateCadetCalculatedScore()
        {
            if (SelectedSubjectForCadetScore == null) return;

            double totalCredits = SelectedSubjectForCadetScore.Credits > 0 ? SelectedSubjectForCadetScore.Credits : 1.0;
            double sumContribution = 0;
            bool allComponentsRecorded = CadetComponentGradeItems.Count > 0;

            foreach (var c in CadetComponentGradeItems)
            {
                if (c.Score.HasValue && c.Score.Value >= 0)
                {
                    sumContribution += c.Score.Value * c.Credits;
                }
                else
                {
                    allComponentsRecorded = false;
                }
            }

            // Điểm trung bình môn chỉ có khi TẤT CẢ các cột của môn chính được nhập
            if (allComponentsRecorded)
            {
                CadetCalculatedSubjectScore = Math.Round(sumContribution / totalCredits, 2);
                CadetCalculatedSubjectScoreDisplay = CadetCalculatedSubjectScore.Value.ToString("F2");
            }
            else
            {
                CadetCalculatedSubjectScore = null;
                CadetCalculatedSubjectScoreDisplay = "--";
            }

            bool hasMissing = CadetComponentGradeItems.Any(c => c.HasMissingWarning && (!c.Score.HasValue || c.Score.Value < 0));
            CadetSubjectStatusIcon = hasMissing ? "⚠️" : (CadetCalculatedSubjectScore.HasValue ? "✅" : "⚪");
            CadetSubjectStatusTooltip = hasMissing ? "Chưa thi (đợt thi đã có trên 20 học viên có điểm)" : (CadetCalculatedSubjectScore.HasValue ? "Đã hoàn thành đầy đủ các đợt thi" : "Chưa hoàn thành đủ các đợt thi");
        }

        [RelayCommand]
        public async Task SaveCadetScoreModalAsync()
        {
            if (CadetForScoreEntry == null || SelectedSubjectForCadetScore == null) return;
            if (!CheckCanBoOrAdminPermission("nhập hoặc sửa điểm học viên")) return;
            if (!await _securityGate.EnsureUnlockedAsync($"Lưu điểm học viên '{CadetForScoreEntry.FullName}' môn '{SelectedSubjectForCadetScore.SubjectName}'"))
                return;

            IsBusy = true;
            try
            {
                var componentScores = CadetComponentGradeItems
                    .Select(c => (componentId: c.ComponentId, score: c.Score))
                    .ToList();

                var res = await _creditService.SaveCadetSubjectGradesAsync(
                    CadetForScoreEntry.CadetId, SelectedSubjectForCadetScore.Id, componentScores);

                StatusMessage = res.Message;
                if (res.Success)
                {
                    System.Windows.MessageBox.Show(res.Message, "Lưu Điểm Thành Công",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    await LoadDataAsync();
                    await LoadCadetSubjectGradesAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "Lỗi Lưu Điểm",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
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

        #region MA TRẬN NHẬP ĐIỂM THEO MÔN HỌC (GRADE ENTRY MATRIX ACTIONS)

        [RelayCommand]
        public async Task OpenGradeMatrixModalAsync(CreditSubject? subject = null)
        {
            if (!CheckCanBoOrAdminPermission("mở bảng nhập điểm ma trận")) return;
            if (!await _securityGate.EnsureUnlockedAsync(subject != null ? $"Nhập điểm ma trận môn '{subject.SubjectName}'" : "Nhập điểm ma trận theo môn học")) return;

            SearchSubjectText = string.Empty;
            FilteredSubjectsForGrading.Clear();
            foreach (var s in Subjects) FilteredSubjectsForGrading.Add(s);

            SelectedSubjectForGrading = subject ?? Subjects.FirstOrDefault();
            IsGradeMatrixModalVisible = true;
            await LoadGradeMatrixForSelectedSubjectAsync();
        }

        [RelayCommand]
        public void CloseGradeMatrixModal()
        {
            IsGradeMatrixModalVisible = false;
        }

        partial void OnSearchSubjectTextChanged(string value)
        {
            FilteredSubjectsForGrading.Clear();
            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var s in Subjects) FilteredSubjectsForGrading.Add(s);
            }
            else
            {
                var lower = value.Trim().ToLower();
                var matched = Subjects.Where(s => s.SubjectCode.ToLower().Contains(lower) || 
                                                  s.SubjectName.ToLower().Contains(lower)).ToList();
                foreach (var m in matched) FilteredSubjectsForGrading.Add(m);
            }
        }

        partial void OnSelectedSubjectForGradingChanged(CreditSubject? value)
        {
            if (value != null && IsGradeMatrixModalVisible)
            {
                _ = LoadGradeMatrixForSelectedSubjectAsync();
            }
        }

        public async Task LoadGradeMatrixForSelectedSubjectAsync()
        {
            if (SelectedSubjectForGrading == null)
            {
                CurrentSubjectInfoText = "Chưa chọn môn học.";
                CurrentSubjectComponents.Clear();
                SubjectGradeRows.Clear();
                return;
            }

            IsBusy = true;
            try
            {
                var (subj, components, rows) = await _creditService.GetSubjectGradeMatrixAsync(
                    SelectedSubjectForGrading.Id, SelectedUnit, SelectedClass);

                CurrentSubjectComponents.Clear();
                foreach (var c in components) CurrentSubjectComponents.Add(c);

                CurrentSubjectInfoText = $"Mã môn: {SelectedSubjectForGrading.SubjectCode}  |  Tổng số tín chỉ: {SelectedSubjectForGrading.CalculatedTotalCredits:F2} TC  |  Số đợt kiểm tra: {components.Count} đợt";

                // Cấu hình hiển thị động các cột đợt kiểm tra
                IsCol1Visible = components.Count >= 1;
                Col1Header = components.Count >= 1 ? components[0].DisplayHeader : "Đợt 1";

                IsCol2Visible = components.Count >= 2;
                Col2Header = components.Count >= 2 ? components[1].DisplayHeader : "Đợt 2";

                IsCol3Visible = components.Count >= 3;
                Col3Header = components.Count >= 3 ? components[2].DisplayHeader : "Đợt 3";

                IsCol4Visible = components.Count >= 4;
                Col4Header = components.Count >= 4 ? components[3].DisplayHeader : "Đợt 4";

                IsCol5Visible = components.Count >= 5;
                Col5Header = components.Count >= 5 ? components[4].DisplayHeader : "Đợt 5";

                IsCol6Visible = components.Count >= 6;
                Col6Header = components.Count >= 6 ? components[5].DisplayHeader : "Đợt 6";

                SubjectGradeRows.Clear();
                foreach (var r in rows) SubjectGradeRows.Add(r);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi nạp bảng điểm môn: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SaveGradeMatrixAsync()
        {
            if (SelectedSubjectForGrading == null)
            {
                StatusMessage = "Vui lòng chọn môn học để lưu điểm.";
                return;
            }

            if (!CheckCanBoOrAdminPermission("lưu bảng điểm môn học")) return;
            if (!await _securityGate.EnsureUnlockedAsync($"Lưu bảng điểm môn '{SelectedSubjectForGrading.SubjectName}'"))
                return;

            IsBusy = true;
            try
            {
                var res = await _creditService.SaveSubjectGradeMatrixAsync(
                    SelectedSubjectForGrading.Id, SubjectGradeRows.ToList());
                
                StatusMessage = res.Message;
                if (res.Success)
                {
                    System.Windows.MessageBox.Show(res.Message, "Lưu Điểm Thành Công",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    await LoadDataAsync();
                    await LoadGradeMatrixForSelectedSubjectAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "Lỗi Lưu Điểm",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi lưu bảng điểm: {ex.Message}";
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
            if (!await _securityGate.EnsureUnlockedAsync("Xuất Báo Cáo Bảng Điểm Tín Chỉ ra Excel"))
                return;

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

        #region BREAKDOWN MODAL ACTIONS
        [RelayCommand]
        public async Task OpenBreakdownModalAsync(CadetAcademicSummaryDto? summary)
        {
            if (summary == null) return;
            BreakdownCadetName = summary.FullName;
            BreakdownCadetInfo = $"Mã HV: {summary.CadetCode}  |  Đơn vị: {summary.Unit}  |  Lớp: {summary.ClassName}  |  TBM toàn khóa: {summary.Gpa:F2}";
            
            CadetBreakdowns.Clear();
            var breakdowns = await _creditService.GetSubjectBreakdownForCadetAsync(summary.CadetId);
            foreach (var b in breakdowns) CadetBreakdowns.Add(b);

            IsBreakdownModalVisible = true;
        }

        [RelayCommand]
        public void CloseBreakdownModal()
        {
            IsBreakdownModalVisible = false;
        }
        #endregion

        #region IMPORT EXCEL
        [RelayCommand]
        public async Task ImportStandardExcelAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Nhập điểm môn học từ file Excel"))
                return;

            string? filePath = _fileDialogService.ShowOpenFileDialog("Tập tin Excel (*.xlsx)|*.xlsx|Tất cả tập tin (*.*)|*.*");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var confirm = System.Windows.MessageBox.Show(
                $"Hệ thống sẽ nạp/cập nhật điểm môn học và BẢO LƯU NGUYÊN VẸN mã số học viên (ID) hiện có từ file:\n{filePath}\n\nĐồng chí có chắc chắn muốn thực hiện?",
                "Xác Nhận Nhập Điểm Từ Excel",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusMessage = "Đang nạp dữ liệu điểm và bảo lưu mã học viên từ Excel...";
            try
            {
                var res = await _creditService.ImportStandardTbmExcelAsync(filePath);
                StatusMessage = res.Message;

                if (res.Success)
                {
                    System.Windows.MessageBox.Show(res.Message, "Nhập Điểm Thành Công", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    await InitializeAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "Lỗi Nhập Excel", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi nhập Excel: {ex.Message}";
                System.Windows.MessageBox.Show($"Lỗi nhập Excel: {ex.Message}", "Lỗi", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion
    }
}
