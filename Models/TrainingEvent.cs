using System;

namespace QL_HocVien.Models
{
    public class TrainingEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // Tiêu đề sự kiện
        public string Category { get; set; } = "Kiểm tra thể lực"; // "Kiểm tra thể lực", "Thi cử quân sự", "Tập luyện / Rèn luyện", "Hội thao / Sự kiện"
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
        public string TargetUnit { get; set; } = "Toàn đơn vị"; // Đơn vị/Lớp áp dụng (Đại đội 1, Toàn đơn vị, K26A...)
        public string Location { get; set; } = string.Empty; // Thao trường, Bãi tập xà, Sân vận động, Bể bơi...
        public string Priority { get; set; } = "Bình thường"; // "Khẩn cấp", "Cao", "Bình thường"
        public string Status { get; set; } = "Đang chuẩn bị"; // "Đang chuẩn bị", "Đang diễn ra", "Đã hoàn thành", "Tạm hoãn"
        public string Description { get; set; } = string.Empty; // Nội dung chỉ thị, ghi chú chi tiết
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
