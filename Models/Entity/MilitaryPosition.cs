using System;

namespace QL_HocVien.Models.Entity
{
    public class MilitaryPosition
    {
        public int Id { get; set; }
        public string PositionCode { get; set; } = string.Empty; // HV, CS, TDT, LP, LT, CTP, CTT, BTV, GV, CBQL
        public string PositionName { get; set; } = string.Empty; // Há»c viÃªn, Chiáº¿n sÄ©, Tiá»ƒu Ä‘á»™i trÆ°á»Ÿng, Lá»›p phÃ³, Lá»›p trÆ°á»Ÿng...
        public string PositionGroup { get; set; } = "Há»c viÃªn";  // Há»c viÃªn / Chiáº¿n sÄ©, CÃ¡n bá»™ PhÃ¢n Ä‘á»™i, CÃ¡n bá»™ Chá»‰ huy, CÃ¡n bá»™ Giáº£ng dáº¡y
        public int DisplayOrder { get; set; } = 1;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

