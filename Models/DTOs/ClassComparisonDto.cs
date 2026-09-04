namespace QL_HocVien.Models.DTOs
{
    public class ClassComparisonDto
    {
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty; // Đại đội quản lý
        public string OfficerInCharge { get; set; } = string.Empty;
        public int TotalCadets { get; set; }

        public double BaselinePassRate { get; set; }
        public double ComparePassRate { get; set; }
        public double PassRateDelta => ComparePassRate - BaselinePassRate;

        public double BaselineExcellentRate { get; set; }
        public double CompareExcellentRate { get; set; }

        public int GrowthCadetsCount { get; set; }
        public int UnchangedCadetsCount { get; set; }
        public int RegressionCadetsCount { get; set; }

        public TrendDirection Trend => PassRateDelta switch
        {
            > 0.001 => TrendDirection.Growth,
            < -0.001 => TrendDirection.Regression,
            _ => (CompareExcellentRate > BaselineExcellentRate ? TrendDirection.Growth : 
                 (CompareExcellentRate < BaselineExcellentRate ? TrendDirection.Regression : TrendDirection.Unchanged))
        };

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

        public int RankInUnit { get; set; } // Thứ hạng thi đua trong Đại đội
    }
}
