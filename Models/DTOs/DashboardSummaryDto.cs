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
        public int FairCount { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int TotalTestedSubjects { get; set; }

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

        public double PassRateOnly
        {
            get => TotalExamRecords > 0 ? Math.Round((double)PassCount / TotalExamRecords * 100, 1) : 0;
            set { }
        }

        public double OverallPassRate
        {
            get => TotalExamRecords > 0 ? Math.Round((double)(TotalExamRecords - FailCount) / TotalExamRecords * 100, 1) : 100;
            set { }
        }

        public double PassRate
        {
            get => OverallPassRate;
            set { }
        }

        public double FailRate
        {
            get => TotalExamRecords > 0 ? Math.Round((double)FailCount / TotalExamRecords * 100, 1) : 0;
            set { }
        }

        public double EliteRate
        {
            get => Math.Round(ExcellentRate + GoodRate, 1);
            set { }
        }

        public string OverallRatingLabel
        {
            get
            {
                if (TotalExamRecords == 0) return "Chưa có dữ liệu";
                if (OverallPassRate >= 100 && ExcellentRate >= 50) return "Đơn vị Đạt Xuất Sắc";
                if (OverallPassRate >= 95 && (ExcellentRate + GoodRate) >= 50) return "Đơn vị Đạt Chuẩn Giỏi";
                if (OverallPassRate >= 90 && (ExcellentRate + GoodRate + FairRate) >= 50) return "Đơn vị Đạt Chuẩn Khá";
                return "Đơn vị Đạt Mức Trung Bình";
            }
            set { }
        }

        public string OverallRatingColor
        {
            get
            {
                if (TotalExamRecords == 0) return "#64748B";
                if (OverallPassRate >= 100 && ExcellentRate >= 50) return "#7C3AED";
                if (OverallPassRate >= 95 && (ExcellentRate + GoodRate) >= 50) return "#2563EB";
                if (OverallPassRate >= 90 && (ExcellentRate + GoodRate + FairRate) >= 50) return "#16A34A";
                return "#D97706";
            }
            set { }
        }
    }
}
