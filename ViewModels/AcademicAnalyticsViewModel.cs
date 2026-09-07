using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class AcademicAnalyticsViewModel : ViewModelBase
    {
        private readonly IAcademicAnalyticsService _academicAnalyticsService;
        private readonly ICatalogService _catalogService;
        private readonly IClassService _classService;
        private readonly IFileDialogService _fileDialogService;

        #region FILTER PROPERTIES
        [ObservableProperty]
        private ObservableCollection<string> _unitOptions = new();

        [ObservableProperty]
        private string _selectedUnit = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<string> _classOptions = new();

        [ObservableProperty]
        private string _selectedClass = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<string> _ratingOptions = new() 
        { 
            "Tất cả xếp loại", 
            "Giỏi", 
            "Khá", 
            "Trung bình", 
            "Yếu" 
        };

        [ObservableProperty]
        private string _selectedRating = "Tất cả xếp loại";

        [ObservableProperty]
        private ObservableCollection<string> _statusOptions = new() 
        { 
            "Tất cả trạng thái", 
            "✅ Đủ môn", 
            "⚠️ Thiếu môn" 
        };

        [ObservableProperty]
        private string _selectedStatus = "Tất cả trạng thái";

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private int _selectedTabIndex = 0; // 0: Đơn vị, 1: Lớp, 2: Cá nhân
        #endregion

        #region KPI SUMMARY PROPERTIES
        [ObservableProperty]
        private int _totalCadets;

        [ObservableProperty]
        private double _averageGpa;

        [ObservableProperty]
        private int _excellentCount;

        [ObservableProperty]
        private double _excellentPercentage;

        [ObservableProperty]
        private int _goodCount;

        [ObservableProperty]
        private double _goodPercentage;

        [ObservableProperty]
        private int _averageCount;

        [ObservableProperty]
        private double _averagePercentage;

        [ObservableProperty]
        private int _weakCount;

        [ObservableProperty]
        private double _weakPercentage;

        [ObservableProperty]
        private int _missingCount;

        [ObservableProperty]
        private double _missingPercentage;

        [ObservableProperty]
        private int _completedCount;

        [ObservableProperty]
        private double _completedPercentage;
        #endregion

        #region DATA COLLECTIONS
        [ObservableProperty]
        private ObservableCollection<AcademicUnitComparisonDto> _unitComparisons = new();

        [ObservableProperty]
        private ObservableCollection<AcademicClassComparisonDto> _classComparisons = new();

        [ObservableProperty]
        private ObservableCollection<AcademicCadetAnalyticsDto> _cadetAnalytics = new();

        [ObservableProperty]
        private AcademicCadetAnalyticsDto? _selectedCadet;

        private AcademicAnalyticsResultDto? _cachedResult;
        #endregion

        public AcademicAnalyticsViewModel(
            IAcademicAnalyticsService academicAnalyticsService,
            ICatalogService catalogService,
            IClassService classService,
            IFileDialogService fileDialogService)
        {
            _academicAnalyticsService = academicAnalyticsService;
            _catalogService = catalogService;
            _classService = classService;
            _fileDialogService = fileDialogService;

            Title = "Phân Tích & So Sánh Học Lực Toàn Đơn Vị";
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                // Nạp danh sách Đơn vị
                var units = await _catalogService.GetAllUnitsAsync();
                UnitOptions.Clear();
                UnitOptions.Add("Tất cả");
                foreach (var u in units.OrderBy(x => x.UnitName))
                {
                    UnitOptions.Add(u.UnitName);
                }

                // Nạp danh sách Lớp
                var classes = await _classService.GetAllClassesAsync();
                ClassOptions.Clear();
                ClassOptions.Add("Tất cả");
                foreach (var c in classes.OrderBy(x => x.ClassName))
                {
                    ClassOptions.Add(c.ClassName);
                }

                await ExecuteAnalyticsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khởi tạo phân tích: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ExecuteAnalyticsAsync()
        {
            IsBusy = true;
            StatusMessage = "Đang tổng hợp và phân tích học lực...";
            try
            {
                var result = await _academicAnalyticsService.GetAcademicAnalyticsAsync(
                    unit: SelectedUnit == "Tất cả" ? null : SelectedUnit,
                    className: SelectedClass == "Tất cả" ? null : SelectedClass,
                    rating: SelectedRating,
                    status: SelectedStatus,
                    keyword: SearchKeyword
                );

                _cachedResult = result;

                // Cập nhật KPI
                TotalCadets = result.TotalCadetsEvaluated;
                AverageGpa = result.AverageGpa;

                ExcellentCount = result.ExcellentCount;
                ExcellentPercentage = result.ExcellentPercentage;

                GoodCount = result.GoodCount;
                GoodPercentage = result.GoodPercentage;

                AverageCount = result.AverageCount;
                AveragePercentage = result.AveragePercentage;

                WeakCount = result.WeakCount;
                WeakPercentage = result.WeakPercentage;

                MissingCount = result.MissingSubjectsCount;
                MissingPercentage = result.MissingSubjectsPercentage;

                CompletedCount = result.CompletedCadetsCount;
                CompletedPercentage = result.CompletedCadetsPercentage;

                // Cập nhật danh sách hiển thị
                UnitComparisons = new ObservableCollection<AcademicUnitComparisonDto>(result.UnitComparisons);
                ClassComparisons = new ObservableCollection<AcademicClassComparisonDto>(result.ClassComparisons);
                CadetAnalytics = new ObservableCollection<AcademicCadetAnalyticsDto>(result.CadetAnalytics);

                if (CadetAnalytics.Any())
                {
                    SelectedCadet = CadetAnalytics.First();
                }
                else
                {
                    SelectedCadet = null;
                }

                StatusMessage = $"Đã phân tích kết quả {TotalCadets} học viên. TBM bình quân: {AverageGpa:F2}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi phân tích: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ResetFiltersAsync()
        {
            SelectedUnit = "Tất cả";
            SelectedClass = "Tất cả";
            SelectedRating = "Tất cả xếp loại";
            SelectedStatus = "Tất cả trạng thái";
            SearchKeyword = string.Empty;

            await ExecuteAnalyticsAsync();
        }

        [RelayCommand]
        public async Task ExportExcelAsync()
        {
            if (_cachedResult == null || _cachedResult.TotalCadetsEvaluated == 0)
            {
                StatusMessage = "Không có dữ liệu phân tích để xuất file Excel.";
                return;
            }

            string defaultFileName = $"PhanTich_HocLuc_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string? filePath = _fileDialogService.ShowSaveFileDialog(defaultFileName);

            if (string.IsNullOrWhiteSpace(filePath))
                return;

            IsBusy = true;
            StatusMessage = "Đang xuất báo cáo Excel...";
            try
            {
                var res = await _academicAnalyticsService.ExportAcademicAnalyticsToExcelAsync(_cachedResult, filePath);
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
    }
}
