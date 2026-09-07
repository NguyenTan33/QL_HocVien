using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Services;
using QL_HocVien.ViewModels;
using QL_HocVien.Views.UserControls;
using Xunit;

namespace QL_HocVien.Tests
{
    public class ExcelImportAndCalendarTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ITrainingEventRepository _eventRepository;
        private readonly ITrainingEventService _eventService;
        private readonly ICatalogService _catalogService;
        private readonly IExcelService _excelService;
        private readonly string _dbName;
        private readonly string _tempExcelDir;

        public ExcelImportAndCalendarTests()
        {
            _dbName = $"TestDb_ExcelCal_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbName}.db")
                .Options;

            _context = new AppDbContext(options);
            DbInitializer.Initialize(_context);

            _eventRepository = new TrainingEventRepository(_context);
            _eventService = new TrainingEventService(_eventRepository);

            var cadetRepo = new CadetRepository(_context);
            var classRepo = new ClassRepository(_context);
            var subjectRepo = new SubjectRepository(_context);
            var examRepo = new PhysicalExamRepository(_context);
            var evalService = new EvaluationService();
            var offRepo = new OfficerRepository(_context);
            var rankRepo = new RankRepository(_context);
            var posRepo = new PositionRepository(_context);
            var unitRepo = new UnitRepository(_context);
            var majorRepo = new MajorRepository(_context);
            var secValidator = new QL_HocVien.Infrastructure.Security.ExcelSecurityValidator();

            _catalogService = new CatalogService(rankRepo, posRepo, unitRepo, majorRepo);

            _excelService = new ExcelService(
                _context,
                cadetRepo,
                classRepo,
                subjectRepo,
                examRepo,
                evalService,
                offRepo,
                rankRepo,
                posRepo,
                unitRepo,
                majorRepo,
                secValidator);

            _tempExcelDir = Path.Combine(Path.GetTempPath(), $"ExcelTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempExcelDir);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            try
            {
                if (File.Exists($"{_dbName}.db")) File.Delete($"{_dbName}.db");
                if (Directory.Exists(_tempExcelDir)) Directory.Delete(_tempExcelDir, true);
            }
            catch { }
        }

        [Fact]
        public async Task Test_ImportCadets_WithoutSttColumn_DoesNotShiftColumns()
        {
            // Mô phỏng đúng file Excel thực tế người dùng tải lên:
            // Cột A: "Mã học viên" (chứa ký tự xuống dòng \n)
            // Cột B: "Họ và tên"
            // Cột C: "Cấp bậc"
            // Cột D: "Chức vụ"
            // Cột E: "Đơn vị"
            // Cột F: "Lớp"
            // Không hề có cột STT!
            string filePath = Path.Combine(_tempExcelDir, "Cadets_NoSTT.xlsx");
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Danh sách học viên");
                ws.Cell(1, 1).Value = "Mã học viên";
                ws.Cell(1, 2).Value = "Họ và tên";
                ws.Cell(1, 3).Value = "Cấp bậc";
                ws.Cell(1, 4).Value = "Chức vụ";
                ws.Cell(1, 5).Value = "Đơn vị";
                ws.Cell(1, 6).Value = "Lớp";
                ws.Cell(1, 7).Value = "Số điện thoại";
                ws.Cell(1, 8).Value = "Email";

                // Dòng dữ liệu mẫu bị xuống dòng \n như ảnh thực tế của người dùng
                ws.Cell(2, 1).Value = "ĐH.07\n5.276";
                ws.Cell(2, 2).Value = "Đặng Thắng An";
                ws.Cell(2, 3).Value = "Trung sĩ";
                ws.Cell(2, 4).Value = "Học viên";
                ws.Cell(2, 5).Value = "Trung đội 1";
                ws.Cell(2, 6).Value = "K26A";
                ws.Cell(2, 7).Value = "0987654321";
                ws.Cell(2, 8).Value = "an.dt@hocvien.edu.vn";

                wb.SaveAs(filePath);
            }

            var (success, msg, cadets) = await _excelService.ImportCadetsFromExcelAsync(filePath);

            Assert.True(success, msg);
            Assert.NotEmpty(cadets);

            var imported = cadets.FirstOrDefault(c => c.FullName == "Đặng Thắng An");
            Assert.NotNull(imported);

            // Kiểm tra: Mã học viên phải được làm sạch xuống dòng và KHÔNG bị gán họ tên!
            Assert.Equal("ĐH.07 5.276", imported.CadetCode);
            Assert.Equal("Đặng Thắng An", imported.FullName);
            Assert.Equal("Trung sĩ", imported.Rank);
            Assert.Equal("Học viên", imported.Position);
            Assert.Equal("Trung đội 1", imported.Unit);
            Assert.Contains("K26A", imported.ClassName);
            Assert.Equal("0987654321", imported.PhoneNumber);
        }

        [Fact]
        public async Task Test_ImportCadets_WithSttColumn_WorksCorrectly()
        {
            // Trường hợp file có cột STT ở cột 1
            string filePath = Path.Combine(_tempExcelDir, "Cadets_WithSTT.xlsx");
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Danh sách học viên");
                ws.Cell(1, 1).Value = "STT";
                ws.Cell(1, 2).Value = "Mã học viên";
                ws.Cell(1, 3).Value = "Họ và tên";
                ws.Cell(1, 4).Value = "Cấp bậc";
                ws.Cell(1, 5).Value = "Chức vụ";
                ws.Cell(1, 6).Value = "Đơn vị";
                ws.Cell(1, 7).Value = "Lớp";

                ws.Cell(2, 1).Value = 1;
                ws.Cell(2, 2).Value = "HV-TEST-001";
                ws.Cell(2, 3).Value = "Nguyễn Văn B";
                ws.Cell(2, 4).Value = "Hạ sĩ";
                ws.Cell(2, 5).Value = "Tiểu đội trưởng";
                ws.Cell(2, 6).Value = "Đại đội 2";
                ws.Cell(2, 7).Value = "K26B";

                wb.SaveAs(filePath);
            }

            var (success, msg, cadets) = await _excelService.ImportCadetsFromExcelAsync(filePath);

            Assert.True(success, msg);
            var imported = cadets.FirstOrDefault(c => c.CadetCode == "HV-TEST-001");
            Assert.NotNull(imported);
            Assert.Equal("Nguyễn Văn B", imported.FullName);
            Assert.Equal("Hạ sĩ", imported.Rank);
            Assert.Equal("Tiểu đội trưởng", imported.Position);
            Assert.Equal("Đại đội 2", imported.Unit);
        }

        [Fact]
        public void Test_GenerateCalendarGrid_Generates42Days()
        {
            var vm = new TrainingTimelineViewModel(_eventService, _catalogService);
            vm.CurrentMonthDate = new DateTime(2026, 9, 1);

            vm.GenerateCalendarGrid();

            Assert.Equal(42, vm.CalendarDays.Count);

            // Kiểm tra tính liên tục của các ngày
            for (int i = 1; i < 42; i++)
            {
                Assert.Equal(vm.CalendarDays[i - 1].Date.AddDays(1).Date, vm.CalendarDays[i].Date.Date);
            }

            // Kiểm tra có chứa các ngày trong tháng 9/2026
            Assert.Contains(vm.CalendarDays, d => d.Date.Day == 1 && d.Date.Month == 9 && d.IsCurrentMonth);
            Assert.Contains(vm.CalendarDays, d => d.Date.Day == 30 && d.Date.Month == 9 && d.IsCurrentMonth);
        }

        [Fact]
        public async Task Test_CalendarDay_EventIndicators()
        {
            // Tạo 1 sự kiện kiểm tra thể lực vào ngày 15/09/2026 đến 17/09/2026
            var evt = new TrainingEvent
            {
                Title = "Kiểm tra 3 môn quân sự phối hợp",
                Category = "Thi cử quân sự",
                StartDate = new DateTime(2026, 9, 15),
                EndDate = new DateTime(2026, 9, 17),
                TargetUnit = "Đại đội 1",
                Location = "Thao trường A",
                Priority = "Khẩn cấp",
                Status = "Đang diễn ra"
            };
            await _eventService.CreateEventAsync(evt);

            var vm = new TrainingTimelineViewModel(_eventService, _catalogService);
            vm.CurrentMonthDate = new DateTime(2026, 9, 1);
            await vm.LoadEventsAsync();

            // Ngày 16/09/2026 phải có sự kiện và đánh dấu đỏ
            var day16 = vm.CalendarDays.FirstOrDefault(d => d.Date.Year == 2026 && d.Date.Month == 9 && d.Date.Day == 16);
            Assert.NotNull(day16);
            Assert.True(day16.HasEvents);
            Assert.True(day16.HasExamEvent);
            Assert.Equal("#DC2626", day16.PrimaryCategoryColor);

            // Ngày 25/09/2026 không có sự kiện
            var day25 = vm.CalendarDays.FirstOrDefault(d => d.Date.Year == 2026 && d.Date.Month == 9 && d.Date.Day == 25);
            Assert.NotNull(day25);
            Assert.False(day25.HasEvents);
        }

        [Fact]
        public void Test_TrainingTimelineView_Instantiation_And_ToggleViewMode()
        {
            Exception? exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    if (System.Windows.Application.Current == null)
                    {
                        var app = new System.Windows.Application();
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/QL_HocVien;component/Styles/MilitaryTheme.xaml", UriKind.Absolute)
                        });
                    }
                    else
                    {
                        bool hasTheme = System.Windows.Application.Current.Resources.MergedDictionaries.Any(d => d.Source != null && d.Source.ToString().Contains("MilitaryTheme"));
                        if (!hasTheme)
                        {
                            System.Windows.Application.Current.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QL_HocVien;component/Styles/MilitaryTheme.xaml", UriKind.Absolute)
                            });
                        }
                    }

                    var vm = new TrainingTimelineViewModel(_eventService, _catalogService);
                    var view = new TrainingTimelineView { DataContext = vm };
                    Assert.NotNull(view);

                    // Mặc định Timeline view
                    Assert.False(vm.IsCalendarViewVisible);

                    // Bấm nút chuyển sang Lịch tháng điện thoại
                    vm.SwitchToCalendarViewCommand.Execute(null);
                    Assert.True(vm.IsCalendarViewVisible);
                    Assert.Equal(42, vm.CalendarDays.Count);

                    // Bấm chuyển lại sang Timeline
                    vm.SwitchToTimelineViewCommand.Execute(null);
                    Assert.False(vm.IsCalendarViewVisible);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(5000);

            Assert.Null(exception);
        }
    }
}
