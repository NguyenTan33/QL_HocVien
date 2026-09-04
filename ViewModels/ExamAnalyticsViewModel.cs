using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class ExamAnalyticsViewModel : ViewModelBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ICatalogService _catalogService;
        private readonly IClassService _classService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;

        [ObservableProperty]
        private ObservableCollection<string> _availableSessions = new();

        [ObservableProperty]
        private string _selectedBaselineSession = string.Empty;

        [ObservableProperty]
        private string _selectedCompareSession = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _unitOptions = new();

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<string> _classOptions = new();

        [ObservableProperty]
        private string _selectedClass = "Tất cả";

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _trendOptions = new() { "Tất cả xu hướng", "▲ Tăng trưởng", "— Giữ nguyên", "▼ Thụt lùi" };

        [ObservableProperty]
        private string _selectedTrend = "Tất cả xu hướng";

        [ObservableProperty]
        private ExamComparisonResultDto? _comparisonResult;

        [ObservableProperty]
        private ObservableCollection<UnitComparisonDto> _unitComparisons = new();

        [ObservableProperty]
        private ObservableCollection<ClassComparisonDto> _classComparisons = new();

        [ObservableProperty]
        private ObservableCollection<CadetTrendDto> _cadetTrends = new();

        [ObservableProperty]
        private CadetTrendDto? _selectedCadetTrend;

        // KPI Thống kê
        [ObservableProperty]
        private int _totalCadetsEvaluated;

        [ObservableProperty]
        private int _growthCount;

        [ObservableProperty]
        private int _unchangedCount;

        [ObservableProperty]
        private int _regressionCount;

        [ObservableProperty]
        private double _growthPercentage;

        [ObservableProperty]
        private double _unchangedPercentage;

        [ObservableProperty]
        private double _regressionPercentage;

        [ObservableProperty]
        private double _passRateDelta;

        [ObservableProperty]
        private int _selectedTabIndex = 0; // 0: Đơn vị, 1: Lớp, 2: Cá nhân

        public ExamAnalyticsViewModel(
            IAnalyticsService analyticsService,
            ICatalogService catalogService,
            IClassService classService,
            IExcelService excelService,
            IFileDialogService fileDialogService)
        {
            _analyticsService = analyticsService;
            _catalogService = catalogService;
            _classService = classService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;

            Title = "Phân Tích & So Sánh Đợt Thi Rèn Luyện Thể Lực";
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                // 1. Nạp danh sách đơn vị
                var units = await _catalogService.GetAllUnitsAsync();
                UnitOptions.Clear();
                UnitOptions.Add("Tất cả");
                foreach (var u in units.OrderBy(x => x.UnitName))
                {
                    UnitOptions.Add(u.UnitName);
                }

                // 2. Nạp danh sách lớp
                var classes = await _classService.GetAllClassesAsync();
                ClassOptions.Clear();
                ClassOptions.Add("Tất cả");
                foreach (var cl in classes.OrderBy(x => x.ClassName))
                {
                    ClassOptions.Add(cl.ClassName);
                }

                // 3. Nạp danh sách đợt thi
                var sessions = await _analyticsService.GetAvailableSessionsAsync();
                AvailableSessions.Clear();
                foreach (var s in sessions)
                {
                    AvailableSessions.Add(s);
                }

                if (AvailableSessions.Count >= 2)
                {
                    // Đợt so sánh là mới nhất, đợt gốc là kế tiếp
                    SelectedCompareSession = AvailableSessions[0];
                    SelectedBaselineSession = AvailableSessions[1];
                    await ExecuteComparisonAsync();
                }
                else if (AvailableSessions.Count == 1)
                {
                    SelectedCompareSession = AvailableSessions[0];
                    SelectedBaselineSession = AvailableSessions[0];
                    await ExecuteComparisonAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu đợt thi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ExecuteComparisonAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedBaselineSession) || string.IsNullOrWhiteSpace(SelectedCompareSession))
            {
                ErrorMessage = "Vui lòng chọn cả Đợt gốc và Đợt so sánh để tiến hành phân tích.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _analyticsService.CompareSessionsAsync(
                    SelectedBaselineSession, 
                    SelectedCompareSession, 
                    SelectedUnit == "Tất cả" ? null : SelectedUnit,
                    null,
                    string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword);

                ComparisonResult = result;

                // KPI
                TotalCadetsEvaluated = result.TotalEvaluatedCadets;
                GrowthCount = result.OverallGrowthCount;
                UnchangedCount = result.OverallUnchangedCount;
                RegressionCount = result.OverallRegressionCount;
                GrowthPercentage = result.OverallGrowthPercentage;
                UnchangedPercentage = result.OverallUnchangedPercentage;
                RegressionPercentage = result.OverallRegressionPercentage;
                PassRateDelta = result.PassRateDelta;

                // Đơn vị
                UnitComparisons.Clear();
                foreach (var u in result.UnitComparisons)
                {
                    UnitComparisons.Add(u);
                }

                // Cấp Lớp
                ClassComparisons.Clear();
                var filteredClasses = result.ClassComparisons.AsEnumerable();
                if (SelectedUnit != "Tất cả")
                {
                    filteredClasses = filteredClasses.Where(c => c.Unit == SelectedUnit);
                }
                if (SelectedClass != "Tất cả")
                {
                    filteredClasses = filteredClasses.Where(c => c.ClassName == SelectedClass);
                }
                foreach (var cl in filteredClasses)
                {
                    ClassComparisons.Add(cl);
                }

                // Chi tiết cá nhân có lọc theo Trend
                ApplyCadetFilter();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi phân tích: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void ApplyCadetFilter()
        {
            if (ComparisonResult == null) return;

            var filtered = ComparisonResult.CadetTrends.AsEnumerable();

            // Lọc đơn vị
            if (SelectedUnit != "Tất cả")
            {
                filtered = filtered.Where(c => c.Unit == SelectedUnit);
            }

            // Lọc lớp
            if (SelectedClass != "Tất cả")
            {
                filtered = filtered.Where(c => c.ClassName == SelectedClass);
            }

            // Lọc từ khóa tìm kiếm (mã hoặc tên)
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var kw = SearchKeyword.Trim().ToLower();
                filtered = filtered.Where(c => c.FullName.ToLower().Contains(kw) || c.CadetCode.ToLower().Contains(kw));
            }

            // Lọc theo xu hướng
            if (SelectedTrend == "▲ Tăng trưởng")
            {
                filtered = filtered.Where(c => c.OverallTrend == TrendDirection.Growth);
            }
            else if (SelectedTrend == "— Giữ nguyên")
            {
                filtered = filtered.Where(c => c.OverallTrend == TrendDirection.Unchanged);
            }
            else if (SelectedTrend == "▼ Thụt lùi")
            {
                filtered = filtered.Where(c => c.OverallTrend == TrendDirection.Regression);
            }

            CadetTrends.Clear();
            foreach (var c in filtered)
            {
                CadetTrends.Add(c);
            }

            if (CadetTrends.Any())
            {
                SelectedCadetTrend = CadetTrends[0];
            }
            else
            {
                SelectedCadetTrend = null;
            }
        }

        [RelayCommand]
        public void ResetFilters()
        {
            SelectedUnit = "Tất cả";
            SelectedClass = "Tất cả";
            SelectedTrend = "Tất cả xu hướng";
            SearchKeyword = string.Empty;
            ApplyCadetFilter();
        }

        [RelayCommand]
        public async Task ExportExcelAsync()
        {
            if (ComparisonResult == null || (!UnitComparisons.Any() && !CadetTrends.Any()))
            {
                MessageBox.Show("Không có dữ liệu đối soát để xuất báo cáo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var defaultName = $"BaoCao_SoSanh_TheLuc_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            var filePath = _fileDialogService.ShowSaveFileDialog(defaultName, "Excel Files (*.xlsx)|*.xlsx", "Xuất Báo Cáo So Sánh Rèn Luyện Thể Lực");
            if (string.IsNullOrEmpty(filePath)) return;

            IsBusy = true;
            try
            {
                var exportDto = new ExamComparisonResultDto
                {
                    BaselineSession = SelectedBaselineSession,
                    CompareSession = SelectedCompareSession,
                    TotalEvaluatedCadets = TotalCadetsEvaluated,
                    OverallGrowthCount = GrowthCount,
                    OverallUnchangedCount = UnchangedCount,
                    OverallRegressionCount = RegressionCount,
                    BaselinePassRate = ComparisonResult.BaselinePassRate,
                    ComparePassRate = ComparisonResult.ComparePassRate,
                    UnitComparisons = UnitComparisons.ToList(),
                    ClassComparisons = ClassComparisons.ToList(),
                    CadetTrends = CadetTrends.ToList()
                };

                var (success, msg) = await _excelService.ExportComparisonToExcelAsync(exportDto, filePath);
                if (success)
                {
                    MessageBox.Show(msg, "Xuất Excel Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(msg, "Lỗi Xuất File", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi trong quá trình xuất Excel:\n{ex.Message}", "Lỗi Ngoại Lệ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
