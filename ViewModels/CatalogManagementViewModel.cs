using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Models.Entity;
using QL_HocVien.Services;
using QL_HocVien.Services.Interfaces;

namespace QL_HocVien.ViewModels
{
    public partial class CatalogManagementViewModel : ViewModelBase
    {
        private readonly ICatalogService _catalogService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;
        private readonly ISecurityGateService _securityGate;
        private readonly ICohortService _cohortService;
        private readonly IClassService? _classService;

        public ObservableCollection<MilitaryRank> Ranks { get; } = new();
        public ObservableCollection<MilitaryPosition> Positions { get; } = new();
        public ObservableCollection<MilitaryUnit> Units { get; } = new();
        public ObservableCollection<MilitaryMajor> Majors { get; } = new();
        public ObservableCollection<UnitTreeNode> UnitTreeNodes { get; } = new();

        [ObservableProperty]
        private bool _isTreeViewMode = true;

        [ObservableProperty]
        private UnitTreeNode? _selectedTreeNode;

        public ObservableCollection<string> RankGroups { get; } = new()
        {
            "Tất cả", "Hạ sĩ quan - Binh sĩ", "Sĩ quan cấp Úy", "Sĩ quan cấp Tá", "Sĩ quan cấp Tướng"
        };

        public ObservableCollection<string> PositionGroups { get; } = new()
        {
            "Tất cả", "Học viên / Chiến sĩ", "Cán bộ Phân đội", "Cán bộ Chỉ huy", "Cán bộ Giảng dạy"
        };

        public ObservableCollection<string> ParentUnitFilters { get; } = new() { "Tất cả" };

        public ObservableCollection<string> DepartmentFilters { get; } = new()
        {
            "Tất cả", "Khoa Chiến thuật", "Khoa Binh chủng", "Khoa Quân sự chung", "Khoa Chỉ huy Tham mưu", "Khoa Hậu cần - Kỹ thuật"
        };

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedRankGroup = "Tất cả";

        [ObservableProperty]
        private string _selectedPositionGroup = "Tất cả";

        [ObservableProperty]
        private string _selectedParentUnitFilter = "Tất cả";

        [ObservableProperty]
        private string _selectedDepartmentFilter = "Tất cả";

        [ObservableProperty]
        private bool _isAdvancedFilterVisible;

        [ObservableProperty]
        private int _activeFilterCount;

        [ObservableProperty]
        private MilitaryRank? _selectedRank;

        [ObservableProperty]
        private MilitaryPosition? _selectedPosition;

        [ObservableProperty]
        private MilitaryUnit? _selectedUnit;

        [ObservableProperty]
        private MilitaryMajor? _selectedMajor;

        // Modal Form State
        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _formTitle = string.Empty;

        // Form Fields
        [ObservableProperty]
        private string _formCode = string.Empty;

        [ObservableProperty]
        private string _formName = string.Empty;

        [ObservableProperty]
        private string _formGroup = string.Empty;

        [ObservableProperty]
        private int _formDisplayOrder = 1;

        [ObservableProperty]
        private string _formDescription = string.Empty;

        // Unit Specific
        [ObservableProperty]
        private string _formParentUnit = string.Empty;

        [ObservableProperty]
        private string _formCommanderName = string.Empty;

        [ObservableProperty]
        private string _formContactPhone = string.Empty;

        // Major Specific
        [ObservableProperty]
        private string _formTrainingDuration = string.Empty;

        [ObservableProperty]
        private string _formDepartment = string.Empty;

        // Cohort Integration (Từ CSDL SQL)
        public ObservableCollection<AcademicCohort> AvailableCohorts { get; } = new();

        [ObservableProperty]
        private AcademicCohort? _selectedFormCohort;

        /// <summary>
        /// 0 = Trực thuộc Khóa học (K75, K26...)
        /// 1 = Trực thuộc Đơn vị cấp trên (Tiểu đoàn, Đại đội...)
        /// 2 = Đơn vị cấp cao nhất độc lập (Không có cấp trên)
        /// </summary>
        [ObservableProperty]
        private int _unitParentType;

        public bool IsRootUnitForm => UnitParentType == 0;
        public bool IsChildUnitForm => UnitParentType == 1;
        public bool IsIndependentUnitForm => UnitParentType == 2;

        partial void OnUnitParentTypeChanged(int value)
        {
            OnPropertyChanged(nameof(IsRootUnitForm));
            OnPropertyChanged(nameof(IsChildUnitForm));
            OnPropertyChanged(nameof(IsIndependentUnitForm));

            if (value == 0)
            {
                if (SelectedFormCohort != null)
                {
                    FormParentUnit = SelectedFormCohort.CohortCode;
                }
                else if (AvailableCohorts.Count > 0)
                {
                    SelectedFormCohort = AvailableCohorts.FirstOrDefault();
                    FormParentUnit = SelectedFormCohort?.CohortCode ?? string.Empty;
                }
            }
            else if (value == 2)
            {
                FormParentUnit = string.Empty;
            }
        }

        partial void OnSelectedFormCohortChanged(AcademicCohort? value)
        {
            if (value != null && UnitParentType == 0)
            {
                FormParentUnit = value.CohortCode;
            }
        }

        public CatalogManagementViewModel(
            ICatalogService catalogService,
            IExcelService excelService,
            IFileDialogService fileDialogService,
            ISecurityGateService securityGate,
            IClassService classService)
            : this(catalogService, excelService, fileDialogService, securityGate, null, classService)
        {
        }

        public CatalogManagementViewModel(
            ICatalogService catalogService,
            IExcelService excelService,
            IFileDialogService fileDialogService,
            ISecurityGateService securityGate,
            ICohortService? cohortService = null,
            IClassService? classService = null)
        {
            _catalogService = catalogService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            _securityGate = securityGate;
            _cohortService = cohortService;
            _classService = classService;
            Title = "Danh Mục Tổ Chức Quân Sự";

            _ = LoadAllDataAsync();
        }

        public async Task LoadCohortsAsync()
        {
            if (_cohortService == null) return;
            try
            {
                var cohorts = await _cohortService.GetAllCohortsAsync();
                AvailableCohorts.Clear();
                foreach (var c in cohorts)
                {
                    AvailableCohorts.Add(c);
                }
            }
            catch { }
        }

        [RelayCommand]
        public async Task LoadAllDataAsync()
        {
            IsBusy = true;
            try
            {
                await LoadCohortsAsync();

                var ranks = await _catalogService.GetAllRanksAsync();
                Ranks.Clear();
                foreach (var r in ranks) Ranks.Add(r);

                var positions = await _catalogService.GetAllPositionsAsync();
                Positions.Clear();
                foreach (var p in positions) Positions.Add(p);

                var units = await _catalogService.GetAllUnitsAsync();
                Units.Clear();
                foreach (var u in units) Units.Add(u);

                var majors = await _catalogService.GetAllMajorsAsync();
                Majors.Clear();
                foreach (var m in majors) Majors.Add(m);

                await BuildUnitTreeAsync();

                StatusMessage = $"Đã tải: {Ranks.Count} cấp bậc, {Positions.Count} chức vụ, {Units.Count} đơn vị, {Majors.Count} chuyên ngành.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu danh mục: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void ToggleAdvancedFilter()
        {
            IsAdvancedFilterVisible = !IsAdvancedFilterVisible;
        }

        [RelayCommand]
        public void ResetFilters()
        {
            SearchKeyword = string.Empty;
            SelectedRankGroup = "Tất cả";
            SelectedPositionGroup = "Tất cả";
            SelectedParentUnitFilter = "Tất cả";
            SelectedDepartmentFilter = "Tất cả";
            _ = SearchAsync();
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            IsBusy = true;
            try
            {
                int count = 0;
                if (!string.IsNullOrWhiteSpace(SearchKeyword)) count++;
                if (SelectedRankGroup != "Tất cả" && SelectedTabIndex == 0) count++;
                if (SelectedPositionGroup != "Tất cả" && SelectedTabIndex == 1) count++;
                if (SelectedParentUnitFilter != "Tất cả" && SelectedTabIndex == 2) count++;
                if (SelectedDepartmentFilter != "Tất cả" && SelectedTabIndex == 3) count++;
                ActiveFilterCount = count;

                switch (SelectedTabIndex)
                {
                    case 0: // Cấp bậc
                        var rCriteria = new QL_HocVien.Models.Filters.CatalogFilterCriteria
                        {
                            Keyword = SearchKeyword,
                            Group = SelectedRankGroup
                        };
                        var rList = await _catalogService.SearchRanksAsync(rCriteria);
                        Ranks.Clear();
                        foreach (var r in rList) Ranks.Add(r);
                        StatusMessage = $"Tìm thấy {Ranks.Count} cấp bậc {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
                        break;
                    case 1: // Chức vụ
                        var pCriteria = new QL_HocVien.Models.Filters.CatalogFilterCriteria
                        {
                            Keyword = SearchKeyword,
                            Group = SelectedPositionGroup
                        };
                        var pList = await _catalogService.SearchPositionsAsync(pCriteria);
                        Positions.Clear();
                        foreach (var p in pList) Positions.Add(p);
                        StatusMessage = $"Tìm thấy {Positions.Count} chức vụ {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
                        break;
                    case 2: // Đơn vị
                        var uCriteria = new QL_HocVien.Models.Filters.CatalogFilterCriteria
                        {
                            Keyword = SearchKeyword,
                            ParentUnit = SelectedParentUnitFilter
                        };
                        var uList = await _catalogService.SearchUnitsAsync(uCriteria);
                        Units.Clear();
                        foreach (var u in uList) Units.Add(u);
                        StatusMessage = $"Tìm thấy {Units.Count} đơn vị {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
                        await BuildUnitTreeAsync();
                        break;
                    case 3: // Chuyên ngành
                        var mCriteria = new QL_HocVien.Models.Filters.CatalogFilterCriteria
                        {
                            Keyword = SearchKeyword,
                            Department = SelectedDepartmentFilter
                        };
                        var mList = await _catalogService.SearchMajorsAsync(mCriteria);
                        Majors.Clear();
                        foreach (var m in mList) Majors.Add(m);
                        StatusMessage = $"Tìm thấy {Majors.Count} chuyên ngành {(ActiveFilterCount > 0 ? $"({ActiveFilterCount} bộ lọc đang áp dụng)" : "")}.";
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tìm kiếm: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task OpenAddModalAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Thêm mới danh mục quân sự")) return;

            IsEditing = false;
            ClearForm();

            switch (SelectedTabIndex)
            {
                case 0:
                    FormTitle = "Thêm Cấp Bậc Quân Hàm Mới";
                    FormGroup = "Sĩ quan cấp Úy";
                    FormDisplayOrder = Ranks.Count > 0 ? Ranks.Max(r => r.DisplayOrder) + 1 : 1;
                    break;
                case 1:
                    FormTitle = "Thêm Chức Vụ Quân Sự Mới";
                    FormGroup = "Chỉ huy Phân đội";
                    FormDisplayOrder = Positions.Count > 0 ? Positions.Max(p => p.DisplayOrder) + 1 : 1;
                    break;
                case 2:
                    await LoadCohortsAsync();
                    FormTitle = "Thêm Đơn Vị Quản Lý Mới";
                    if (AvailableCohorts.Count > 0 && Units.Count == 0)
                    {
                        UnitParentType = 0;
                        SelectedFormCohort = AvailableCohorts.FirstOrDefault();
                        FormParentUnit = SelectedFormCohort?.CohortCode ?? string.Empty;
                    }
                    else if (Units.Count > 0)
                    {
                        UnitParentType = 1;
                        FormParentUnit = string.Empty;
                    }
                    else
                    {
                        UnitParentType = 2;
                        FormParentUnit = string.Empty;
                    }
                    break;
                case 3:
                    FormTitle = "Thêm Chuyên Ngành Đào Tạo Mới";
                    FormTrainingDuration = "4 năm";
                    FormDepartment = "Khoa Quân sự";
                    break;
            }

            IsFormVisible = true;
        }

        [RelayCommand]
        public async Task OpenEditModalAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Chỉnh sửa thông tin danh mục quân sự")) return;

            ClearForm();
            IsEditing = true;

            switch (SelectedTabIndex)
            {
                case 0:
                    if (SelectedRank == null)
                    {
                        MessageBox.Show("Vui lòng chọn một cấp bậc để chỉnh sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    FormTitle = $"Chỉnh Sửa Cấp Bậc: {SelectedRank.RankName}";
                    FormCode = SelectedRank.RankCode;
                    FormName = SelectedRank.RankName;
                    FormGroup = SelectedRank.RankGroup;
                    FormDisplayOrder = SelectedRank.DisplayOrder;
                    FormDescription = SelectedRank.Description ?? string.Empty;
                    break;

                case 1:
                    if (SelectedPosition == null)
                    {
                        MessageBox.Show("Vui lòng chọn một chức vụ để chỉnh sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    FormTitle = $"Chỉnh Sửa Chức Vụ: {SelectedPosition.PositionName}";
                    FormCode = SelectedPosition.PositionCode;
                    FormName = SelectedPosition.PositionName;
                    FormGroup = SelectedPosition.PositionGroup;
                    FormDisplayOrder = SelectedPosition.DisplayOrder;
                    FormDescription = SelectedPosition.Description ?? string.Empty;
                    break;

                case 2:
                    if (SelectedUnit == null)
                    {
                        MessageBox.Show("Vui lòng chọn một đơn vị để chỉnh sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    await LoadCohortsAsync();
                    FormTitle = $"Chỉnh Sửa Đơn Vị: {SelectedUnit.UnitName}";
                    FormCode = SelectedUnit.UnitCode;
                    FormName = SelectedUnit.UnitName;
                    FormCommanderName = SelectedUnit.CommanderName ?? string.Empty;
                    FormContactPhone = SelectedUnit.ContactPhone ?? string.Empty;
                    FormDescription = SelectedUnit.Description ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(SelectedUnit.ParentUnit) ||
                        SelectedUnit.ParentUnit.Equals("Học viện", StringComparison.OrdinalIgnoreCase) ||
                        SelectedUnit.ParentUnit.Equals("Bộ chỉ huy", StringComparison.OrdinalIgnoreCase))
                    {
                        UnitParentType = 2; // Đơn vị cấp cao nhất độc lập
                        FormParentUnit = string.Empty;
                    }
                    else
                    {
                        var matchedCohort = AvailableCohorts.FirstOrDefault(c =>
                            c.CohortCode.Equals(SelectedUnit.ParentUnit, StringComparison.OrdinalIgnoreCase) ||
                            c.CohortName.Equals(SelectedUnit.ParentUnit, StringComparison.OrdinalIgnoreCase));

                        if (matchedCohort != null)
                        {
                            UnitParentType = 0; // Trực thuộc Khóa học
                            SelectedFormCohort = matchedCohort;
                            FormParentUnit = matchedCohort.CohortCode;
                        }
                        else
                        {
                            UnitParentType = 1; // Trực thuộc Đơn vị cấp trên
                            FormParentUnit = SelectedUnit.ParentUnit;
                        }
                    }
                    break;

                case 3:
                    if (SelectedMajor == null)
                    {
                        MessageBox.Show("Vui lòng chọn một chuyên ngành để chỉnh sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    FormTitle = $"Chỉnh Sửa Chuyên Ngành: {SelectedMajor.MajorName}";
                    FormCode = SelectedMajor.MajorCode;
                    FormName = SelectedMajor.MajorName;
                    FormTrainingDuration = SelectedMajor.TrainingDuration ?? string.Empty;
                    FormDepartment = SelectedMajor.Department ?? string.Empty;
                    FormDescription = SelectedMajor.Description ?? string.Empty;
                    break;
            }

            IsFormVisible = true;
        }

        [RelayCommand]
        public void CloseForm()
        {
            IsFormVisible = false;
            ClearForm();
        }

        [RelayCommand]
        public async Task SaveFormAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Lưu danh mục tổ chức")) return;

            if (string.IsNullOrWhiteSpace(FormCode) || string.IsNullOrWhiteSpace(FormName))
            {
                MessageBox.Show("Mã và Tên không được để trống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                switch (SelectedTabIndex)
                {
                    case 0: // Cấp bậc
                        var rank = new MilitaryRank
                        {
                            Id = IsEditing && SelectedRank != null ? SelectedRank.Id : 0,
                            RankCode = FormCode.Trim(),
                            RankName = FormName.Trim(),
                            RankGroup = string.IsNullOrWhiteSpace(FormGroup) ? "Sĩ quan cấp Úy" : FormGroup.Trim(),
                            DisplayOrder = FormDisplayOrder,
                            Description = FormDescription?.Trim()
                        };
                        (bool Success, string Message) rRes = IsEditing 
                            ? await _catalogService.UpdateRankAsync(rank)
                            : (await _catalogService.AddRankAsync(rank) is var rAdd ? (rAdd.Success, rAdd.Message) : (false, ""));
                        StatusMessage = rRes.Message;
                        if (!rRes.Success)
                        {
                            MessageBox.Show(rRes.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        break;

                    case 1: // Chức vụ
                        var pos = new MilitaryPosition
                        {
                            Id = IsEditing && SelectedPosition != null ? SelectedPosition.Id : 0,
                            PositionCode = FormCode.Trim(),
                            PositionName = FormName.Trim(),
                            PositionGroup = string.IsNullOrWhiteSpace(FormGroup) ? "Chỉ huy Phân đội" : FormGroup.Trim(),
                            DisplayOrder = FormDisplayOrder,
                            Description = FormDescription?.Trim()
                        };
                        (bool Success, string Message) pRes = IsEditing 
                            ? await _catalogService.UpdatePositionAsync(pos)
                            : (await _catalogService.AddPositionAsync(pos) is var pAdd ? (pAdd.Success, pAdd.Message) : (false, ""));
                        StatusMessage = pRes.Message;
                        if (!pRes.Success)
                        {
                            MessageBox.Show(pRes.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        break;

                    case 2: // Đơn vị
                        string parentUnit = "";
                        if (UnitParentType == 0) // Trực thuộc Khóa học
                        {
                            parentUnit = SelectedFormCohort?.CohortCode ?? FormParentUnit?.Trim() ?? string.Empty;
                        }
                        else if (UnitParentType == 1) // Trực thuộc Đơn vị cấp trên
                        {
                            parentUnit = FormParentUnit?.Trim() ?? string.Empty;
                        }
                        else // Đơn vị cấp cao nhất độc lập
                        {
                            parentUnit = string.Empty;
                        }

                        var unit = new MilitaryUnit
                        {
                            Id = IsEditing && SelectedUnit != null ? SelectedUnit.Id : 0,
                            UnitCode = FormCode.Trim(),
                            UnitName = FormName.Trim(),
                            ParentUnit = parentUnit.Trim(),
                            CommanderName = FormCommanderName?.Trim() ?? string.Empty,
                            ContactPhone = FormContactPhone?.Trim() ?? string.Empty,
                            Description = FormDescription?.Trim() ?? string.Empty
                        };
                        (bool Success, string Message) uRes = IsEditing 
                            ? await _catalogService.UpdateUnitAsync(unit)
                            : (await _catalogService.AddUnitAsync(unit) is var uAdd ? (uAdd.Success, uAdd.Message) : (false, ""));
                        StatusMessage = uRes.Message;
                        if (!uRes.Success)
                        {
                            MessageBox.Show(uRes.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        break;

                    case 3: // Chuyên ngành
                        var major = new MilitaryMajor
                        {
                            Id = IsEditing && SelectedMajor != null ? SelectedMajor.Id : 0,
                            MajorCode = FormCode.Trim(),
                            MajorName = FormName.Trim(),
                            TrainingDuration = FormTrainingDuration?.Trim(),
                            Department = FormDepartment?.Trim(),
                            Description = FormDescription?.Trim()
                        };
                        (bool Success, string Message) mRes = IsEditing 
                            ? await _catalogService.UpdateMajorAsync(major)
                            : (await _catalogService.AddMajorAsync(major) is var mAdd ? (mAdd.Success, mAdd.Message) : (false, ""));
                        StatusMessage = mRes.Message;
                        if (!mRes.Success)
                        {
                            MessageBox.Show(mRes.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        break;
                }

                IsFormVisible = false;
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeleteAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Xóa mục trong danh mục tổ chức")) return;

            if (SelectedTabIndex == 2)
            {
                if (SelectedUnit == null) return;
                var allUnits = (await _catalogService.GetAllUnitsAsync()).ToList();
                bool hasChildren = allUnits.Any(u => u.Id != SelectedUnit.Id &&
                    !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                    (string.Equals(u.ParentUnit.Trim(), SelectedUnit.UnitName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrWhiteSpace(SelectedUnit.UnitCode) && string.Equals(u.ParentUnit.Trim(), SelectedUnit.UnitCode.Trim(), StringComparison.OrdinalIgnoreCase))));
                bool cascade = false;
                if (hasChildren)
                {
                    var confirmBranch = MessageBox.Show(
                        $"Đơn vị '{SelectedUnit.UnitName}' hiện có các đơn vị trực thuộc.\n\n" +
                        "• Bấm 'Yes' để XÓA TOÀN BỘ đơn vị này và tất cả các đơn vị con trực thuộc.\n" +
                        "• Bấm 'No' để CHUYỂN các đơn vị con lên đơn vị cấp trên (không xóa con).\n" +
                        "• Bấm 'Cancel' để hủy thao tác.",
                        "Xác nhận xóa đơn vị có cấp con",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);
                    if (confirmBranch == MessageBoxResult.Cancel) return;
                    cascade = (confirmBranch == MessageBoxResult.Yes);
                }
                else
                {
                    var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa Đơn vị: {SelectedUnit.UnitName} ({SelectedUnit.UnitCode}) không?",
                        "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm != MessageBoxResult.Yes) return;
                }

                IsBusy = true;
                try
                {
                    var res = await _catalogService.DeleteUnitCascadeAsync(SelectedUnit.Id, cascade);
                    StatusMessage = res.Message;
                    if (!res.Success)
                    {
                        MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        await LoadAllDataAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsBusy = false;
                }
                return;
            }

            string itemDesc = "";
            switch (SelectedTabIndex)
            {
                case 0:
                    if (SelectedRank == null) return;
                    itemDesc = $"Cấp bậc: {SelectedRank.RankName} ({SelectedRank.RankCode})";
                    break;
                case 1:
                    if (SelectedPosition == null) return;
                    itemDesc = $"Chức vụ: {SelectedPosition.PositionName} ({SelectedPosition.PositionCode})";
                    break;
                case 3:
                    if (SelectedMajor == null) return;
                    itemDesc = $"Chuyên ngành: {SelectedMajor.MajorName} ({SelectedMajor.MajorCode})";
                    break;
            }

            var confirmGen = MessageBox.Show($"Bạn có chắc chắn muốn xóa {itemDesc} không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmGen != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                (bool Success, string Message) res = (false, "");
                switch (SelectedTabIndex)
                {
                    case 0:
                        res = await _catalogService.DeleteRankAsync(SelectedRank!.Id);
                        break;
                    case 1:
                        res = await _catalogService.DeletePositionAsync(SelectedPosition!.Id);
                        break;
                    case 3:
                        res = await _catalogService.DeleteMajorAsync(SelectedMajor!.Id);
                        break;
                }

                StatusMessage = res.Message;
                if (!res.Success)
                {
                    MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    await LoadAllDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ExportExcelAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Xuất danh mục tổ chức ra Excel")) return;

            var filePath = _fileDialogService.ShowSaveFileDialog("Excel Files (*.xlsx)|*.xlsx", "Danh_Muc_To_Chuc_Quan_Doi.xlsx");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ExportCatalogsToExcelAsync(filePath);
                StatusMessage = result.Message;
                MessageBox.Show(result.Message, result.Success ? "Thành công" : "Lỗi", MessageBoxButton.OK,
                                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ImportExcelAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Nhập danh mục tổ chức từ file Excel")) return;

            var filePath = _fileDialogService.ShowOpenFileDialog("Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", "Chọn file Excel danh mục tổ chức");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsBusy = true;
            try
            {
                var result = await _excelService.ImportCatalogsFromExcelAsync(filePath);
                StatusMessage = result.Message;
                MessageBox.Show(result.Message, result.Success ? "Thành công" : "Lỗi", MessageBoxButton.OK,
                                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (result.Success)
                {
                    await LoadAllDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nhập Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region Quản Lý Sơ Đồ Rễ Cây Đơn Vị & Đại Đội

        [RelayCommand]
        public void ToggleViewMode()
        {
            IsTreeViewMode = !IsTreeViewMode;
        }

        [RelayCommand]
        public void ExpandAllTree()
        {
            SetExpandRecursive(UnitTreeNodes, true);
        }

        [RelayCommand]
        public void CollapseAllTree()
        {
            SetExpandRecursive(UnitTreeNodes, false);
        }

        private void SetExpandRecursive(IEnumerable<UnitTreeNode> nodes, bool expand, HashSet<UnitTreeNode>? visitedNodes = null)
        {
            visitedNodes ??= new HashSet<UnitTreeNode>();
            foreach (var node in nodes)
            {
                if (!visitedNodes.Add(node)) continue;
                node.IsExpanded = expand;
                if (node.HasChildren)
                {
                    SetExpandRecursive(node.Children, expand, visitedNodes);
                }
            }
        }

        [RelayCommand]
        public void SelectTreeNode(UnitTreeNode? node)
        {
            if (node == null) return;
            DeselectAllRecursive(UnitTreeNodes);
            node.IsSelected = true;
            SelectedTreeNode = node;
            if (node.Unit != null)
            {
                SelectedUnit = node.Unit;
            }
        }

        private void DeselectAllRecursive(IEnumerable<UnitTreeNode> nodes, HashSet<UnitTreeNode>? visitedNodes = null)
        {
            visitedNodes ??= new HashSet<UnitTreeNode>();
            foreach (var n in nodes)
            {
                if (!visitedNodes.Add(n)) continue;
                n.IsSelected = false;
                if (n.HasChildren) DeselectAllRecursive(n.Children, visitedNodes);
            }
        }

        [RelayCommand]
        public async Task AddChildUnitAsync(UnitTreeNode? parentNode)
        {
            if (!await _securityGate.EnsureUnlockedAsync("Thêm mới đơn vị quân sự")) return;

            await LoadCohortsAsync();
            SelectedTabIndex = 2;
            IsEditing = false;
            ClearForm();

            if (parentNode?.CohortItem != null)
            {
                UnitParentType = 0; // Trực thuộc Khóa học
                FormTitle = $"Thêm Đơn Vị Trực Thuộc Khóa Học: {parentNode.Name}";
                SelectedFormCohort = AvailableCohorts.FirstOrDefault(c => c.CohortCode == parentNode.Code) ?? parentNode.CohortItem;
                FormParentUnit = parentNode.Code;
            }
            else
            {
                UnitParentType = 1; // Trực thuộc Đơn vị cấp trên
                FormTitle = $"Thêm Đơn Vị Trực Thuộc: {parentNode?.Name ?? "Đơn vị"}";
                FormParentUnit = parentNode?.Name ?? string.Empty;
            }
            IsFormVisible = true;
        }

        [RelayCommand]
        public async Task AddRootUnitAsync()
        {
            if (!await _securityGate.EnsureUnlockedAsync("Thêm mới đơn vị quân sự")) return;

            await LoadCohortsAsync();
            SelectedTabIndex = 2;
            IsEditing = false;
            ClearForm();

            if (AvailableCohorts.Count > 0)
            {
                UnitParentType = 0;
                FormTitle = "Thêm Đơn Vị Gốc (Trực thuộc Khóa học)";
                SelectedFormCohort = AvailableCohorts.FirstOrDefault();
                FormParentUnit = SelectedFormCohort?.CohortCode ?? string.Empty;
            }
            else
            {
                UnitParentType = 2;
                FormTitle = "Thêm Đơn Vị Cấp Cao Nhất (Độc Lập)";
                FormParentUnit = string.Empty;
            }
            IsFormVisible = true;
        }

        [RelayCommand]
        public async Task EditUnitNodeAsync(UnitTreeNode? node)
        {
            if (node?.Unit == null) return;
            SelectedTabIndex = 2;
            SelectedUnit = node.Unit;
            await OpenEditModalAsync();
        }

        [RelayCommand]
        public async Task DeleteUnitNodeAsync(UnitTreeNode? node)
        {
            if (node == null) return;

            // 1. Nếu là Lớp đào tạo học viên (ClassNode)
            if (node.ClassItem != null && _classService != null)
            {
                if (!await _securityGate.EnsureUnlockedAsync("Xóa lớp học viên khỏi cơ cấu")) return;

                var confirm = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa Lớp đào tạo:\n• {node.ClassItem.ClassName} (Mã: {node.ClassItem.ClassCode})\nkhỏi cơ cấu không?",
                    "Xác nhận xóa lớp học viên",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                IsBusy = true;
                try
                {
                    var res = await _classService.DeleteClassAsync(node.ClassItem.Id);
                    StatusMessage = res.Message;
                    if (!res.Success)
                    {
                        MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        await BuildUnitTreeAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xóa lớp: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsBusy = false;
                }
                return;
            }

            // 2. Nếu là Node Khóa học hoặc Node Nhóm ảo (Id == 0)
            if (node.CohortItem != null || node.IsVirtualNode || (node.Unit != null && node.Unit.Id == 0))
            {
                if (!await _securityGate.EnsureUnlockedAsync("Xóa liên kết cấp trên")) return;

                var confirm = MessageBox.Show(
                    $"Bạn có muốn giải phóng các đơn vị con trực thuộc '{node.Name}' để chuyển các đơn vị này thành đơn vị cấp cao nhất độc lập không?",
                    "Xác nhận giải phóng cấp trên",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                IsBusy = true;
                try
                {
                    var allUnits = (await _catalogService.GetAllUnitsAsync()).ToList();
                    var childUnits = allUnits.Where(u => !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                        (string.Equals(u.ParentUnit.Trim(), node.Name.Trim(), StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(u.ParentUnit.Trim(), node.Code.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();

                    foreach (var cu in childUnits)
                    {
                        cu.ParentUnit = string.Empty;
                        await _catalogService.UpdateUnitAsync(cu);
                    }
                    await LoadAllDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi giải phóng đơn vị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsBusy = false;
                }
                return;
            }

            // 3. Đơn vị quân sự thực tế có Id > 0
            if (node.Unit != null && node.Unit.Id > 0)
            {
                if (!await _securityGate.EnsureUnlockedAsync("Xóa đơn vị khỏi cơ cấu tổ chức")) return;

                var unitToDelete = node.Unit;
                var allUnits = (await _catalogService.GetAllUnitsAsync()).ToList();
                bool hasChildren = allUnits.Any(u => u.Id != unitToDelete.Id &&
                    !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                    (string.Equals(u.ParentUnit.Trim(), unitToDelete.UnitName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrWhiteSpace(unitToDelete.UnitCode) && string.Equals(u.ParentUnit.Trim(), unitToDelete.UnitCode.Trim(), StringComparison.OrdinalIgnoreCase))));

                bool cascade = false;
                if (hasChildren)
                {
                    var confirmBranch = MessageBox.Show(
                        $"Đơn vị '{unitToDelete.UnitName}' ({unitToDelete.UnitCode}) hiện có các đơn vị trực thuộc.\n\n" +
                        "• Bấm 'Yes' để XÓA TOÀN BỘ đơn vị này và tất cả các đơn vị con trực thuộc.\n" +
                        "• Bấm 'No' để CHUYỂN các đơn vị con lên đơn vị cấp trên (không xóa con).\n" +
                        "• Bấm 'Cancel' để hủy thao tác.",
                        "Xác nhận xóa đơn vị có cấp con",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (confirmBranch == MessageBoxResult.Cancel) return;
                    cascade = (confirmBranch == MessageBoxResult.Yes);
                }
                else
                {
                    var confirm = MessageBox.Show(
                        $"Bạn có chắc chắn muốn xóa Đơn vị: {unitToDelete.UnitName} ({unitToDelete.UnitCode}) không?",
                        "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm != MessageBoxResult.Yes) return;
                }

                IsBusy = true;
                try
                {
                    var res = await _catalogService.DeleteUnitCascadeAsync(unitToDelete.Id, cascade);
                    StatusMessage = res.Message;
                    if (!res.Success)
                    {
                        MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        await LoadAllDataAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xóa đơn vị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsBusy = false;
                }
                return;
            }
        }

        public async Task BuildUnitTreeAsync()
        {
            UnitTreeNodes.Clear();
            var allUnits = Units.ToList();

            List<MilitaryClass> classes = new();
            if (_classService != null)
            {
                try
                {
                    var clsList = await _classService.GetAllClassesAsync();
                    classes = clsList.ToList();
                }
                catch { }
            }

            List<AcademicCohort> cohorts = new();
            if (_cohortService != null)
            {
                try
                {
                    var cList = await _cohortService.GetAllCohortsAsync();
                    cohorts = cList.ToList();
                }
                catch { }
            }

            // 1. Tạo node Cấp Khóa Học cho các Khóa học thực tế trong CSDL
            var cohortNodes = new Dictionary<string, UnitTreeNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var cohort in cohorts)
            {
                var cNode = new UnitTreeNode
                {
                    CohortItem = cohort,
                    NodeId = $"cohort_{cohort.Id}",
                    Name = !string.IsNullOrWhiteSpace(cohort.CohortName) ? cohort.CohortName : cohort.CohortCode,
                    Code = cohort.CohortCode,
                    Value = cohort.CohortCode,
                    Level = 0,
                    LevelName = "CẤP KHÓA HỌC",
                    Commander = "Ban Chỉ huy Khóa học",
                    Phone = cohort.AcademicYear ?? "---",
                    Description = $"Khóa đào tạo: {cohort.CohortCode} ({cohort.AcademicYear})",
                    Icon = "🎓",
                    BadgeBrush = "#1E40AF",
                    IsExpanded = true
                };

                // Hiển thị node Khóa học trên sơ đồ nếu có đơn vị con trực thuộc
                bool hasChildUnits = allUnits.Any(u => !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                    (string.Equals(u.ParentUnit.Trim(), cohort.CohortCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(u.ParentUnit.Trim(), cohort.CohortName.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (hasChildUnits)
                {
                    cohortNodes[cohort.CohortCode] = cNode;
                    cohortNodes[cohort.CohortName] = cNode;
                    UnitTreeNodes.Add(cNode);
                }
            }

            // Gắn các đơn vị con trực thuộc Khóa học
            var attachedUnitIds = new HashSet<int>();
            foreach (var cNode in cohortNodes.Values.Distinct())
            {
                var childUnits = allUnits
                    .Where(u => u.Id > 0 &&
                                !attachedUnitIds.Contains(u.Id) &&
                                !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                                (string.Equals(u.ParentUnit.Trim(), cNode.Code.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(u.ParentUnit.Trim(), cNode.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var cu in childUnits)
                {
                    attachedUnitIds.Add(cu.Id);
                    int nextLevel = cNode.Level + 1;
                    var childNode = CreateUnitNode(cu, nextLevel, classes);
                    childNode.ParentNode = cNode;
                    cNode.Children.Add(childNode);
                    AttachChildrenRecursive(childNode, allUnits, classes, null, attachedUnitIds, 0);
                }
            }

            // 2. Tìm các đơn vị cấp cao nhất quân sự (không thuộc Khóa học)
            var existingUnitNames = new HashSet<string>(allUnits.Select(u => u.UnitName.Trim()), StringComparer.OrdinalIgnoreCase);
            var existingUnitCodes = new HashSet<string>(allUnits.Select(u => u.UnitCode.Trim()), StringComparer.OrdinalIgnoreCase);
            var cohortKeys = new HashSet<string>(cohortNodes.Keys, StringComparer.OrdinalIgnoreCase);

            var rootUnits = allUnits.Where(u =>
                u.Id > 0 &&
                !attachedUnitIds.Contains(u.Id) &&
                (string.IsNullOrWhiteSpace(u.ParentUnit) ||
                 u.ParentUnit.Equals("Học viện", StringComparison.OrdinalIgnoreCase) ||
                 u.ParentUnit.Equals("Bộ chỉ huy", StringComparison.OrdinalIgnoreCase) ||
                 (!existingUnitNames.Contains(u.ParentUnit.Trim()) &&
                  !existingUnitCodes.Contains(u.ParentUnit.Trim()) &&
                  !cohortKeys.Contains(u.ParentUnit.Trim())))
            ).ToList();

            foreach (var ru in rootUnits)
            {
                int level = DetermineLevel(ru);
                var node = CreateUnitNode(ru, level, classes);
                UnitTreeNodes.Add(node);
                attachedUnitIds.Add(ru.Id);
                AttachChildrenRecursive(node, allUnits, classes, null, attachedUnitIds, 0);
            }

            // 3. Đơn vị mồ côi còn sót lại (nếu có)
            foreach (var u in allUnits)
            {
                if (u.Id > 0 && !attachedUnitIds.Contains(u.Id))
                {
                    var orphanNode = CreateUnitNode(u, DetermineLevel(u), classes);
                    UnitTreeNodes.Add(orphanNode);
                    attachedUnitIds.Add(u.Id);
                    AttachChildrenRecursive(orphanNode, allUnits, classes, null, attachedUnitIds, 0);
                }
            }
        }

        private void CollectAddedUnitIds(IEnumerable<UnitTreeNode> nodes, HashSet<int> ids, HashSet<UnitTreeNode>? visitedNodes = null)
        {
            visitedNodes ??= new HashSet<UnitTreeNode>();
            foreach (var n in nodes)
            {
                if (!visitedNodes.Add(n)) continue;
                if (n.Unit != null && n.Unit.Id > 0) ids.Add(n.Unit.Id);
                if (n.HasChildren) CollectAddedUnitIds(n.Children, ids, visitedNodes);
            }
        }

        private int DetermineLevel(MilitaryUnit u)
        {
            string name = (u.UnitName ?? "").ToLowerInvariant();
            string code = (u.UnitCode ?? "").ToLowerInvariant();
            if (name.Contains("khóa") || code.StartsWith("k") || name.StartsWith("k")) return 0;
            if (name.Contains("trung đoàn") || name.Contains("học viện") || name.Contains("sư đoàn") || code.StartsWith("e")) return 1;
            if (name.Contains("tiểu đoàn") || code.StartsWith("d")) return 2;
            if (name.Contains("đại đội") || code.StartsWith("c")) return 3;
            if (name.Contains("trung đội") || name.Contains("lớp") || code.StartsWith("b")) return 4;
            if (name.Contains("tiểu đội") || code.StartsWith("a")) return 5;
            if (name.Contains("nhóm") || name.Contains("tổ") || code.StartsWith("n")) return 6;
            return 3;
        }

        private UnitTreeNode CreateUnitNode(MilitaryUnit u, int level, List<MilitaryClass> classes)
        {
            string levelName = level switch
            {
                0 => "CẤP KHÓA HỌC",
                1 => "CẤP TRUNG ĐOÀN",
                2 => "CẤP TIỂU ĐOÀN",
                3 => "CẤP ĐẠI ĐỘI",
                4 => "CẤP TRUNG ĐỘI",
                5 => "CẤP TIỂU ĐỘI",
                6 => "CẤP NHÓM / TỔ",
                _ => "PHÂN ĐỘI"
            };

            string icon = level switch
            {
                0 => "🎓",
                1 => "🏛️",
                2 => "🛡️",
                3 => "🚩",
                4 => "🎖️",
                5 => "🎯",
                6 => "🔹",
                _ => "🎖️"
            };

            string badgeBrush = level switch
            {
                0 => "#1E40AF",
                1 => "#8B1E1E",
                2 => "#2E5A36",
                3 => "#9C4116",
                4 => "#1E426D",
                5 => "#4F46E5",
                6 => "#0D9488",
                _ => "#334155"
            };

            var node = new UnitTreeNode
            {
                Unit = u,
                NodeId = $"unit_{u.Id}",
                Name = u.UnitName,
                Code = u.UnitCode,
                Value = !string.IsNullOrWhiteSpace(u.UnitName) ? u.UnitName : u.UnitCode,
                Level = level,
                LevelName = levelName,
                Commander = !string.IsNullOrWhiteSpace(u.CommanderName) ? u.CommanderName : "Chưa biên chế",
                Phone = !string.IsNullOrWhiteSpace(u.ContactPhone) ? u.ContactPhone : "---",
                Description = u.Description ?? string.Empty,
                Icon = icon,
                BadgeBrush = badgeBrush,
                IsExpanded = level <= 2
            };

            return node;
        }

        private void AttachChildrenRecursive(
            UnitTreeNode parentNode,
            List<MilitaryUnit> allUnits,
            List<MilitaryClass> classes,
            HashSet<int>? branchUnitIds = null,
            HashSet<int>? allAttachedIds = null,
            int depth = 0)
        {
            if (depth > 15) return; // Bảo vệ chống tràn stack tối đa 15 cấp

            branchUnitIds ??= new HashSet<int>();
            if (parentNode.Unit != null && parentNode.Unit.Id > 0)
            {
                branchUnitIds.Add(parentNode.Unit.Id);
            }

            allAttachedIds ??= new HashSet<int>();

            var childUnits = allUnits
                .Where(u => u.Id > 0 &&
                            !branchUnitIds.Contains(u.Id) &&
                            !allAttachedIds.Contains(u.Id) &&
                            !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                            (u.ParentUnit.Trim().Equals(parentNode.Name.Trim(), StringComparison.OrdinalIgnoreCase) ||
                             (!string.IsNullOrWhiteSpace(parentNode.Code) && u.ParentUnit.Trim().Equals(parentNode.Code.Trim(), StringComparison.OrdinalIgnoreCase))) &&
                            !string.Equals(u.UnitCode, parentNode.Code, StringComparison.OrdinalIgnoreCase) &&
                            !ReferenceEquals(u, parentNode.Unit))
                .ToList();

            foreach (var cu in childUnits)
            {
                allAttachedIds.Add(cu.Id);
                int nextLevel = parentNode.Level + 1;
                var childNode = CreateUnitNode(cu, nextLevel, classes);
                childNode.ParentNode = parentNode;
                parentNode.Children.Add(childNode);

                var nextBranch = new HashSet<int>(branchUnitIds) { cu.Id };
                AttachChildrenRecursive(childNode, allUnits, classes, nextBranch, allAttachedIds, depth + 1);
            }

            if (!parentNode.IsClassLeaf)
            {
                var unitClasses = classes
                    .Where(c => !string.IsNullOrWhiteSpace(c.Unit) &&
                                c.Unit.Trim().Equals(parentNode.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var cls in unitClasses)
                {
                    var classNode = new UnitTreeNode
                    {
                        ClassItem = cls,
                        ParentNode = parentNode,
                        NodeId = $"class_{cls.Id}",
                        Name = $"{cls.ClassCode} - {cls.ClassName}",
                        Code = cls.ClassCode,
                        Level = parentNode.Level + 1,
                        LevelName = "PHÂN ĐỘI / LỚP HỌC VIÊN",
                        Commander = !string.IsNullOrWhiteSpace(cls.OfficerInCharge) ? cls.OfficerInCharge : "Chưa phân công",
                        Phone = !string.IsNullOrWhiteSpace(cls.AcademicYear) ? cls.AcademicYear : "Niên khóa đào tạo",
                        Description = $"Chuyên ngành: {cls.Major} | Khóa: {cls.AcademicYear}",
                        Icon = "🎓",
                        BadgeBrush = "#1E426D",
                        IsClassLeaf = true,
                        IsExpanded = false
                    };
                    parentNode.Children.Add(classNode);
                }
            }
        }

        #endregion

        private void ClearForm()
        {
            FormCode = string.Empty;
            FormName = string.Empty;
            FormGroup = string.Empty;
            FormDisplayOrder = 1;
            FormDescription = string.Empty;
            FormParentUnit = string.Empty;
            FormCommanderName = string.Empty;
            FormContactPhone = string.Empty;
            FormTrainingDuration = string.Empty;
            FormDepartment = string.Empty;
            SelectedFormCohort = null;
        }

        partial void OnSearchKeywordChanged(string value) => _ = SearchAsync();
        partial void OnSelectedTabIndexChanged(int value)
        {
            if (!IsBusy) _ = SearchAsync();
        }
        partial void OnSelectedRankGroupChanged(string value) => _ = SearchAsync();
        partial void OnSelectedPositionGroupChanged(string value) => _ = SearchAsync();
        partial void OnSelectedParentUnitFilterChanged(string value) => _ = SearchAsync();
        partial void OnSelectedDepartmentFilterChanged(string value) => _ = SearchAsync();
    }
}
