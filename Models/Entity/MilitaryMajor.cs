using System;

namespace QL_HocVien.Models.Entity
{
    public class MilitaryMajor
    {
        public int Id { get; set; }
        public string MajorCode { get; set; } = string.Empty; // CHTM, HCQS, KTQS, TSDN, TTLN, PB, TG
        public string MajorName { get; set; } = string.Empty; // Chá»‰ huy Tham mÆ°u Lá»¥c quÃ¢n, Háº­u cáº§n QuÃ¢n sá»±...
        public string TrainingDuration { get; set; } = "4 nÄƒm"; // 4 nÄƒm, 5 nÄƒm...
        public string Department { get; set; } = "Khoa Chiáº¿n thuáº­t"; // Khoa Ä‘Ã o táº¡o phá»¥ trÃ¡ch
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

