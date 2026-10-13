using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.DTOs
{
    /// <summary>
    /// DTO đại diện cho một mốc thời gian (node) trong dòng thời gian đào tạo (Timeline) của học viên
    /// </summary>
    public partial class CadetTimelineNodeDto : ObservableObject
    {
        public int Id { get; set; }
        public int CadetId { get; set; }

        public string SchoolYear { get; set; } = string.Empty;
        public int YearOrder { get; set; } = 1;

        public string YearLabel => $"Năm Thứ {YearOrder} ({SchoolYear})";

        public string Cohort { get; set; } = string.Empty;
        public string CohortDisplay => !string.IsNullOrWhiteSpace(Cohort) ? $"Khóa: {Cohort}" : string.Empty;

        public string Unit { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string UnitDisplay => !string.IsNullOrWhiteSpace(UnitName) ? UnitName : Unit;

        public string ClassName { get; set; } = string.Empty;

        public string Status { get; set; } = "Đang học";

        public double? AnnualGpa { get; set; }
        public string AnnualGpaDisplay => AnnualGpa.HasValue && AnnualGpa.Value > 0
            ? $"{AnnualGpa.Value:F2}"
            : "Chưa có điểm";

        public string AcademicRating
        {
            get
            {
                if (!AnnualGpa.HasValue || AnnualGpa.Value <= 0) return "Chưa có điểm";
                double score = AnnualGpa.Value;
                if (score >= 9.0) return "Xuất sắc";
                if (score >= 8.0) return "Giỏi";
                if (score >= 7.0) return "Khá";
                if (score >= 5.0) return "Trung bình";
                return "Yếu";
            }
        }

        public string RatingBadgeColor => AcademicRating switch
        {
            "Xuất sắc" => "#8B5CF6", // Tím
            "Giỏi" => "#3B82F6",     // Lam
            "Khá" => "#10B981",      // Lục
            "Trung bình" => "#F59E0B",// Vàng hổ phách
            "Yếu" => "#EF4444",      // Đỏ
            _ => "#64748B"           // Xám
        };

        public string TransferReason { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
        public string EffectiveDateDisplay => EffectiveDate.HasValue ? EffectiveDate.Value.ToString("dd/MM/yyyy") : "---";

        public bool IsCurrent { get; set; }
        public bool IsGraduated => string.Equals(Status, "Đã hoàn thành", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(Status, "Đã tốt nghiệp", StringComparison.OrdinalIgnoreCase);

        public string StatusBadgeColor => IsGraduated
            ? "#10B981"
            : (IsCurrent ? "#3B82F6" : "#64748B");
    }
}
