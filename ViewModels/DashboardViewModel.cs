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
            IPhysicalExamService examService)
        {
            _cadetService = cadetService;
            _subjectService = subjectService;
            _examService = examService;
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
    }
}
