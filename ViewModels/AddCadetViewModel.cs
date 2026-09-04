using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class AddCadetViewModel : ViewModelBase
    {
        private readonly ICadetService _cadetService;
        private readonly IClassService _classService;
        private readonly ICatalogService _catalogService;

        public ObservableCollection<MilitaryClass> AvailableClasses { get; } = new();

        [ObservableProperty]
        private MilitaryClass? _selectedMilitaryClass;

        partial void OnSelectedMilitaryClassChanged(MilitaryClass? value)
        {
            if (value != null)
            {
                ClassName = value.ClassName;
                if (!string.IsNullOrWhiteSpace(value.Unit) && UnitList.Contains(value.Unit))
                {
                    SelectedUnit = value.Unit;
                }
            }
        }

        [ObservableProperty]
        private string _cadetCode = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _className = "K26A - Chỉ huy Tham mưu";

        [ObservableProperty]
        private string _selectedRank = "Binh nhì";

        [ObservableProperty]
        private string _selectedPosition = "Học viên";

        [ObservableProperty]
        private string _selectedUnit = "Đại đội 1";

        [ObservableProperty]
        private string _selectedGender = "Nam";

        [ObservableProperty]
        private DateTime? _dateOfBirth = new DateTime(2004, 1, 1);

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _successMessage = string.Empty;

        public ObservableCollection<string> RankList { get; } = new()
        {
            "Binh nhì", "Binh nhất", "Hạ sĩ", "Trung sĩ", "Thượng sĩ", "Chuẩn úy", "Thiếu úy", "Trung úy"
        };

        public ObservableCollection<string> UnitList { get; } = new()
        {
            "Đại đội 1", "Đại đội 2", "Đại đội 3", "Đại đội 4", "Trung đội 1", "Trung đội 2", "Trung đội 3"
        };

        public ObservableCollection<string> PositionList { get; } = new()
        {
            "Học viên", "Chiến sĩ", "Tiểu đội trưởng", "Lớp phó", "Lớp trưởng", "Tổ trưởng"
        };

        public ObservableCollection<string> GenderList { get; } = new() { "Nam", "Nữ" };

        public event Action? OnCadetSaved;

        public AddCadetViewModel(ICadetService cadetService, IClassService classService, ICatalogService catalogService)
        {
            _cadetService = cadetService;
            _classService = classService;
            _catalogService = catalogService;
            Title = "Thêm Mới Học Viên";

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadCatalogDropdownsAsync();
            await LoadAvailableClassesAsync();
            await GenerateSuggestedCodeAsync();
        }

        private async Task LoadCatalogDropdownsAsync()
        {
            try
            {
                var ranks = await _catalogService.GetRankDropdownAsync();
                if (ranks.Any())
                {
                    RankList.Clear();
                    foreach (var r in ranks) RankList.Add(r);
                    if (string.IsNullOrWhiteSpace(SelectedRank) || !RankList.Contains(SelectedRank))
                        SelectedRank = RankList[0];
                }

                var units = await _catalogService.GetUnitDropdownAsync();
                if (units.Any())
                {
                    UnitList.Clear();
                    foreach (var u in units) UnitList.Add(u);
                    if (string.IsNullOrWhiteSpace(SelectedUnit) || !UnitList.Contains(SelectedUnit))
                        SelectedUnit = UnitList[0];
                }

                var positions = await _catalogService.GetPositionDropdownAsync();
                if (positions.Any())
                {
                    PositionList.Clear();
                    foreach (var p in positions) PositionList.Add(p);
                    if (string.IsNullOrWhiteSpace(SelectedPosition) || !PositionList.Contains(SelectedPosition))
                        SelectedPosition = PositionList[0];
                }
            }
            catch
            {
                // Giữ giá trị mặc định nếu có ngoại lệ
            }
        }

        public async Task LoadAvailableClassesAsync()
        {
            try
            {
                var classes = await _classService.GetAllClassesAsync();
                AvailableClasses.Clear();
                foreach (var c in classes)
                {
                    AvailableClasses.Add(c);
                }
                if (AvailableClasses.Count > 0)
                {
                    SelectedMilitaryClass = AvailableClasses[0];
                }
            }
            catch
            {
                // Fallback nếu có lỗi
            }
        }

        [RelayCommand]
        public async Task GenerateSuggestedCodeAsync()
        {
            CadetCode = await _cadetService.GenerateSuggestedCadetCodeAsync();
        }

        [RelayCommand]
        private async Task SaveCadetAsync()
        {
            if (await ExecuteSaveAsync())
            {
                OnCadetSaved?.Invoke();
            }
        }

        [RelayCommand]
        private async Task SaveAndContinueAsync()
        {
            if (await ExecuteSaveAsync())
            {
                // Reset form để thêm tiếp
                FullName = string.Empty;
                PhoneNumber = string.Empty;
                Email = string.Empty;
                await GenerateSuggestedCodeAsync();
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            OnCadetSaved?.Invoke();
        }

        private async Task<bool> ExecuteSaveAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(CadetCode))
            {
                ErrorMessage = "Vui lòng nhập Mã học viên (hoặc dùng mã gợi ý có sẵn).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "Vui lòng nhập Họ và tên học viên.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ErrorMessage = "Vui lòng nhập Số điện thoại liên lạc.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ClassName))
            {
                ErrorMessage = "Vui lòng nhập Lớp học viên.";
                return false;
            }

            int? age = null;
            if (DateOfBirth.HasValue)
            {
                age = DateTime.Today.Year - DateOfBirth.Value.Year;
            }

            var cadet = new Cadet
            {
                CadetCode = CadetCode.Trim(),
                FullName = FullName.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                ClassId = SelectedMilitaryClass?.Id,
                ClassName = !string.IsNullOrWhiteSpace(ClassName) ? ClassName.Trim() : (SelectedMilitaryClass?.ClassName ?? string.Empty),
                Rank = SelectedRank,
                Position = SelectedPosition,
                Unit = SelectedUnit,
                Gender = SelectedGender,
                DateOfBirth = DateOfBirth,
                Age = age,
                Email = !string.IsNullOrWhiteSpace(Email) ? Email.Trim() : $"{CadetCode.ToLower().Replace("-", "")}@hocvien.edu.vn",
                CreatedAt = DateTime.Now
            };

            IsBusy = true;
            try
            {
                var result = await _cadetService.AddCadetAsync(cadet);
                if (result.Success)
                {
                    SuccessMessage = result.Message;
                    return true;
                }
                else
                {
                    ErrorMessage = result.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi lưu học viên: {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
