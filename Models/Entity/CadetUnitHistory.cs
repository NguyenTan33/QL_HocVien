using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_HocVien.Models.Entity
{
    /// <summary>
    /// Thực thể ghi nhận Lịch sử biên chế, luân chuyển đơn vị và quá trình đào tạo theo từng năm học của học viên
    /// (SCD Type 2 / Temporal Unit Progression)
    /// </summary>
    [Table("CadetUnitHistories")]
    public class CadetUnitHistory
    {
        [Key]
        public int Id { get; set; }

        public int CadetId { get; set; }
        [ForeignKey(nameof(CadetId))]
        public virtual Cadet? Cadet { get; set; }

        /// <summary>Năm học diễn ra quá trình này (ví dụ: "2023 - 2024", "2024 - 2025")</summary>
        [Required]
        [MaxLength(30)]
        public string SchoolYear { get; set; } = string.Empty;

        /// <summary>Thứ tự năm học trong khóa: 1 (Năm 1), 2 (Năm 2), 3 (Năm 3), 4 (Năm 4)...</summary>
        public int YearOrder { get; set; } = 1;

        /// <summary>Khóa đào tạo tại thời điểm năm học này (ví dụ: "K75", "K76")</summary>
        [MaxLength(50)]
        public string Cohort { get; set; } = string.Empty;

        /// <summary>Đường dẫn mã đơn vị (ví dụ: "dBB1/cBB1/bBB1" hoặc "dBB1/cBB1/bBB2")</summary>
        [Required]
        [MaxLength(100)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>Tên đơn vị đầy đủ dạng văn bản (ví dụ: "Tiểu đội 1 ➔ Đại đội 1 ➔ Tiểu đoàn 1")</summary>
        [MaxLength(255)]
        public string UnitName { get; set; } = string.Empty;

        /// <summary>Lớp đào tạo nếu có (ví dụ: "CNTT-K75")</summary>
        [MaxLength(100)]
        public string ClassName { get; set; } = string.Empty;

        /// <summary>Trạng thái đào tạo trong năm: "Đang học", "Hoàn thành năm học", "Đã hoàn thành", "Bảo lưu", "Thôi học"</summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Đang học";

        /// <summary>Điểm trung bình môn (TBM / GPA) của năm học này (tự động tính hoặc cập nhật)</summary>
        public double? AnnualGpa { get; set; }

        /// <summary>Lý do điều chuyển hoặc ghi chú phân luồng</summary>
        [MaxLength(500)]
        public string? TransferReason { get; set; }

        /// <summary>Ngày quyết định hoặc ngày hiệu lực điều chuyển</summary>
        public DateTime? EffectiveDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
