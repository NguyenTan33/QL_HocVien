using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Models.Filters;
using QL_HocVien.Services;
using QL_HocVien.Services.Interfaces;

namespace QL_HocVien.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IDashboardAnalyticsService _analyticsService;
        private readonly ITrainingRecommendationService? _recommendationService;
        private readonly ITrainingEventService _eventService;
        private readonly ICadetService _cadetService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;
        private readonly ISecurityGateService _securityGate;

        #region BỘ LỌC NÂNG CAO HỌC VỤ (ACADEMIC FILTER PROPERTIES)
        public ObservableCollection<string> UnitOptions { get; } = new();
        public ObservableCollection<string> ClassOptions { get; } = new();
        public ObservableCollection<string> SessionOptions { get; } = new();
        public ObservableCollection<CreditSubject> CreditSubjectOptions { get; } = new();
        public ObservableCollection<Subject> SubjectOptions { get; } = new();
        public ObservableCollection<string> GradeOptions { get; } = new()
        {
            "Tất cả", "Giỏi", "Khá", "Trung bình", "Yếu"
        };
        public ObservableCollection<string> StatusOptions { get; } = new()
        {
            "Tất cả", "Đủ môn", "Nợ/Thiếu môn"
        };

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private string _selectedClass = "Tất cả";

        [ObservableProperty]
        private string _selectedSession = "Tất cả";

        [ObservableProperty]
        private CreditSubject? _selectedCreditSubject;

        [ObservableProperty]
        private Subject? _selectedSubject;

        [ObservableProperty]
        private string _selectedGrade = "Tất cả";

        [ObservableProperty]
        private string _selectedStatus = "Tất cả";

        [ObservableProperty]
        private DateTime? _fromDate;

        [ObservableProperty]
        private DateTime? _toDate;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private bool _isAdvancedFilterExpanded = true;
        #endregion

        #region THẺ KPI CHỈ HUY HỌC VỤ CHIẾN LƯỢC (ACADEMIC STRATEGY KPIS)
        [ObservableProperty]
        private int _totalCadets;

        [ObservableProperty]
        private int _totalUnitsCount;

        [ObservableProperty]
        private int _totalClassesCount;

        [ObservableProperty]
        private int _totalCreditSubjects;

        [ObservableProperty]
        private int _totalCreditScores;

        [ObservableProperty]
        private int _totalExamRecords;

        [ObservableProperty]
        private int _totalTestedSubjects;

        [ObservableProperty]
        private int _uniqueTestedCadets;

        [ObservableProperty]
        private double _averageGpa;

        [ObservableProperty]
        private double _graduationReadinessRate;

        [ObservableProperty]
        private double _passRate;

        [ObservableProperty]
        private double _eliteRate;

        [ObservableProperty]
        private int _excellentCount;

        [ObservableProperty]
        private int _goodCount;

        [ObservableProperty]
        private int _fairCount;

        [ObservableProperty]
        private int _passCount;

        [ObservableProperty]
        private int _failCount;

        [ObservableProperty]
        private double _failRate;

        [ObservableProperty]
        private int _warningCount;

        [ObservableProperty]
        private int _completedCadetsCount;

        [ObservableProperty]
        private string _overallRatingLabel = "Đang tải dữ liệu...";

        [ObservableProperty]
        private string _overallRatingColor = "#1E3A8A";

        [ObservableProperty]
        private string _upcomingEventTitle = "Chưa có lịch thi gần nhất";

        [ObservableProperty]
        private string _upcomingEventTime = "";

        [ObservableProperty]
        private bool _hasUpcomingEvent;
        #endregion

        #region BIỂU ĐỒ & DỮ LIỆU PHÂN TÍCH (DATA COLLECTIONS)
        public ObservableCollection<UnitLeaderboardDto> UnitLeaderboard { get; } = new();
        public ObservableCollection<AcademicClassComparisonDto> ClassLeaderboard { get; } = new();
        public ObservableCollection<SubjectPerformanceDto> SubjectPerformances { get; } = new();
        public ObservableCollection<TrainingEvent> MonthlyFocusEvents { get; } = new();
        public ObservableCollection<CadetHonorDto> HonoredCadets { get; } = new();
        public ObservableCollection<AcademicWarningCadetDto> AcademicWarnings { get; } = new();
        public ObservableCollection<UntestedCadetDto> UntestedCadets { get; } = new();
        public ObservableCollection<AcademicCadetAnalyticsDto> CumulativeCadets { get; } = new();
        public ObservableCollection<PhysicalExamRecord> FailedRecords { get; } = new();

        [ObservableProperty]
        private int _selectedTabIndex = 0; // 0: Bảng Vàng Danh Dự, 1: Cảnh Báo Học Vụ, 2: Hiệu Suất Học Phần, 3: Thi Đua Đơn Vị

        [ObservableProperty]
        private bool _isCombatMode = true;
        #endregion

        public DashboardViewModel(
            IDashboardAnalyticsService analyticsService,
            ITrainingRecommendationService? recommendationService,
            ITrainingEventService eventService,
            ICadetService cadetService,
            IExcelService excelService,
            IFileDialogService fileDialogService,
            ISecurityGateService securityGate,
            IThemeService? themeService = null)
        {
            _analyticsService = analyticsService;
            _recommendationService = recommendationService;
            _eventService = eventService;
            _cadetService = cadetService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            _securityGate = securityGate;

            if (themeService != null)
            {
                _isCombatMode = themeService.IsCombatMode;
                themeService.ThemeChanged += mode => IsCombatMode = mode;
            }
            else
            {
                _isCombatMode = ThemeService.CurrentIsCombatMode;
            }

            Title = "Trung Tâm Quản Trị & Phân Tích Học Vụ Đào Tạo";

            _ = InitializeDashboardAsync();
        }

        public async Task InitializeDashboardAsync()
        {
            IsBusy = true;
            try
            {
                var units = await _analyticsService.GetAvailableUnitsAsync();
                UnitOptions.Clear();
                foreach (var u in units) UnitOptions.Add(u);

                var classes = await _analyticsService.GetAvailableClassesAsync();
                ClassOptions.Clear();
                foreach (var c in classes) ClassOptions.Add(c);

                var sessions = await _analyticsService.GetAvailableSessionsAsync();
                SessionOptions.Clear();
                foreach (var s in sessions) SessionOptions.Add(s);

                var creditSubjects = await _analyticsService.GetAvailableCreditSubjectsAsync();
                CreditSubjectOptions.Clear();
                foreach (var cs in creditSubjects) CreditSubjectOptions.Add(cs);
                SelectedCreditSubject = CreditSubjectOptions.FirstOrDefault();

                var subjects = await _analyticsService.GetAvailableSubjectsAsync();
                SubjectOptions.Clear();
                foreach (var sub in subjects) SubjectOptions.Add(sub);
                SelectedSubject = SubjectOptions.FirstOrDefault();

                await LoadUpcomingEventAsync();
                await LoadDashboardDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khởi tạo Dashboard: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadUpcomingEventAsync()
        {
            try
            {
                var events = await _eventService.GetAllEventsAsync();
                var nextEvent = events
                    .Where(e => e.StartDate >= DateTime.Today && e.Status != "Completed")
                    .OrderBy(e => e.StartDate)
                    .FirstOrDefault();

                if (nextEvent != null)
                {
                    HasUpcomingEvent = true;
                    UpcomingEventTitle = nextEvent.Title;
                    UpcomingEventTime = $"{nextEvent.StartDate:dd/MM} - {nextEvent.Location}";
                }
                else
                {
                    HasUpcomingEvent = false;
                    UpcomingEventTitle = "Không có lịch thi sắp tới";
                    UpcomingEventTime = "Duy trì kế hoạch giảng dạy thường xuyên";
                }
            }
            catch
            {
                HasUpcomingEvent = false;
            }
        }

        [RelayCommand]
        public async Task LoadDashboardDataAsync()
        {
            IsBusy = true;
            try
            {
                var criteria = new DashboardFilterCriteria
                {
                    Unit = SelectedUnit,
                    ClassName = SelectedClass,
                    ExamSession = SelectedSession,
                    SubjectId = SelectedCreditSubject?.Id ?? SelectedSubject?.Id,
                    Grade = SelectedGrade,
                    AcademicRating = SelectedGrade,
                    Status = SelectedStatus,
                    FromDate = FromDate,
                    ToDate = ToDate,
                    SearchKeyword = SearchKeyword
                };

                // 1. Tải Summary & KPI Học Vụ
                var summary = await _analyticsService.GetSummaryAsync(criteria);
                TotalCadets = summary.TotalCadets;
                TotalUnitsCount = summary.TotalUnitsCount;
                TotalClassesCount = summary.TotalClassesCount;
                TotalCreditSubjects = summary.TotalCreditSubjects;
                TotalCreditScores = summary.TotalCreditScores;
                TotalTestedSubjects = summary.TotalTestedSubjects;
                TotalExamRecords = summary.TotalExamRecords;
                UniqueTestedCadets = summary.UniqueTestedCadets;
                AverageGpa = summary.AverageGpa;
                GraduationReadinessRate = summary.GraduationReadinessRate;
                PassRate = summary.GraduationReadinessRate > 0 ? summary.GraduationReadinessRate : summary.PassRate;
                EliteRate = summary.EliteRate;
                ExcellentCount = summary.ExcellentCount;
                GoodCount = summary.GoodCount;
                FairCount = summary.FairCount;
                PassCount = summary.PassCount;
                FailCount = summary.FailCount;
                FailRate = summary.FailRate;
                WarningCount = summary.WarningCount;
                CompletedCadetsCount = summary.CompletedCadetsCount;
                OverallRatingLabel = summary.OverallRatingLabel;
                OverallRatingColor = summary.OverallRatingColor;

                // 2. Tải Xếp hạng thi đua đơn vị & lớp
                var units = await _analyticsService.GetUnitLeaderboardAsync(criteria);
                UnitLeaderboard.Clear();
                foreach (var u in units) UnitLeaderboard.Add(u);

                var classes = await _analyticsService.GetClassLeaderboardAsync(criteria);
                ClassLeaderboard.Clear();
                foreach (var cl in classes) ClassLeaderboard.Add(cl);

                // 3. Tải Sự kiện trọng tâm trong tháng
                var monthlyEvents = await _analyticsService.GetMonthlyFocusEventsAsync();
                MonthlyFocusEvents.Clear();
                foreach (var ev in monthlyEvents) MonthlyFocusEvents.Add(ev);

                // 4. Tải Phân tích môn học tín chỉ
                var subPerfs = await _analyticsService.GetSubjectPerformancesAsync(criteria);
                SubjectPerformances.Clear();
                foreach (var s in subPerfs) SubjectPerformances.Add(s);

                // 5. Tải Bảng vàng vinh danh học viên xuất sắc (Top GPA)
                var honors = await _analyticsService.GetHonoredCadetsAsync(criteria, 15);
                HonoredCadets.Clear();
                foreach (var h in honors) HonoredCadets.Add(h);

                // 6. Tải Danh sách Cảnh báo học vụ & Nợ môn
                var warnings = await _analyticsService.GetAcademicWarningCadetsAsync(criteria);
                AcademicWarnings.Clear();
                foreach (var w in warnings) AcademicWarnings.Add(w);

                // 7. Tải Học viên chưa thi / thiếu tín chỉ
                var untested = await _analyticsService.GetUntestedCadetsAsync(criteria);
                UntestedCadets.Clear();
                foreach (var uc in untested) UntestedCadets.Add(uc);

                // 8. Tải Dữ liệu tích lũy toàn diện
                var cumulative = await _analyticsService.GetCadetCumulativeAnalyticsAsync(criteria);
                CumulativeCadets.Clear();
                foreach (var c in cumulative) CumulativeCadets.Add(c);

                StatusMessage = $"Cập nhật thành công số liệu học vụ: {TotalCadets} học viên, Điểm TB GPA {AverageGpa:F2}/10, Tỷ lệ đạt chuẩn {GraduationReadinessRate:F1}%.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu phân tích: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ApplyFilterAsync()
        {
            await LoadDashboardDataAsync();
        }

        [RelayCommand]
        public async Task ResetFilterAsync()
        {
            SelectedUnit = "Tất cả";
            SelectedClass = "Tất cả";
            SelectedSession = "Tất cả";
            SelectedCreditSubject = CreditSubjectOptions.FirstOrDefault();
            SelectedSubject = SubjectOptions.FirstOrDefault();
            SelectedGrade = "Tất cả";
            SelectedStatus = "Tất cả";
            FromDate = null;
            ToDate = null;
            SearchKeyword = string.Empty;

            await LoadDashboardDataAsync();
        }

        [RelayCommand]
        public void ToggleFilterExpansion()
        {
            IsAdvancedFilterExpanded = !IsAdvancedFilterExpanded;
        }

        [RelayCommand]
        public async Task ExportExecutiveReportAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Xuất Báo Cáo Tổng Quan Học Vụ & Đào Tạo Tín Chỉ ra Excel (6 Sheets)")) return;

            var fileName = $"BaoCao_TongQuan_HocVu_TinChi_QLHV_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            var filePath = _fileDialogService.ShowSaveFileDialog(
                fileName, 
                "Excel Files (*.xlsx)|*.xlsx", 
                "Xuất Báo Cáo Học Vụ & Đào Tạo Tín Chỉ Đa Sheet ra Excel");

            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var summary = new DashboardSummaryDto
                {
                    TotalCadets = TotalCadets,
                    TotalUnitsCount = TotalUnitsCount,
                    TotalClassesCount = TotalClassesCount,
                    TotalCreditSubjects = TotalCreditSubjects,
                    TotalCreditScores = TotalCreditScores,
                    AverageGpa = AverageGpa,
                    ExcellentCount = ExcellentCount,
                    GoodCount = GoodCount,
                    FairCount = FairCount,
                    PassCount = PassCount,
                    FailCount = FailCount,
                    WarningCount = WarningCount,
                    CompletedCadetsCount = CompletedCadetsCount,
                    TotalExamRecords = TotalExamRecords
                };

                var result = await _excelService.ExportAcademicDashboardMultiSheetReportAsync(
                    filePath,
                    summary,
                    UnitLeaderboard.ToList(),
                    ClassLeaderboard.ToList(),
                    SubjectPerformances.ToList(),
                    AcademicWarnings.ToList(),
                    HonoredCadets.ToList(),
                    CumulativeCadets.ToList());

                StatusMessage = result.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi xuất báo cáo: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExportAllDataAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Sao lưu toàn bộ cơ sở dữ liệu ra Excel")) return;

            var fileName = $"BaoCao_TongHop_QLHV_{DateTime.Today:yyyyMMdd}.xlsx";
            var filePath = _fileDialogService.ShowSaveFileDialog(fileName, "Excel Files (*.xlsx)|*.xlsx", "Xuất toàn bộ cơ sở dữ liệu ra Excel");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ExportAllDataToExcelAsync(filePath);
                StatusMessage = result.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi xuất toàn bộ dữ liệu: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ImportAllDataAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Nhập và khôi phục toàn bộ dữ liệu từ Excel")) return;

            var filePath = _fileDialogService.ShowOpenFileDialog("Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "Chọn tệp Excel để nhập/khôi phục toàn bộ dữ liệu");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ImportAllDataFromExcelAsync(filePath);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    System.Windows.MessageBox.Show(
                        result.Message,
                        "Khôi Phục Dữ Liệu Thành Công",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                    await InitializeDashboardAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        result.Message,
                        "Khôi Phục Dữ Liệu Thất Bại",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi nhập dữ liệu: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
