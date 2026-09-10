using System;

namespace QL_HocVien.Models
{
    public class MilitaryPosition
    {
        public int Id { get; set; }
        public string PositionCode { get; set; } = string.Empty; // HV, CS, TDT, LP, LT, CTP, CTT, BTV, GV, CBQL
        public string PositionName { get; set; } = string.Empty; // Học viên, Chiến sĩ, Tiểu đội trưởng, Lớp phó, Lớp trưởng...
        public string PositionGroup { get; set; } = "Học viên";  // Học viên / Chiến sĩ, Cán bộ Phân đội, Cán bộ Chỉ huy, Cán bộ Giảng dạy
        public int DisplayOrder { get; set; } = 1;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
