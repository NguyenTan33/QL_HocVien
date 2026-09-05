using System;

namespace QL_HocVien.Models
{
    public class CreditScoreRecord
    {
        public int Id { get; set; }
        public int CadetId { get; set; }
        public Cadet? Cadet { get; set; }

        public int CreditSubjectId { get; set; }
        public CreditSubject? CreditSubject { get; set; }

        // Điểm thường xuyên (nếu có)
        public double? RegularScore { get; set; }

        // Điểm thi (nếu có)
        public double? ExamScore { get; set; }

        // Điểm tổng kết môn thang điểm 10
        public double FinalScore { get; set; }

        // Đợt kiểm tra / Học kỳ
        public string ExamSession { get; set; } = "Học kỳ 1";

        public DateTime ExamDate { get; set; } = DateTime.Today;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
