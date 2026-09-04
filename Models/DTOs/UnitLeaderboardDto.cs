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

        public double PassRate => TotalExamRecords > 0 ? Math.Round((double)PassedCount / TotalExamRecords * 100, 1) : 0;
        public double EliteRate => TotalExamRecords > 0 ? Math.Round((double)EliteCount / TotalExamRecords * 100, 1) : 0;

        public string RankMedal => Rank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => $"#{Rank}"
        };

        public string EvaluationStatus
        {
            get
            {
                if (PassRate >= 95 && EliteRate >= 45) return "Đơn vị Xuất sắc";
                if (PassRate >= 88) return "Đạt Chuẩn Tốt";
                if (PassRate >= 78) return "Đạt Yêu Cầu";
                return "Cần Chấn Chỉnh";
            }
        }

        public string StatusColor => PassRate switch
        {
            >= 90 => "#16A34A",
            >= 80 => "#2563EB",
            >= 70 => "#D97706",
            _ => "#DC2626"
        };

        public string StatusBackground => PassRate switch
        {
            >= 90 => "#DCFCE7",
            >= 80 => "#DBEAFE",
            >= 70 => "#FEF3C7",
            _ => "#FEE2E2"
        };
    }
}
