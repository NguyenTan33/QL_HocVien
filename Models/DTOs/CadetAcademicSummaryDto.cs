using System;
using System.Collections.Generic;
using QL_HocVien.Services;

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

        public string StatusBadgeColor => ThemeService.CurrentIsCombatMode
            ? (HasMissingSubjects ? "#FBBF24" : "#4ADE80")
            : (HasMissingSubjects ? "#92400E" : "#166534");

        public string StatusBadgeBg => ThemeService.CurrentIsCombatMode
            ? (HasMissingSubjects ? "#4A3315" : "#143820")
            : (HasMissingSubjects ? "#FEF3C7" : "#DCFCE7");
        
        // Màu nền dòng: Nâu hổ phách tác chiến cảnh báo tương phản cao cho học viên thiếu môn; Pastel ấm nhẹ trong chế độ hành chính
        public string RowBackground => HasMissingSubjects
            ? (ThemeService.CurrentIsCombatMode ? "#382914" : "#FEF3C7")
            : "Transparent";

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
            get => ThemeService.CurrentIsCombatMode
                ? (AcademicRating switch
                {
                    "Giỏi" => "#93C5FD",
                    "Khá" => "#86EFAC",
                    "TB" => "#FCD34D",
                    "Yếu" => "#FCA5A5",
                    _ => "#CBD5E1"
                })
                : (AcademicRating switch
                {
                    "Giỏi" => "#1E40AF",
                    "Khá" => "#166534",
                    "TB" => "#92400E",
                    "Yếu" => "#991B1B",
                    _ => "#475569"
                });
            set { }
        }

        public string RatingBackground
        {
            get => ThemeService.CurrentIsCombatMode
                ? (AcademicRating switch
                {
                    "Giỏi" => "#1E3A5F",
                    "Khá" => "#143D24",
                    "TB" => "#452A12",
                    "Yếu" => "#4A1A1A",
                    _ => "#253628"
                })
                : (AcademicRating switch
                {
                    "Giỏi" => "#DBEAFE",
                    "Khá" => "#DCFCE7",
                    "TB" => "#FEF3C7",
                    "Yếu" => "#FEE2E2",
                    _ => "#F1F5F9"
                });
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
