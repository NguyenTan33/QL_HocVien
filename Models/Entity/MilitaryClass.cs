using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.Entity
{
    public partial class MilitaryClass : ObservableObject
    {
        public int Id { get; set; }
        public string ClassCode { get; set; } = string.Empty; // MÃ£ lá»›p: K26A, K26B, CHTM01...
        public string ClassName { get; set; } = string.Empty; // TÃªn lá»›p: K26A - Chá»‰ huy Tham mÆ°u
        public string Unit { get; set; } = "Äáº¡i Ä‘á»™i 1";       // ÄÆ¡n vá»‹ quáº£n lÃ½: Äáº¡i Ä‘á»™i 1, Äáº¡i Ä‘á»™i 2...
        public string Major { get; set; } = "Chá»‰ huy Tham mÆ°u"; // ChuyÃªn ngÃ nh Ä‘Ã o táº¡o
        public string OfficerInCharge { get; set; } = string.Empty; // CÃ¡n bá»™ chá»§ nhiá»‡m / Quáº£n lÃ½ lá»›p
        public int? OfficerId { get; set; }                          // KhÃ³a ngoáº¡i liÃªn káº¿t Officer (náº¿u cÃ³)
        public Officer? Officer { get; set; }

        public string AcademicYear { get; set; } = "2023 - 2027";   // NiÃªn khÃ³a / KhÃ³a há»c
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan há»‡ 1-N: Má»™t lá»›p há»c cÃ³ nhiá»u há»c viÃªn
        public ICollection<Cadet> Cadets { get; set; } = new List<Cadet>();

        private bool _isSelected;

        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}

