using System;

namespace QL_HocVien.Models.DTOs
{
    public class SubjectPerformanceDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int TotalTested { get; set; }
        public int PassedCount { get; set; }
        public int EliteCount { get; set; }
        public int FailedCount { get; set; }

        public double PassRate
        {
            get => TotalTested > 0 ? Math.Round((double)PassedCount / TotalTested * 100, 1) : 0;
            set { }
        }

        public double FailRate
        {
            get => TotalTested > 0 ? Math.Round((double)FailedCount / TotalTested * 100, 1) : 0;
            set { }
        }

        public double EliteRate
        {
            get => TotalTested > 0 ? Math.Round((double)EliteCount / TotalTested * 100, 1) : 0;
            set { }
        }

        public string DifficultyLevel
        {
            get
            {
                if (FailRate >= 20) return "⚠️ Nội dung khó - Tỷ lệ trượt cao";
                if (FailRate >= 10) return "⚡ Mức độ trung bình";
                return "✅ Nắm vững - Tỷ lệ đạt tốt";
            }
            set { }
        }

        public string BarColor
        {
            get => PassRate switch
            {
                >= 90 => "#16A34A",
                >= 80 => "#0D9488",
                >= 70 => "#D97706",
                _ => "#DC2626"
            };
            set { }
        }
    }
}
