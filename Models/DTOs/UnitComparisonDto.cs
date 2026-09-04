namespace QL_HocVien.Models.DTOs
{
    public class UnitComparisonDto
    {
        public string UnitName { get; set; } = string.Empty; // Tên Đại đội / Đơn vị
        public int TotalCadets { get; set; } // Tổng quân số tham gia kiểm tra

        // Tỷ lệ Đạt yêu cầu
        public int BaselinePassCount { get; set; }
        public double BaselinePassRate { get; set; } // % Đạt đợt 1
        public int ComparePassCount { get; set; }
        public double ComparePassRate { get; set; } // % Đạt đợt 2
        public double PassRateDelta => ComparePassRate - BaselinePassRate; // Chênh lệch %

        // Tỷ lệ Khá - Giỏi
        public double BaselineExcellentRate { get; set; }
        public double CompareExcellentRate { get; set; }
        public double ExcellentRateDelta => CompareExcellentRate - BaselineExcellentRate;

        // Phân bổ xu hướng cá nhân trong đơn vị
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

        public string EvaluationComment { get; set; } = string.Empty;
    }
}
