using System;

namespace QL_HocVien.Models
{
    public class PhysicalExamRecord
    {
        public int Id { get; set; }
        public int CadetId { get; set; }
        public Cadet? Cadet { get; set; }
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }
        
        public DateTime ExamDate { get; set; } = DateTime.Today;
        public string ExamSession { get; set; } = string.Empty; // Ví dụ: "Kiểm tra Quý 3/2026", "Kiểm tra định kỳ"
        public double ScoreValue { get; set; } // Kết quả thực tế (ví dụ: 15 lần, 13.5 giây, 85 mét)
        public string Grade { get; set; } = "Chưa xếp loại"; // "Xuất sắc", "Giỏi", "Khá", "Đạt", "Không đạt"
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
