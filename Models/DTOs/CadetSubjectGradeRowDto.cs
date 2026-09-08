using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.DTOs
{
    /// <summary>
    /// Đại diện cho một dòng học viên trong Ma trận Bảng Nhập Điểm theo Môn học
    /// </summary>
    public partial class CadetSubjectGradeRowDto : ObservableObject
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// Điểm của từng đợt kiểm tra theo ComponentId (Key = ComponentId, Value = Điểm số hoặc null nếu chưa thi)
        /// </summary>
        public Dictionary<int, double?> ComponentScores { get; set; } = new Dictionary<int, double?>();

        /// <summary>
        /// Mảng điểm thành phần để bind nhanh theo thứ tự cột đợt kiểm tra 1, 2, 3, 4, 5...
        /// </summary>
        private double? _score1;
        public double? Score1
        {
            get => _score1;
            set
            {
                if (SetProperty(ref _score1, value))
                    OnScoreChanged(1, value);
            }
        }

        private double? _score2;
        public double? Score2
        {
            get => _score2;
            set
            {
                if (SetProperty(ref _score2, value))
                    OnScoreChanged(2, value);
            }
        }

        private double? _score3;
        public double? Score3
        {
            get => _score3;
            set
            {
                if (SetProperty(ref _score3, value))
                    OnScoreChanged(3, value);
            }
        }

        private double? _score4;
        public double? Score4
        {
            get => _score4;
            set
            {
                if (SetProperty(ref _score4, value))
                    OnScoreChanged(4, value);
            }
        }

        private double? _score5;
        public double? Score5
        {
            get => _score5;
            set
            {
                if (SetProperty(ref _score5, value))
                    OnScoreChanged(5, value);
            }
        }

        private double? _score6;
        public double? Score6
        {
            get => _score6;
            set
            {
                if (SetProperty(ref _score6, value))
                    OnScoreChanged(6, value);
            }
        }

        private double? _calculatedSubjectScore;
        /// <summary>
        /// Điểm tổng kết môn học tự động tính từ các đợt kiểm tra theo công thức tỷ trọng
        /// </summary>
        public double? CalculatedSubjectScore
        {
            get => _calculatedSubjectScore;
            set
            {
                if (SetProperty(ref _calculatedSubjectScore, value))
                {
                    OnPropertyChanged(nameof(CalculatedSubjectScoreDisplay));
                    OnPropertyChanged(nameof(StatusBadgeText));
                    OnPropertyChanged(nameof(StatusBadgeBg));
                    OnPropertyChanged(nameof(StatusBadgeFg));
                }
            }
        }

        public string CalculatedSubjectScoreDisplay =>
            CalculatedSubjectScore.HasValue ? CalculatedSubjectScore.Value.ToString("F2") : "--";

        private bool _hasMissingInActiveComponent;
        /// <summary>
        /// Cờ cảnh báo: True nếu học viên chưa có điểm ở đợt kiểm tra mà đã có > 10 học viên khác có điểm
        /// </summary>
        public bool HasMissingInActiveComponent
        {
            get => _hasMissingInActiveComponent;
            set
            {
                if (SetProperty(ref _hasMissingInActiveComponent, value))
                {
                    OnPropertyChanged(nameof(RowBackground));
                    OnPropertyChanged(nameof(StatusBadgeText));
                    OnPropertyChanged(nameof(StatusBadgeBg));
                    OnPropertyChanged(nameof(StatusBadgeFg));
                }
            }
        }

        public string RowBackground => HasMissingInActiveComponent ? "#382914" : "Transparent";

        public string StatusBadgeText => HasMissingInActiveComponent ? "⚠️" : (CalculatedSubjectScore.HasValue ? "✅" : "⚪");
        public string StatusBadgeTooltip => HasMissingInActiveComponent ? "Chưa thi (đợt thi đã có >10 học viên có điểm)" : (CalculatedSubjectScore.HasValue ? "Đã có điểm đầy đủ" : "Chưa mở đợt thi");
        public string StatusBadgeBg => HasMissingInActiveComponent ? "#4A3315" : (CalculatedSubjectScore.HasValue ? "#143820" : "#253628");
        public string StatusBadgeFg => HasMissingInActiveComponent ? "#FBBF24" : (CalculatedSubjectScore.HasValue ? "#4ADE80" : "#94A3B8");

        public string MissingComponentsDisplay { get; set; } = string.Empty;

        public Action<CadetSubjectGradeRowDto, int, double?>? OnScoreUpdatedCallback { get; set; }

        private void OnScoreChanged(int colIndex, double? newScore)
        {
            OnScoreUpdatedCallback?.Invoke(this, colIndex, newScore);
        }
    }
}
