using System;

namespace QL_HocVien.Models.DTOs
{
    public class UnitLeaderboardDto
    {
        public int Rank { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int TotalCadets { get; set; }
        public int TotalExamRecords { get; set; }
        public int PassedCount { get; set; }
        public int EliteCount { get; set; }
        public int FailedCount { get; set; }
        public int ExcellentCount { get; set; }
        public int GoodCount { get; set; }
        public int FairCount { get; set; }

        public double PassRate
        {
            get => TotalExamRecords > 0 ? Math.Round((double)PassedCount / TotalExamRecords * 100, 1) : 0;
            set { }
        }

        public double ExcellentRate
        {
            get => TotalExamRecords > 0 ? Math.Round((double)ExcellentCount / TotalExamRecords * 100, 1) : 0;
            set { }
        }

        public double GoodRate
        {
            get => TotalExamRecords > 0 ? Math.Round((double)GoodCount / TotalExamRecords * 100, 1) : 0;
            set { }
        }

        public double FairRate
        {
            get => TotalExamRecords > 0 ? Math.Round((double)FairCount / TotalExamRecords * 100, 1) : 0;
            set { }
        }

        public double EliteRate
        {
            get => TotalExamRecords > 0 ? Math.Round((double)EliteCount / TotalExamRecords * 100, 1) : 0;
            set { }
        }

        public string RankMedal
        {
            get => Rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{Rank}"
            };
            set { }
        }

        public string EvaluationStatus
        {
            get
            {
                // Quy tắc tính theo môn tín chỉ:
                // * đơn vị đạt xuất sắc: 100% đạt, XS >= 50%
                // * đơn vị giỏi: Đạt >= 95%, XS + G >= 50%
                // * đơn vị khá: Đạt >= 90%, XS + G + K >= 50%
                // * đơn vị trung bình: nếu k đủ điều kiện đạt khá
                if (PassRate >= 100 && ExcellentRate >= 50) return "Đơn vị Xuất sắc";
                if (PassRate >= 95 && (ExcellentRate + GoodRate) >= 50) return "Đơn vị Giỏi";
                if (PassRate >= 90 && (ExcellentRate + GoodRate + FairRate) >= 50) return "Đơn vị Khá";
                return "Đơn vị Trung bình";
            }
            set { }
        }

        public string StatusColor
        {
            get => EvaluationStatus switch
            {
                "Đơn vị Xuất sắc" => "#7C3AED",
                "Đơn vị Giỏi" => "#2563EB",
                "Đơn vị Khá" => "#16A34A",
                _ => "#D97706"
            };
            set { }
        }

        public string StatusBackground
        {
            get => EvaluationStatus switch
            {
                "Đơn vị Xuất sắc" => "#F5F3FF",
                "Đơn vị Giỏi" => "#DBEAFE",
                "Đơn vị Khá" => "#DCFCE7",
                _ => "#FEF3C7"
            };
            set { }
        }
    }
}
