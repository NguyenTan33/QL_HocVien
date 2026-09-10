using System;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.Entity
{
    public partial class PhysicalExamRecord : ObservableObject
    {
        public int Id { get; set; }
        public int CadetId { get; set; }
        public Cadet? Cadet { get; set; }
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }
        
        public DateTime ExamDate { get; set; } = DateTime.Today;
        public string ExamSession { get; set; } = string.Empty; // VÃ­ dá»¥: "Kiá»ƒm tra QuÃ½ 3/2026", "Kiá»ƒm tra Ä‘á»‹nh ká»³"
        public double ScoreValue { get; set; } // Káº¿t quáº£ thá»±c táº¿ (vÃ­ dá»¥: 15 láº§n, 13.5 giÃ¢y, 85 mÃ©t)
        public string Grade { get; set; } = "ChÆ°a xáº¿p loáº¡i"; // "Xuáº¥t sáº¯c", "Giá»i", "KhÃ¡", "Äáº¡t", "KhÃ´ng Ä‘áº¡t"
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        private bool _isSelected;

        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}

