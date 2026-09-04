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

namespace QL_HocVien.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IDashboardAnalyticsService _analyticsService;
        private readonly ITrainingRecommendationService _recommendationService;
        private readonly ITrainingEventService _eventService;
        private readonly ICadetService _cadetService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;

        #region BỘ LỌC NÂNG CAO (FILTER PROPERTIES)
        public ObservableCollection<string> UnitOptions { get; } = new();
        public ObservableCollection<string> ClassOptions { get; } = new();
        public ObservableCollection<string> SessionOptions { get; } = new();
        public ObservableCollection<Subject> SubjectOptions { get; } = new();
        public ObservableCollection<string> GradeOptions { get; } = new()
        {
            "Tất cả", "Xuất sắc", "Giỏi", "Khá", "Đạt", "Không đạt"
        };

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private string _selectedClass = "Tất cả";

        [ObservableProperty]
        private string _selectedSession = "Tất cả";

        [ObservableProperty]
        private Subject? _selectedSubject;

        [ObservableProperty]
        private string _selectedGrade = "Tất cả";

        [ObservableProperty]
        private DateTime? _fromDate;

        [ObservableProperty]
        private DateTime? _toDate;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private bool _isAdvancedFilterExpanded = true;
        #endregion

        #region THẺ KPI CHỈ HUY CHIẾN LƯỢC (COMMAND STRATEGY KPIS)
        [ObservableProperty]
        private int _totalCadets;

        [ObservableProperty]
        private int _totalUnitsCount;

        [ObservableProperty]
        private int _totalClassesCount;

        [ObservableProperty]
        private int _totalExamRecords;

        [ObservableProperty]
        private int _uniqueTestedCadets;

        [ObservableProperty]
        private double _passRate;

        [ObservableProperty]
        private double _eliteRate;

        [ObservableProperty]
        private int _excellentCount;

        [ObservableProperty]
        private int _goodCount;

        [ObservableProperty]
        private int _passCount;

        [ObservableProperty]
        private int _failCount;

        [ObservableProperty]
        private double _failRate;

        [ObservableProperty]
        private string _overallRatingLabel = "Đang tải...";

        [ObservableProperty]
        private string _overallRatingColor = "#1E3A8A";

        [ObservableProperty]
        private string _upcomingEventTitle = "Chưa có sự kiện gần nhất";

        [ObservableProperty]
        private string _upcomingEventTime = "";

        [ObservableProperty]
        private bool _hasUpcomingEvent;
        #endregion

        #region BIỂU ĐỒ & DỮ LIỆU PHÂN TÍCH (DATA COLLECTIONS)
        public ObservableCollection<UnitLeaderboardDto> UnitLeaderboard { get; } = new();
        public ObservableCollection<SubjectPerformanceDto> SubjectPerformances { get; } = new();
        public ObservableCollection<CadetHonorDto> HonoredCadets { get; } = new();
        public ObservableCollection<PhysicalExamRecord> FailedRecords { get; } = new();

        // 🤖 TRỢ LÝ ĐỀ XUẤT HUẤN LUYỆN AI
        [ObservableProperty]
        private StrategicDirectiveDto _aiStrategicDirective = new();

        public ObservableCollection<FitnessComponentPrescriptionDto> AiComponentPrescriptions { get; } = new();
        public ObservableCollection<PersonalizedCadetPrescriptionDto> AiPersonalizedPrescriptions { get; } = new();

        [ObservableProperty]
        private int _selectedTabIndex = 0; // 0: AI Đề Xuất, 1: Cần Bồi Dưỡng, 2: Vinh Danh, 3: Thi Đua Đơn Vị
        #endregion

        public DashboardViewModel(
            IDashboardAnalyticsService analyticsService,
            ITrainingRecommendationService recommendationService,
            ITrainingEventService eventService,
            ICadetService cadetService,
            IExcelService excelService,
            IFileDialogService fileDialogService)
        {
            _analyticsService = analyticsService;
            _recommendationService = recommendationService;
            _eventService = eventService;
            _cadetService = cadetService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;

            Title = "Trung Tâm Chỉ Huy & Phân Tích Rèn Luyện Thể Lực Quân Đội";

            _ = InitializeDashboardAsync();
        }

        public async Task InitializeDashboardAsync()
        {
            IsBusy = true;
            try
            {
                // Nạp danh sách bộ lọc
                var units = await _analyticsService.GetAvailableUnitsAsync();
                UnitOptions.Clear();
                foreach (var u in units) UnitOptions.Add(u);

                var classes = await _analyticsService.GetAvailableClassesAsync();
                ClassOptions.Clear();
                foreach (var c in classes) ClassOptions.Add(c);

                var sessions = await _analyticsService.GetAvailableSessionsAsync();
                SessionOptions.Clear();
                foreach (var s in sessions) SessionOptions.Add(s);

                var subjects = await _analyticsService.GetAvailableSubjectsAsync();
                SubjectOptions.Clear();
                foreach (var sub in subjects) SubjectOptions.Add(sub);
                SelectedSubject = SubjectOptions.FirstOrDefault();

                // Nạp sự kiện huấn luyện gần nhất
                await LoadUpcomingEventAsync();

                // Nạp dữ liệu phân tích Dashboard
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
                    UpcomingEventTitle = "Không có sự kiện sắp tới";
                    UpcomingEventTime = "Duy trì rèn luyện thường xuyên";
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
                    SubjectId = SelectedSubject?.Id,
                    Grade = SelectedGrade,
                    FromDate = FromDate,
                    ToDate = ToDate,
                    SearchKeyword = SearchKeyword
                };

                // 1. Tải Summary & KPI
                var summary = await _analyticsService.GetSummaryAsync(criteria);
                TotalCadets = summary.TotalCadets;
                TotalUnitsCount = summary.TotalUnitsCount;
                TotalClassesCount = summary.TotalClassesCount;
                TotalExamRecords = summary.TotalExamRecords;
                UniqueTestedCadets = summary.UniqueTestedCadets;
                PassRate = summary.OverallPassRate;
                EliteRate = summary.EliteRate;
                ExcellentCount = summary.ExcellentCount;
                GoodCount = summary.GoodCount;
                PassCount = summary.PassCount;
                FailCount = summary.FailCount;
                FailRate = summary.FailRate;
                OverallRatingLabel = summary.OverallRatingLabel;
                OverallRatingColor = summary.OverallRatingColor;

                // 2. Tải Xếp hạng đơn vị
                var units = await _analyticsService.GetUnitLeaderboardAsync(criteria);
                UnitLeaderboard.Clear();
                foreach (var u in units) UnitLeaderboard.Add(u);

                // 3. Tải Phân tích môn thể lực
                var subPerfs = await _analyticsService.GetSubjectPerformancesAsync(criteria);
                SubjectPerformances.Clear();
                foreach (var s in subPerfs) SubjectPerformances.Add(s);

                // 4. Tải Danh sách vinh danh học viên xuất sắc
                var honors = await _analyticsService.GetHonoredCadetsAsync(criteria, 15);
                HonoredCadets.Clear();
                foreach (var h in honors) HonoredCadets.Add(h);

                // 5. Tải Danh sách học viên chưa đạt chuẩn
                var failed = await _analyticsService.GetFailedRecordsAsync(criteria);
                FailedRecords.Clear();
                foreach (var f in failed) FailedRecords.Add(f);

                // 6. 🤖 Sinh Đề Xuất Huấn Luyện AI
                var filteredRecords = await _analyticsService.GetFilteredRecordsAsync(criteria);
                var allCadets = await _cadetService.GetAllCadetsAsync();
                var aiSummary = await _recommendationService.GenerateRecommendationsAsync(filteredRecords, allCadets, SelectedUnit);

                AiStrategicDirective = aiSummary.StrategicDirective;

                AiComponentPrescriptions.Clear();
                foreach (var p in aiSummary.ComponentPrescriptions) AiComponentPrescriptions.Add(p);

                AiPersonalizedPrescriptions.Clear();
                foreach (var pp in aiSummary.PersonalizedCadetPrescriptions) AiPersonalizedPrescriptions.Add(pp);

                StatusMessage = $"Cập nhật thành công số liệu: {TotalExamRecords} lượt kiểm tra, tỷ lệ đạt {PassRate:F1}%.";
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
            SelectedSubject = SubjectOptions.FirstOrDefault();
            SelectedGrade = "Tất cả";
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
            var fileName = $"BaoCao_TongQuan_DeXuatHuấnLuyen_QLHV_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            var filePath = _fileDialogService.ShowSaveFileDialog(
                fileName, 
                "Excel Files (*.xlsx)|*.xlsx", 
                "Xuất Báo Cáo Tổng Quan & Đề Xuất Huấn Luyện AI ra Excel");

            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var summary = new DashboardSummaryDto
                {
                    TotalCadets = TotalCadets,
                    TotalUnitsCount = TotalUnitsCount,
                    TotalClassesCount = TotalClassesCount,
                    TotalExamRecords = TotalExamRecords,
                    UniqueTestedCadets = UniqueTestedCadets,
                    ExcellentCount = ExcellentCount,
                    GoodCount = GoodCount,
                    PassCount = PassCount,
                    FailCount = FailCount
                };

                var aiRecSummary = new TrainingRecommendationSummaryDto
                {
                    StrategicDirective = AiStrategicDirective,
                    ComponentPrescriptions = AiComponentPrescriptions.ToList(),
                    PersonalizedCadetPrescriptions = AiPersonalizedPrescriptions.ToList()
                };

                var result = await _excelService.ExportDashboardExecutiveReportAsync(
                    filePath,
                    summary,
                    UnitLeaderboard.ToList(),
                    SubjectPerformances.ToList(),
                    aiRecSummary,
                    FailedRecords.ToList(),
                    HonoredCadets.ToList());

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
            var filePath = _fileDialogService.ShowOpenFileDialog("Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "Chọn tệp Excel để nhập/khôi phục toàn bộ dữ liệu");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ImportAllDataFromExcelAsync(filePath);
                StatusMessage = result.Message;
                if (result.Success)
                {
                    await InitializeDashboardAsync();
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
