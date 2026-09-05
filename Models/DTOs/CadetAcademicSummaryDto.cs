using System;
using System.Collections.Generic;

namespace QL_HocVien.Models.DTOs
{
    public class CadetAcademicSummaryDto
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        // Điểm theo từng môn tín chỉ (Key = CreditSubjectId, Value = FinalScore)
        public Dictionary<int, double?> SubjectScores { get; set; } = new();

        public int TotalCreditsEarned { get; set; }
        public int TotalSubjectsCompleted { get; set; }
        public double Gpa { get; set; }

        public string AcademicRating
        {
            get
            {
                if (TotalSubjectsCompleted == 0) return "Chưa có điểm";
                if (Gpa >= 8.5) return "Xuất sắc";
                if (Gpa >= 8.0) return "Giỏi";
                if (Gpa >= 7.0) return "Khá";
                if (Gpa >= 5.0) return "Trung bình";
                return "Không đạt";
            }
            set { }
        }

        public string RatingColor
        {
            get => AcademicRating switch
            {
                "Xuất sắc" => "#7C3AED",
                "Giỏi" => "#2563EB",
                "Khá" => "#16A34A",
                "Trung bình" => "#D97706",
                "Không đạt" => "#DC2626",
                _ => "#64748B"
            };
            set { }
        }

        public string RatingBackground
        {
            get => AcademicRating switch
            {
                "Xuất sắc" => "#F5F3FF",
                "Giỏi" => "#DBEAFE",
                "Khá" => "#DCFCE7",
                "Trung bình" => "#FEF3C7",
                "Không đạt" => "#FEE2E2",
                _ => "#F1F5F9"
            };
            set { }
        }
    }
}
