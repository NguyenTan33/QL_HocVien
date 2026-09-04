using System;

namespace QL_HocVien.Models.DTOs
{
    public class DashboardSummaryDto
    {
        public int TotalCadets { get; set; }
        public int TotalUnitsCount { get; set; }
        public int TotalClassesCount { get; set; }
        public int TotalExamRecords { get; set; }
        public int UniqueTestedCadets { get; set; }

        public int ExcellentCount { get; set; }
        public int GoodCount { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }

        public double ExcellentRate => TotalExamRecords > 0 ? Math.Round((double)ExcellentCount / TotalExamRecords * 100, 1) : 0;
        public double GoodRate => TotalExamRecords > 0 ? Math.Round((double)GoodCount / TotalExamRecords * 100, 1) : 0;
        public double PassRateOnly => TotalExamRecords > 0 ? Math.Round((double)PassCount / TotalExamRecords * 100, 1) : 0;
        public double OverallPassRate => TotalExamRecords > 0 ? Math.Round((double)(TotalExamRecords - FailCount) / TotalExamRecords * 100, 1) : 100;
        public double FailRate => TotalExamRecords > 0 ? Math.Round((double)FailCount / TotalExamRecords * 100, 1) : 0;
        public double EliteRate => Math.Round(ExcellentRate + GoodRate, 1);

        public string OverallRatingLabel
        {
            get
            {
                if (TotalExamRecords == 0) return "Chưa có dữ liệu";
                if (OverallPassRate >= 95 && EliteRate >= 50) return "Đơn vị Rèn luyện Xuất sắc";
                if (OverallPassRate >= 90) return "Đơn vị Đạt Chuẩn Giỏi";
                if (OverallPassRate >= 80) return "Đơn vị Đạt Yêu Cầu";
                return "Cần Tăng Cường Huấn Luyện";
            }
        }

        public string OverallRatingColor
        {
            get
            {
                if (TotalExamRecords == 0) return "#64748B";
                if (OverallPassRate >= 90) return "#16A34A";
                if (OverallPassRate >= 80) return "#D97706";
                return "#DC2626";
            }
        }
    }
}
