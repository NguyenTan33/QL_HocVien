using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models;
using QL_HocVien.Services;

namespace QL_HocVien.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly ICadetService _cadetService;
        private readonly ISubjectService _subjectService;
        private readonly IPhysicalExamService _examService;
        private readonly IExcelService _excelService;
        private readonly IFileDialogService _fileDialogService;

        [ObservableProperty]
        private int _totalCadets;

        [ObservableProperty]
        private int _totalSubjects;

        [ObservableProperty]
        private int _totalExamRecords;

        [ObservableProperty]
        private int _excellentCount;

        [ObservableProperty]
        private int _goodCount;

        [ObservableProperty]
        private int _passCount;

        [ObservableProperty]
        private int _failCount;

        [ObservableProperty]
        private double _passRate;

        public ObservableCollection<PhysicalExamRecord> FailedRecords { get; } = new();

        public DashboardViewModel(
            ICadetService cadetService,
            ISubjectService subjectService,
            IPhysicalExamService examService,
            IExcelService excelService,
            IFileDialogService fileDialogService)
        {
            _cadetService = cadetService;
            _subjectService = subjectService;
            _examService = examService;
            _excelService = excelService;
            _fileDialogService = fileDialogService;
            Title = "Bảng Tổng Quan & Báo Cáo Rèn Luyện Thể Lực";

            _ = LoadDashboardDataAsync();
        }

        [RelayCommand]
        public async Task LoadDashboardDataAsync()
        {
            IsBusy = true;
            try
            {
                var cadets = await _cadetService.GetAllCadetsAsync();
                var subjects = await _subjectService.GetAllSubjectsAsync();
                var records = (await _examService.GetAllRecordsAsync()).ToList();

                TotalCadets = cadets.Count();
                TotalSubjects = subjects.Count();
                TotalExamRecords = records.Count;

                ExcellentCount = records.Count(r => r.Grade == "Xuất sắc");
                GoodCount = records.Count(r => r.Grade == "Giỏi");
                PassCount = records.Count(r => r.Grade == "Khá" || r.Grade == "Đạt");
                FailCount = records.Count(r => r.Grade == "Không đạt");

                if (TotalExamRecords > 0)
                {
                    PassRate = Math.Round((double)(TotalExamRecords - FailCount) / TotalExamRecords * 100, 1);
                }
                else
                {
                    PassRate = 100.0;
                }

                FailedRecords.Clear();
                var failed = await _examService.GetFailedRecordsAsync();
                foreach (var item in failed)
                {
                    FailedRecords.Add(item);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu tổng quan: {ex.Message}";
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
                    await LoadDashboardDataAsync();
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
