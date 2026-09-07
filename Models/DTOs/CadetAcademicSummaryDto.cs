using System;
using System.Collections.Generic;

namespace QL_HocVien.Models.DTOs
{
    public class CadetAcademicSummaryDto
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        // Điểm theo từng môn/thành phần tín chỉ (Key = CreditSubjectId, Value = FinalScore)
        public Dictionary<int, double?> SubjectScores { get; set; } = new();

        // Điểm theo từng đợt kiểm tra / thành phần con (Key = ComponentId, Value = FinalScore)
        public Dictionary<int, double?> ComponentScores { get; set; } = new();

        public double TotalCreditsEarned { get; set; }
        public double TotalCurriculumCredits { get; set; } = 62.90;
        public int TotalSubjectsCompleted { get; set; }
        public double Gpa { get; set; }

        // Nhận diện học viên thiếu môn (Dòng màu vàng)
        public int MissingSubjectsCount { get; set; } = 0;
        public List<string> MissingSubjectsList { get; set; } = new();
        public bool HasMissingSubjects => MissingSubjectsCount > 0;

        public string MissingSubjectsDisplay => HasMissingSubjects
            ? $"Thiếu {MissingSubjectsCount} môn: {string.Join(", ", MissingSubjectsList)}"
            : "Đủ tất cả môn học";

        public string StatusDisplay => HasMissingSubjects
            ? $"⚠️ Thiếu {MissingSubjectsCount} môn"
            : "✅ Đủ môn";

        public string StatusBadgeColor => HasMissingSubjects ? "#B45309" : "#15803D";
        public string StatusBadgeBg => HasMissingSubjects ? "#FEF08A" : "#DCFCE7";
        
        // Màu nền dòng: Vàng nhạt cảnh báo cho học viên thiếu môn (khớp dòng màu vàng trong Excel)
        public string RowBackground => HasMissingSubjects ? "#FFFBEB" : "Transparent";

        // Danh sách phân rã điểm thành phần của các môn lớn
        public List<MajorSubjectBreakdownDto> MajorSubjectBreakdowns { get; set; } = new();

        public string AcademicRating
        {
            get
            {
                if (TotalSubjectsCompleted == 0) return "Chưa có điểm";
                if (Gpa >= 8.0) return "Giỏi";
                if (Gpa >= 7.0) return "Khá";
                if (Gpa >= 5.0) return "TB";
                return "Yếu";
            }
            set { }
        }

        public string RatingColor
        {
            get => AcademicRating switch
            {
                "Giỏi" => "#2563EB",
                "Khá" => "#16A34A",
                "TB" => "#D97706",
                "Yếu" => "#DC2626",
                _ => "#64748B"
            };
            set { }
        }

        public string RatingBackground
        {
            get => AcademicRating switch
            {
                "Giỏi" => "#DBEAFE",
                "Khá" => "#DCFCE7",
                "TB" => "#FEF3C7",
                "Yếu" => "#FEE2E2",
                _ => "#F1F5F9"
            };
            set { }
        }
    }

    /// <summary>
    /// DTO mô tả một môn học lớn và điểm tổng hợp từ các thành phần con
    /// </summary>
    public class MajorSubjectBreakdownDto
    {
        public string MajorSubjectName { get; set; } = string.Empty;
        public double TotalCredits { get; set; }
        public double? FinalScore { get; set; }
        public bool IsComplete { get; set; } = true;
        public List<ComponentScoreDto> Components { get; set; } = new();
    }

    /// <summary>
    /// DTO mô tả chi tiết 1 thành phần con của môn học (vd CNTT1, CNTT2, CNTT, Thi CNTT)
    /// </summary>
    public class ComponentScoreDto
    {
        public string ComponentName { get; set; } = string.Empty;
        public double Credits { get; set; }
        public double? RecordedScore { get; set; }
        
        // Tỷ lệ chiếm trong môn: Credits / TotalCredits môn lớn
        public double WeightRatio { get; set; }

        // Điểm thành phần chiếm trong môn = RecordedScore * Credits / TotalCredits
        public double? ContributionScore { get; set; }

        public string ScoreDisplay => RecordedScore.HasValue ? $"{RecordedScore.Value:F1}" : "Chưa có";
        public string ContributionDisplay => ContributionScore.HasValue ? $"{ContributionScore.Value:F2} đ" : "0.00 đ";
    }
}
