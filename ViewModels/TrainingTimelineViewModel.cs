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
    public partial class TrainingTimelineViewModel : ViewModelBase
    {
        private readonly ITrainingEventService _eventService;
        private readonly ICatalogService _catalogService;

        [ObservableProperty]
        private ObservableCollection<TrainingEvent> _events = new();

        [ObservableProperty]
        private TrainingEvent? _selectedEvent;

        // Bộ lọc Timeline
        [ObservableProperty]
        private ObservableCollection<string> _categoryFilters = new()
        {
            "Tất cả",
            "Kiểm tra thể lực",
            "Thi cử quân sự",
            "Tập luyện / Rèn luyện",
            "Hội thao / Sự kiện"
        };

        [ObservableProperty]
        private string _selectedCategory = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<string> _statusFilters = new()
        {
            "Tất cả",
            "Đang chuẩn bị",
            "Đang diễn ra",
            "Đã hoàn thành",
            "Tạm hoãn"
        };

        [ObservableProperty]
        private string _selectedStatus = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<string> _unitOptions = new();

        [ObservableProperty]
        private ObservableCollection<string> _priorityOptions = new()
        {
            "Bình thường",
            "Cao",
            "Khẩn cấp"
        };

        // Form thêm / sửa
        [ObservableProperty]
        private bool _isFormVisible = false;

        [ObservableProperty]
        private string _formHeader = "Thêm Mốc Sự Kiện Mới";

        [ObservableProperty]
        private int _editId;

        [ObservableProperty]
        private string _editTitle = string.Empty;

        [ObservableProperty]
        private string _editCategory = "Kiểm tra thể lực";

        [ObservableProperty]
        private DateTime _editStartDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _editEndDate = DateTime.Today;

        [ObservableProperty]
        private string _editTargetUnit = "Toàn đơn vị";

        [ObservableProperty]
        private string _editLocation = string.Empty;

        [ObservableProperty]
        private string _editPriority = "Bình thường";

        [ObservableProperty]
        private string _editStatus = "Đang chuẩn bị";

        [ObservableProperty]
        private string _editDescription = string.Empty;

        // Thống kê nhanh
        [ObservableProperty]
        private int _totalEventsCount;

        [ObservableProperty]
        private int _ongoingEventsCount;

        [ObservableProperty]
        private int _completedEventsCount;

        [ObservableProperty]
        private int _urgentEventsCount;

        public TrainingTimelineViewModel(
            ITrainingEventService eventService,
            ICatalogService catalogService)
        {
            _eventService = eventService;
            _catalogService = catalogService;

            Title = "Lịch Huấn Luyện, Thi Cử & Mốc Sự Kiện Quân Sự";
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                var units = await _catalogService.GetAllUnitsAsync();
                UnitOptions.Clear();
                UnitOptions.Add("Toàn đơn vị");
                foreach (var u in units.OrderBy(x => x.UnitName))
                {
                    UnitOptions.Add(u.UnitName);
                }

                await LoadEventsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi nạp danh mục: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task LoadEventsAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                var items = await _eventService.GetFilteredEventsAsync(
                    SelectedCategory == "Tất cả" ? null : SelectedCategory,
                    SelectedStatus == "Tất cả" ? null : SelectedStatus,
                    null, null);

                Events.Clear();
                foreach (var item in items)
                {
                    Events.Add(item);
                }

                TotalEventsCount = Events.Count;
                OngoingEventsCount = Events.Count(e => e.Status == "Đang diễn ra");
                CompletedEventsCount = Events.Count(e => e.Status == "Đã hoàn thành");
                UrgentEventsCount = Events.Count(e => e.Priority == "Khẩn cấp" && e.Status != "Đã hoàn thành");

                if (Events.Any())
                {
                    SelectedEvent = Events[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu lịch trình: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void OpenAddForm()
        {
            FormHeader = "★ THÊM MỐC SỰ KIỆN HUẤN LUYỆN MỚI";
            EditId = 0;
            EditTitle = string.Empty;
            EditCategory = "Kiểm tra thể lực";
            EditStartDate = DateTime.Today;
            EditEndDate = DateTime.Today;
            EditTargetUnit = "Toàn đơn vị";
            EditLocation = string.Empty;
            EditPriority = "Bình thường";
            EditStatus = "Đang chuẩn bị";
            EditDescription = string.Empty;

            IsFormVisible = true;
        }

        [RelayCommand]
        public void OpenEditForm(TrainingEvent? evt)
        {
            var target = evt ?? SelectedEvent;
            if (target == null) return;

            FormHeader = "★ CHỈNH SỬA MỐC SỰ KIỆN HUẤN LUYỆN";
            EditId = target.Id;
            EditTitle = target.Title;
            EditCategory = target.Category;
            EditStartDate = target.StartDate;
            EditEndDate = target.EndDate;
            EditTargetUnit = target.TargetUnit;
            EditLocation = target.Location;
            EditPriority = target.Priority;
            EditStatus = target.Status;
            EditDescription = target.Description;

            IsFormVisible = true;
        }

        [RelayCommand]
        public void CloseForm()
        {
            IsFormVisible = false;
        }

        [RelayCommand]
        public async Task SaveEventAsync()
        {
            if (string.IsNullOrWhiteSpace(EditTitle))
            {
                MessageBox.Show("Vui lòng nhập tên/tiêu đề mốc sự kiện.", "Cảnh Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditEndDate.Date < EditStartDate.Date)
            {
                MessageBox.Show("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", "Cảnh Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                if (EditId == 0)
                {
                    // Thêm mới
                    var newEvt = new TrainingEvent
                    {
                        Title = EditTitle.Trim(),
                        Category = EditCategory,
                        StartDate = EditStartDate,
                        EndDate = EditEndDate,
                        TargetUnit = EditTargetUnit,
                        Location = EditLocation.Trim(),
                        Priority = EditPriority,
                        Status = EditStatus,
                        Description = EditDescription.Trim()
                    };

                    var (success, msg, _) = await _eventService.CreateEventAsync(newEvt);
                    if (success)
                    {
                        IsFormVisible = false;
                        await LoadEventsAsync();
                        MessageBox.Show(msg, "Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(msg, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Cập nhật
                    var updateEvt = new TrainingEvent
                    {
                        Id = EditId,
                        Title = EditTitle.Trim(),
                        Category = EditCategory,
                        StartDate = EditStartDate,
                        EndDate = EditEndDate,
                        TargetUnit = EditTargetUnit,
                        Location = EditLocation.Trim(),
                        Priority = EditPriority,
                        Status = EditStatus,
                        Description = EditDescription.Trim()
                    };

                    var (success, msg) = await _eventService.UpdateEventAsync(updateEvt);
                    if (success)
                    {
                        IsFormVisible = false;
                        await LoadEventsAsync();
                        MessageBox.Show(msg, "Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(msg, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu sự kiện:\n{ex.Message}", "Lỗi Ngoại Lệ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeleteEventAsync(TrainingEvent? evt)
        {
            var target = evt ?? SelectedEvent;
            if (target == null) return;

            var confirm = MessageBox.Show(
                $"Đồng chí có chắc chắn muốn xóa mốc sự kiện:\n'{target.Title}'?",
                "Xác Nhận Xóa Sự Kiện",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var (success, msg) = await _eventService.DeleteEventAsync(target.Id);
                if (success)
                {
                    if (IsFormVisible && EditId == target.Id)
                    {
                        IsFormVisible = false;
                    }
                    await LoadEventsAsync();
                }
                else
                {
                    MessageBox.Show(msg, "Lỗi Xóa", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi ngoại lệ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ToggleCompleteAsync(TrainingEvent? evt)
        {
            var target = evt ?? SelectedEvent;
            if (target == null) return;

            IsBusy = true;
            try
            {
                await _eventService.ToggleCompleteAsync(target.Id);
                await LoadEventsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ResetFilterAsync()
        {
            SelectedCategory = "Tất cả";
            SelectedStatus = "Tất cả";
            await LoadEventsAsync();
        }
    }
}
