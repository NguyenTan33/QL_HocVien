using System;
using System.Collections.Generic;

namespace QL_HocVien.Models
{
    public class Cadet
    {
        public int Id { get; set; }
        public string CadetCode { get; set; } = string.Empty; // Mã học viên, ví dụ HV26-001
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = "Binh nhì"; // Cấp bậc quân đội: Binh nhì, Binh nhất, Hạ sĩ, Trung sĩ, Thượng sĩ, Thiếu úy...
        public string Position { get; set; } = "Học viên"; // Chức vụ: Học viên, Tiểu đội trưởng, Lớp phó, Lớp trưởng...
        public string Unit { get; set; } = "Đại đội 1"; // Đơn vị: Đại đội 1, Trung đội 1...
        public string ClassName { get; set; } = string.Empty; // Tên lớp
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; } = "Nam";
        
        // Liên kết tài khoản đăng nhập (nếu có)
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Liên kết lớp học (nếu có)
        public int? ClassId { get; set; }
        public MilitaryClass? MilitaryClass { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<PhysicalExamRecord> ExamRecords { get; set; } = new List<PhysicalExamRecord>();
    }
}
