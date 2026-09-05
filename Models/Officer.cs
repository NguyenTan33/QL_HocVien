using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models
{
    public partial class Officer : ObservableObject
    {
        public int Id { get; set; }
        public string OfficerCode { get; set; } = string.Empty; // Mã cán bộ: CB-001, CB-002...
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = "Đại úy";           // Cấp bậc: Thiếu úy, Trung úy, Thượng úy, Đại úy, Thiếu tá...
        public string Position { get; set; } = "Chính trị viên"; // Chức vụ: Đại đội trưởng, Chính trị viên, Cán bộ chủ nhiệm...
        public string Unit { get; set; } = "Đại đội 1";        // Đơn vị công tác
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialty { get; set; } = "Quản lý & Huấn luyện học viên"; // Nhiệm vụ / Chuyên môn
        public DateTime? DateOfBirth { get; set; }
        public DateTime? EnlistmentDate { get; set; }           // Ngày nhập ngũ
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Tài khoản đăng nhập hệ thống liên kết (nếu có)
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Danh sách các lớp học được phân công phụ trách
        public ICollection<MilitaryClass> ManagedClasses { get; set; } = new List<MilitaryClass>();

        private bool _isSelected;

        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
