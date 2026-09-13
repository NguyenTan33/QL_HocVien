using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.Entity
{
    /// <summary>
    /// Khóa học (Cohort) — ví dụ K75, K26, K27...
    /// Mỗi Khóa học chứa nhiều Tiểu đoàn (MilitaryUnit) trực thuộc.
    /// </summary>
    public partial class AcademicCohort : ObservableObject
    {
        public int Id { get; set; }

        /// <summary>Mã khóa học, ví dụ: K75, K26, K27. Unique.</summary>
        public string CohortCode { get; set; } = string.Empty;

        /// <summary>Tên đầy đủ khóa học, ví dụ: Khóa 75 Đại học Quân sự</summary>
        public string CohortName { get; set; } = string.Empty;

        /// <summary>Số khóa dạng số, ví dụ: 75 (lấy từ K75)</summary>
        public int CohortNumber { get; set; }

        /// <summary>Năm nhập học, ví dụ: 2023</summary>
        public int? EnrollmentYear { get; set; }

        /// <summary>Năm tốt nghiệp dự kiến, ví dụ: 2027</summary>
        public int? GraduationYear { get; set; }

        /// <summary>Niên khóa đào tạo, ví dụ: 2023 - 2027</summary>
        public string AcademicYear { get; set; } = string.Empty;

        /// <summary>Mô tả / Ghi chú về khóa học</summary>
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation: Một khóa học có nhiều học viên
        public ICollection<Cadet> Cadets { get; set; } = new List<Cadet>();

        // Navigation: Một khóa học liên kết với nhiều lớp học
        public ICollection<MilitaryClass> Classes { get; set; } = new List<MilitaryClass>();

        [NotMapped]
        public int CadetCount { get; set; }

        [NotMapped]
        public string DisplayLabel => $"{CohortCode} — {CohortName}";

        private bool _isSelected;

        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
