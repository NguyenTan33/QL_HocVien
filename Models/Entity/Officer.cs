using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.Entity
{
    public partial class Officer : ObservableObject
    {
        public int Id { get; set; }
        public string OfficerCode { get; set; } = string.Empty; // MÃ£ cÃ¡n bá»™: CB-001, CB-002...
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = "Äáº¡i Ãºy";           // Cáº¥p báº­c: Thiáº¿u Ãºy, Trung Ãºy, ThÆ°á»£ng Ãºy, Äáº¡i Ãºy, Thiáº¿u tÃ¡...
        public string Position { get; set; } = "ChÃ­nh trá»‹ viÃªn"; // Chá»©c vá»¥: Äáº¡i Ä‘á»™i trÆ°á»Ÿng, ChÃ­nh trá»‹ viÃªn, CÃ¡n bá»™ chá»§ nhiá»‡m...
        public string Unit { get; set; } = "Äáº¡i Ä‘á»™i 1";        // ÄÆ¡n vá»‹ cÃ´ng tÃ¡c
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialty { get; set; } = "Quáº£n lÃ½ & Huáº¥n luyá»‡n há»c viÃªn"; // Nhiá»‡m vá»¥ / ChuyÃªn mÃ´n
        public DateTime? DateOfBirth { get; set; }
        public DateTime? EnlistmentDate { get; set; }           // NgÃ y nháº­p ngÅ©
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // TÃ i khoáº£n Ä‘Äƒng nháº­p há»‡ thá»‘ng liÃªn káº¿t (náº¿u cÃ³)
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Danh sÃ¡ch cÃ¡c lá»›p há»c Ä‘Æ°á»£c phÃ¢n cÃ´ng phá»¥ trÃ¡ch
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

