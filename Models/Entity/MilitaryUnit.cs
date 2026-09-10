using System;

namespace QL_HocVien.Models.Entity
{
    public class MilitaryUnit
    {
        public int Id { get; set; }
        public string UnitCode { get; set; } = string.Empty; // c1, c2, c3, c4, d1, d2, e1, b1...
        public string UnitName { get; set; } = string.Empty; // Äáº¡i Ä‘á»™i 1, Äáº¡i Ä‘á»™i 2, Tiá»ƒu Ä‘oÃ n 1...
        public string ParentUnit { get; set; } = "Tiá»ƒu Ä‘oÃ n 1"; // ÄÆ¡n vá»‹ cáº¥p trÃªn trá»±c thuá»™c
        public string CommanderName { get; set; } = string.Empty; // Chá»‰ huy trÆ°á»Ÿng Ä‘Æ¡n vá»‹
        public string ContactPhone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

