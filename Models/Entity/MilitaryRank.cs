using System;

namespace QL_HocVien.Models.Entity
{
    public class MilitaryRank
    {
        public int Id { get; set; }
        public string RankCode { get; set; } = string.Empty; // BN, BN1, HS, TS, ThS, CU, TU, TrU, ThgU, DU, ThTa, TrTa, ThgTa, DTa
        public string RankName { get; set; } = string.Empty; // Binh nhÃ¬, Binh nháº¥t, Háº¡ sÄ©, Trung sÄ©...
        public string RankGroup { get; set; } = "Háº¡ sÄ© quan - Binh sÄ©"; // Háº¡ sÄ© quan - Binh sÄ©, SÄ© quan cáº¥p Ãšy, SÄ© quan cáº¥p TÃ¡, SÄ© quan cáº¥p TÆ°á»›ng
        public int DisplayOrder { get; set; } = 1;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

