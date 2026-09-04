using System;
using System.Collections.Generic;

namespace QL_HocVien.Models
{
    public class MilitaryClass
    {
        public int Id { get; set; }
        public string ClassCode { get; set; } = string.Empty; // Mã lớp: K26A, K26B, CHTM01...
        public string ClassName { get; set; } = string.Empty; // Tên lớp: K26A - Chỉ huy Tham mưu
        public string Unit { get; set; } = "Đại đội 1";       // Đơn vị quản lý: Đại đội 1, Đại đội 2...
        public string Major { get; set; } = "Chỉ huy Tham mưu"; // Chuyên ngành đào tạo
        public string OfficerInCharge { get; set; } = string.Empty; // Cán bộ chủ nhiệm / Quản lý lớp
        public string AcademicYear { get; set; } = "2023 - 2027";   // Niên khóa / Khóa học
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ 1-N: Một lớp học có nhiều học viên
        public ICollection<Cadet> Cadets { get; set; } = new List<Cadet>();
    }
}
