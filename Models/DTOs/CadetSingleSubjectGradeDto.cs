using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.DTOs
{
    /// <summary>
    /// DTO đại diện cho 1 đợt kiểm tra / thi của 1 học viên cụ thể trong Form Nhập Điểm Học Viên
    /// </summary>
    public partial class CadetSingleSubjectGradeDto : ObservableObject
    {
        public int ComponentId { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public double Credits { get; set; } = 1.0;

        private double? _score;
        public double? Score
        {
            get => _score;
            set
            {
                if (SetProperty(ref _score, value))
                {
                    OnScoreChangedAction?.Invoke();
                }
            }
        }

        private bool _hasMissingWarning;
        public bool HasMissingWarning
        {
            get => _hasMissingWarning;
            set => SetProperty(ref _hasMissingWarning, value);
        }

        public string WarningTooltip => HasMissingWarning ? "⚠️ Chưa thi (đợt thi đã có >10 học viên có điểm)" : string.Empty;

        public Action? OnScoreChangedAction { get; set; }
    }
}
