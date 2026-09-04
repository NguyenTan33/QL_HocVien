using System.Collections.Generic;

namespace QL_HocVien.Models.DTOs
{
    public class ExamComparisonResultDto
    {
        public string BaselineSession { get; set; } = string.Empty;
        public string CompareSession { get; set; } = string.Empty;

        // Thống kê tổng quan toàn quân số
        public int TotalEvaluatedCadets { get; set; }
        public int OverallGrowthCount { get; set; }
        public int OverallUnchangedCount { get; set; }
        public int OverallRegressionCount { get; set; }

        public double OverallGrowthPercentage => TotalEvaluatedCadets > 0 
            ? (double)OverallGrowthCount / TotalEvaluatedCadets * 100 : 0;
        public double OverallUnchangedPercentage => TotalEvaluatedCadets > 0 
            ? (double)OverallUnchangedCount / TotalEvaluatedCadets * 100 : 0;
        public double OverallRegressionPercentage => TotalEvaluatedCadets > 0 
            ? (double)OverallRegressionCount / TotalEvaluatedCadets * 100 : 0;

        public double BaselinePassRate { get; set; }
        public double ComparePassRate { get; set; }
        public double PassRateDelta => ComparePassRate - BaselinePassRate;

        // Dữ liệu phân cấp
        public List<UnitComparisonDto> UnitComparisons { get; set; } = new List<UnitComparisonDto>();
        public List<ClassComparisonDto> ClassComparisons { get; set; } = new List<ClassComparisonDto>();
        public List<CadetTrendDto> CadetTrends { get; set; } = new List<CadetTrendDto>();
    }
}
