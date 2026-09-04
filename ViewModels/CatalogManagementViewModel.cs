using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class CatalogManagementViewModel : ViewModelBase
    {
        private readonly ICatalogService _catalogService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;

        public ObservableCollection<MilitaryRank> Ranks { get; } = new();
        public ObservableCollection<MilitaryPosition> Positions { get; } = new();
        public ObservableCollection<MilitaryUnit> Units { get; } = new();
        public ObservableCollection<MilitaryMajor> Majors { get; } = new();

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

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

        public CatalogManagementViewModel(
            ICatalogService catalogService,
            IExcelService excelService,
            IFileDialogService fileDialogService)
        {
            _catalogService = catalogService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            Title = "Danh Mục Tổ Chức Quân Sự";

            _ = LoadAllDataAsync();
        }

        [RelayCommand]
        public async Task LoadAllDataAsync()
        {
            IsBusy = true;
            try
            {
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
        public async Task SearchAsync()
        {
            IsBusy = true;
            try
            {
                switch (SelectedTabIndex)
                {
                    case 0: // Cấp bậc
                        var rList = await _catalogService.SearchRanksAsync(SearchKeyword, null);
                        Ranks.Clear();
                        foreach (var r in rList) Ranks.Add(r);
                        StatusMessage = $"Tìm thấy {Ranks.Count} cấp bậc.";
                        break;
                    case 1: // Chức vụ
                        var pList = await _catalogService.SearchPositionsAsync(SearchKeyword, null);
                        Positions.Clear();
                        foreach (var p in pList) Positions.Add(p);
                        StatusMessage = $"Tìm thấy {Positions.Count} chức vụ.";
                        break;
                    case 2: // Đơn vị
                        var uList = await _catalogService.SearchUnitsAsync(SearchKeyword, null);
                        Units.Clear();
                        foreach (var u in uList) Units.Add(u);
                        StatusMessage = $"Tìm thấy {Units.Count} đơn vị.";
                        break;
                    case 3: // Chuyên ngành
                        var mList = await _catalogService.SearchMajorsAsync(SearchKeyword, null);
                        Majors.Clear();
                        foreach (var m in mList) Majors.Add(m);
                        StatusMessage = $"Tìm thấy {Majors.Count} chuyên ngành.";
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
        public void OpenAddModal()
        {
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
                    FormTitle = "Thêm Đơn Vị Quản Lý Mới";
                    FormParentUnit = "Tiểu đoàn 1";
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
        public void OpenEditModal()
        {
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
                    FormTitle = $"Chỉnh Sửa Đơn Vị: {SelectedUnit.UnitName}";
                    FormCode = SelectedUnit.UnitCode;
                    FormName = SelectedUnit.UnitName;
                    FormParentUnit = SelectedUnit.ParentUnit ?? string.Empty;
                    FormCommanderName = SelectedUnit.CommanderName ?? string.Empty;
                    FormContactPhone = SelectedUnit.ContactPhone ?? string.Empty;
                    FormDescription = SelectedUnit.Description ?? string.Empty;
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
                        var unit = new MilitaryUnit
                        {
                            Id = IsEditing && SelectedUnit != null ? SelectedUnit.Id : 0,
                            UnitCode = FormCode.Trim(),
                            UnitName = FormName.Trim(),
                            ParentUnit = FormParentUnit?.Trim(),
                            CommanderName = FormCommanderName?.Trim(),
                            ContactPhone = FormContactPhone?.Trim(),
                            Description = FormDescription?.Trim()
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
                case 2:
                    if (SelectedUnit == null) return;
                    itemDesc = $"Đơn vị: {SelectedUnit.UnitName} ({SelectedUnit.UnitCode})";
                    break;
                case 3:
                    if (SelectedMajor == null) return;
                    itemDesc = $"Chuyên ngành: {SelectedMajor.MajorName} ({SelectedMajor.MajorCode})";
                    break;
            }

            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa {itemDesc} không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

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
                    case 2:
                        res = await _catalogService.DeleteUnitAsync(SelectedUnit!.Id);
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
        }
    }
}
