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

        // Thuộc tính hiển thị UI theo chuẩn Celandar.png (Không lưu CSDL)
        public string DayOfWeekVietnamese => StartDate.DayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            DayOfWeek.Sunday => "Chủ Nhật",
            _ => string.Empty
        };

        public string WatermarkArtPath
        {
            get
            {
                if (Category == "Kiểm tra thể lực" || Title.Contains("thể lực", StringComparison.OrdinalIgnoreCase))
                    return "/Assets/Images/timeline_art_watchtower.png";
                if (Category == "Thi cử quân sự" || Title.Contains("bắn súng", StringComparison.OrdinalIgnoreCase))
                    return "/Assets/Images/timeline_art_ak_shooting.png";
                if (Category == "Tập luyện / Rèn luyện" || Title.Contains("hành quân", StringComparison.OrdinalIgnoreCase))
                    return "/Assets/Images/timeline_art_marching.png";
                return "/Assets/Images/timeline_art_watchtower.png";
            }
        }

        public string CategoryBg => Category switch
        {
            "Kiểm tra thể lực" => "#0C683B",
            "Thi cử quân sự" => "#0A6A45",
            "Tập luyện / Rèn luyện" => "#0C683B",
            "Hội thao / Sự kiện" => "#1D4ED8",
            _ => "#334155"
        };

        public string PriorityBg => Priority switch
        {
            "Khẩn cấp" => "#FEE2E2",
            "Cao" => "#FEE2E2",
            _ => "#E2E8F0"
        };

        public string PriorityFg => Priority switch
        {
            "Khẩn cấp" => "#DC2626",
            "Cao" => "#DC2626",
            _ => "#475569"
        };

        public string StatusBg => Status switch
        {
            "Đã hoàn thành" => "#DCFCE7",
            "Đang diễn ra" => "#DBEAFE",
            "Đang chuẩn bị" => "#DCFCE7",
            _ => "#F1F5F9"
        };

        public string StatusFg => Status switch
        {
            "Đã hoàn thành" => "#15803D",
            "Đang diễn ra" => "#1D4ED8",
            "Đang chuẩn bị" => "#15803D",
            _ => "#475569"
        };
    }
}
