using System;

namespace QL_HocVien.Models
{
    public class MilitaryRank
    {
        public int Id { get; set; }
        public string RankCode { get; set; } = string.Empty; // BN, BN1, HS, TS, ThS, CU, TU, TrU, ThgU, DU, ThTa, TrTa, ThgTa, DTa
        public string RankName { get; set; } = string.Empty; // Binh nhì, Binh nhất, Hạ sĩ, Trung sĩ...
        public string RankGroup { get; set; } = "Hạ sĩ quan - Binh sĩ"; // Hạ sĩ quan - Binh sĩ, Sĩ quan cấp Úy, Sĩ quan cấp Tá, Sĩ quan cấp Tướng
        public int DisplayOrder { get; set; } = 1;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
