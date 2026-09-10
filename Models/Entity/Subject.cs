using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.Entity
{
    public partial class Subject : ObservableObject
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = string.Empty; // MÃ£ mÃ´n: XD, XK, C100, CV3000, BE, VVC91
        public string SubjectName { get; set; } = string.Empty; // TÃªn mÃ´n
        public string Category { get; set; } = "Sá»©c máº¡nh"; // Sá»©c nhanh, Sá»©c máº¡nh, Sá»©c bá»n, BÃ i táº­p tá»•ng há»£p, BÆ¡i tá»± do
        public string Unit { get; set; } = "láº§n"; // láº§n, giÃ¢y, phÃºt:giÃ¢y, mÃ©t
        public string Description { get; set; } = string.Empty;
        
        // TiÃªu chuáº©n rÃ¨n luyá»‡n theo ThÃ´ng tÆ° 32/2009/TTLT-BQP-BVHTTDL
        public double ExcellentThreshold { get; set; } // Má»©c Giá»i
        public double GoodThreshold { get; set; }      // Má»©c KhÃ¡
        public double PassThreshold { get; set; }      // Má»©c Äáº¡t
        public bool IsHigherBetter { get; set; } = true; // true: sá»‘ cÃ ng cao cÃ ng tá»‘t; false: sá»‘ cÃ ng tháº¥p cÃ ng tá»‘t (cháº¡y)
        
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

