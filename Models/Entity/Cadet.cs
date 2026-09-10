using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.Entity
{
    public partial class Cadet : ObservableObject
    {
        public int Id { get; set; }
        public string CadetCode { get; set; } = string.Empty; // MÃ£ há»c viÃªn, vÃ­ dá»¥ HV26-001
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = "Binh nhÃ¬"; // Cáº¥p báº­c quÃ¢n Ä‘á»™i: Binh nhÃ¬, Binh nháº¥t, Háº¡ sÄ©, Trung sÄ©, ThÆ°á»£ng sÄ©, Thiáº¿u Ãºy...
        public string Position { get; set; } = "Há»c viÃªn"; // Chá»©c vá»¥: Há»c viÃªn, Tiá»ƒu Ä‘á»™i trÆ°á»Ÿng, Lá»›p phÃ³, Lá»›p trÆ°á»Ÿng...
        public string Unit { get; set; } = "Äáº¡i Ä‘á»™i 1"; // ÄÆ¡n vá»‹: Äáº¡i Ä‘á»™i 1, Trung Ä‘á»™i 1...
        public string ClassName { get; set; } = string.Empty; // TÃªn lá»›p
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; } = "Nam";
        
        // LiÃªn káº¿t tÃ i khoáº£n Ä‘Äƒng nháº­p (náº¿u cÃ³)
        public int? UserId { get; set; }
        public User? User { get; set; }

        // LiÃªn káº¿t lá»›p há»c (náº¿u cÃ³)
        public int? ClassId { get; set; }
        public MilitaryClass? MilitaryClass { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
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

