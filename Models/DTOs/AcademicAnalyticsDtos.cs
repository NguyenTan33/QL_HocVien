using System;
using System.Collections.Generic;

namespace QL_HocVien.Models.DTOs
{
    /// <summary>
    /// Kết quả tổng hợp phân tích học lực toàn diện (Đại đội -> Lớp -> Cá nhân)
    /// </summary>
    public class AcademicAnalyticsResultDto
    {
        public int TotalCadetsEvaluated { get; set; }
        public double AverageGpa { get; set; }

        // Xếp loại học lực chung
        public int ExcellentCount { get; set; }
        public double ExcellentPercentage { get; set; }

        public int GoodCount { get; set; }
        public double GoodPercentage { get; set; }

        public int AverageCount { get; set; }
        public double AveragePercentage { get; set; }

        public int WeakCount { get; set; }
        public double WeakPercentage { get; set; }

        // Tình trạng nợ/thiếu môn
        public int MissingSubjectsCount { get; set; }
        public double MissingSubjectsPercentage { get; set; }

        public int CompletedCadetsCount { get; set; }
        public double CompletedCadetsPercentage { get; set; }

        // Danh sách phân cấp
        public List<AcademicUnitComparisonDto> UnitComparisons { get; set; } = new();
        public List<AcademicClassComparisonDto> ClassComparisons { get; set; } = new();
        public List<AcademicCadetAnalyticsDto> CadetAnalytics { get; set; } = new();
    }

    /// <summary>
    /// So sánh và xếp hạng học lực theo Cấp Đại đội / Đơn vị
    /// </summary>
    public class AcademicUnitComparisonDto
    {
        public int RankInBattalion { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int TotalCadets { get; set; }
        public double AverageGpa { get; set; }

        public int ExcellentCount { get; set; }
        public double ExcellentRate { get; set; }

        public int GoodCount { get; set; }
        public double GoodRate { get; set; }

        public int AverageCount { get; set; }
        public double AverageRate { get; set; }

        public int WeakCount { get; set; }
        public double WeakRate { get; set; }

        public int CompletedCount { get; set; }
        public int MissingCount { get; set; }

        public string EvaluationComment { get; set; } = string.Empty;
        public string TrendColor => AverageGpa >= 7.5 ? "#16A34A" : (AverageGpa >= 6.5 ? "#2563EB" : "#D97706");
    }

    /// <summary>
    /// So sánh và xếp hạng học lực theo Cấp Lớp / Phân đội
    /// </summary>
    public class AcademicClassComparisonDto
    {
        public int RankInUnit { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int TotalCadets { get; set; }
        public double AverageGpa { get; set; }

        public int ExcellentCount { get; set; }
        public int GoodCount { get; set; }
        public int AverageWeakCount { get; set; }
        public double GoodOrAboveRate { get; set; }

        public int CompletedCount { get; set; }
        public int MissingCount { get; set; }

        public string EvaluationComment { get; set; } = string.Empty;
        public string TrendSymbol => AverageGpa >= 7.5 ? "▲" : (AverageGpa >= 6.5 ? "—" : "▼");
        public string TrendColor => AverageGpa >= 7.5 ? "#16A34A" : (AverageGpa >= 6.5 ? "#2563EB" : "#DC2626");
    }

    /// <summary>
    /// Phân tích học lực chi tiết theo từng học viên
    /// </summary>
    public class AcademicCadetAnalyticsDto
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double Gpa { get; set; }
        public double TotalCreditsEarned { get; set; }
        public double TotalCurriculumCredits { get; set; } = 62.90;

        public string AcademicRating
        {
            get
            {
                if (Gpa >= 8.0) return "Giỏi";
                if (Gpa >= 7.0) return "Khá";
                if (Gpa >= 5.0) return "Trung bình";
                if (Gpa > 0) return "Yếu";
                return "Chưa có điểm";
            }
        }

        public string RatingColor => AcademicRating switch
        {
            "Giỏi" => "#93C5FD",
            "Khá" => "#86EFAC",
            "Trung bình" => "#FDE047",
            "Yếu" => "#FCA5A5",
            _ => "#B9B99E"
        };

        public string RatingBackground => AcademicRating switch
        {
            "Giỏi" => "#1E3048",
            "Khá" => "#183622",
            "Trung bình" => "#3D3014",
            "Yếu" => "#421818",
            _ => "#253628"
        };

        // Cảnh báo thiếu môn / đợt kiểm tra
        public bool HasMissingSubjects { get; set; }
        public int MissingSubjectsCount { get; set; }
        public string MissingSubjectsDisplay { get; set; } = string.Empty;

        // Dòng màu vàng cho học viên chưa làm bài
        public string RowBackground => HasMissingSubjects ? "#3D3414" : "Transparent";

        public string SummaryText => $"TBM: {Gpa:F2}  |  Xếp loại: {AcademicRating}  |  Đã tích lũy: {TotalCreditsEarned:F2}/{TotalCurriculumCredits:F2} TC  |  {(HasMissingSubjects ? $"⚠️ Thiếu {MissingSubjectsCount} nội dung" : "✅ Hoàn thành đầy đủ")}";

        public List<AcademicCadetSubjectDetailDto> SubjectDetails { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết từng môn / đợt thi của học viên trong bảng Master-Detail
    /// </summary>
    public class AcademicCadetSubjectDetailDto
    {
        public string SubjectName { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public double Credits { get; set; }
        public double? Score { get; set; }
        public string ScoreDisplay => Score.HasValue ? Score.Value.ToString("F2") : "--";

        public string Rating
        {
            get
            {
                if (!Score.HasValue) return "Chưa thi";
                if (Score.Value >= 8.5) return "Xuất sắc";
                if (Score.Value >= 8.0) return "Giỏi";
                if (Score.Value >= 7.0) return "Khá";
                if (Score.Value >= 5.0) return "Đạt (TB)";
                return "Không đạt";
            }
        }

        public string RatingColor => Rating switch
        {
            "Xuất sắc" => "#C084FC",
            "Giỏi" => "#93C5FD",
            "Khá" => "#86EFAC",
            "Đạt (TB)" => "#FDE047",
            "Không đạt" => "#FCA5A5",
            _ => "#B9B99E"
        };

        public bool IsWarning { get; set; }
        public string WarningMessage { get; set; } = string.Empty;
    }
}
