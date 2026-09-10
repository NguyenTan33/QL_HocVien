using System;

namespace QL_HocVien.Models.DTOs
{
    /// <summary>
    /// Thông tin học viên trong diện cảnh báo học vụ, nợ tín chỉ, cần bồi dưỡng / thi lại
    /// </summary>
    public class AcademicWarningCadetDto
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double Gpa { get; set; }
        public double TotalCreditsEarned { get; set; }

        public int MissingSubjectsCount { get; set; }
        public string MissingSubjectsDisplay { get; set; } = string.Empty;
        public int FailedSubjectsCount { get; set; }
        public string FailedSubjectsDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Lý do cảnh báo (ví dụ: GPA < 5.0, Nợ 2 học phần, Chưa tham gia sát hạch)
        /// </summary>
        public string WarningReason { get; set; } = string.Empty;

        /// <summary>
        /// Kế hoạch / Biện pháp xử lý học vụ
        /// </summary>
        public string ActionPlan { get; set; } = "Bố trí phụ đạo chuyên đề, đăng ký kiểm tra / thi lại";

        /// <summary>
        /// Mức độ cảnh báo: Cấp 1, Cấp 2, Nguy cơ buộc thôi học
        /// </summary>
        public string WarningSeverity => (Gpa > 0 && Gpa < 4.0) || FailedSubjectsCount >= 3 || MissingSubjectsCount >= 3
            ? "⚠️ Cảnh báo mức độ 2 (Nguy cơ học lại)"
            : "⚡ Cảnh báo mức độ 1 (Cần phụ đạo)";

        public string SeverityColor => WarningSeverity.Contains("mức độ 2") ? "#DC2626" : "#D97706";
    }
}
