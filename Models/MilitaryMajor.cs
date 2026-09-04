using System;

namespace QL_HocVien.Models
{
    public class MilitaryMajor
    {
        public int Id { get; set; }
        public string MajorCode { get; set; } = string.Empty; // CHTM, HCQS, KTQS, TSDN, TTLN, PB, TG
        public string MajorName { get; set; } = string.Empty; // Chỉ huy Tham mưu Lục quân, Hậu cần Quân sự...
        public string TrainingDuration { get; set; } = "4 năm"; // 4 năm, 5 năm...
        public string Department { get; set; } = "Khoa Chiến thuật"; // Khoa đào tạo phụ trách
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
