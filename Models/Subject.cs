using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models
{
    public partial class Subject : ObservableObject
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = string.Empty; // Mã môn: XD, XK, C100, CV3000, BE, VVC91
        public string SubjectName { get; set; } = string.Empty; // Tên môn
        public string Category { get; set; } = "Sức mạnh"; // Sức nhanh, Sức mạnh, Sức bền, Bài tập tổng hợp, Bơi tự do
        public string Unit { get; set; } = "lần"; // lần, giây, phút:giây, mét
        public string Description { get; set; } = string.Empty;
        
        // Tiêu chuẩn rèn luyện theo Thông tư 32/2009/TTLT-BQP-BVHTTDL
        public double ExcellentThreshold { get; set; } // Mức Giỏi
        public double GoodThreshold { get; set; }      // Mức Khá
        public double PassThreshold { get; set; }      // Mức Đạt
        public bool IsHigherBetter { get; set; } = true; // true: số càng cao càng tốt; false: số càng thấp càng tốt (chạy)
        
        public ICollection<PhysicalExamRecord> ExamRecords { get; set; } = new List<PhysicalExamRecord>();

        private bool _isSelected;

        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
