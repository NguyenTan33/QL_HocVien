using System;

namespace QL_HocVien.Models
{
    public class MilitaryUnit
    {
        public int Id { get; set; }
        public string UnitCode { get; set; } = string.Empty; // c1, c2, c3, c4, d1, d2, e1, b1...
        public string UnitName { get; set; } = string.Empty; // Đại đội 1, Đại đội 2, Tiểu đoàn 1...
        public string ParentUnit { get; set; } = "Tiểu đoàn 1"; // Đơn vị cấp trên trực thuộc
        public string CommanderName { get; set; } = string.Empty; // Chỉ huy trưởng đơn vị
        public string ContactPhone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
