using System.Collections.Generic;

namespace QL_HocVien.Models.DTOs
{
    public enum TrendDirection
    {
        Growth,      // Tăng trưởng (▲)
        Unchanged,   // Giữ nguyên (—)
        Regression   // Thụt lùi (▼)
    }

    public class SubjectTrendItemDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public bool IsHigherBetter { get; set; } = true;

        public double BaselineScore { get; set; }
        public double CompareScore { get; set; }
        public double ScoreDelta { get; set; }

        public string BaselineGrade { get; set; } = string.Empty;
        public string CompareGrade { get; set; } = string.Empty;

        public TrendDirection Trend { get; set; } = TrendDirection.Unchanged;
        public string TrendText => Trend switch
        {
            TrendDirection.Growth => "Tăng trưởng",
            TrendDirection.Regression => "Thụt lùi",
            _ => "Giữ nguyên"
        };
        public string TrendSymbol => Trend switch
        {
            TrendDirection.Growth => "▲",
            TrendDirection.Regression => "▼",
            _ => "—"
        };
        public string TrendColor => Trend switch
        {
            TrendDirection.Growth => "#16A34A",
            TrendDirection.Regression => "#DC2626",
            _ => "#F59E0B"
        };

        public string DetailDescription { get; set; } = string.Empty;
    }

    public class CadetTrendDto
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty; // Đại đội 1, Đại đội 2...
        public string ClassName { get; set; } = string.Empty; // Lớp

        public List<SubjectTrendItemDto> SubjectTrends { get; set; } = new List<SubjectTrendItemDto>();

        public int GrowthCount { get; set; }
        public int UnchangedCount { get; set; }
        public int RegressionCount { get; set; }

        public string OverallBaselineGrade { get; set; } = "Chưa xếp loại";
        public string OverallCompareGrade { get; set; } = "Chưa xếp loại";

        public TrendDirection OverallTrend { get; set; } = TrendDirection.Unchanged;
        public string OverallTrendText => OverallTrend switch
        {
            TrendDirection.Growth => "Tăng trưởng",
            TrendDirection.Regression => "Thụt lùi",
            _ => "Giữ nguyên"
        };
        public string OverallTrendSymbol => OverallTrend switch
        {
            TrendDirection.Growth => "▲",
            TrendDirection.Regression => "▼",
            _ => "—"
        };
        public string OverallTrendColor => OverallTrend switch
        {
            TrendDirection.Growth => "#16A34A",
            TrendDirection.Regression => "#DC2626",
            _ => "#F59E0B"
        };

        public string SummaryText { get; set; } = string.Empty;
    }
}
