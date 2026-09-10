using System;
using System.Collections.Generic;

namespace QL_HocVien.Models.Entity
{
    /// <summary>
    /// Äáº¡i diá»‡n cho má»™t Ä‘á»£t thi / Ä‘á»£t kiá»ƒm tra / cá»™t Ä‘iá»ƒm thÃ nh pháº§n trá»±c thuá»™c má»™t MÃ´n há»c chÃ­nh
    /// VÃ­ dá»¥ mÃ´n CNTT cÃ³ cÃ¡c Ä‘á»£t kiá»ƒm tra: CNTT1 (0.4 TC), CNTT2 (0.4 TC), CNTT (0.8 TC), Thi CNTT (2.4 TC)
    /// </summary>
    public class SubjectAssessmentComponent
    {
        public int Id { get; set; }

        public int CreditSubjectId { get; set; }
        public CreditSubject? CreditSubject { get; set; }

        /// <summary>
        /// TÃªn Ä‘á»£t kiá»ƒm tra / cá»™t Ä‘iá»ƒm (do ngÆ°á»i dÃ¹ng Ä‘áº·t tÃ¹y Ã½, vÃ­ dá»¥: CNTT1, CNTT2, KTTX, Thi káº¿t thÃºc...)
        /// </summary>
        public string ComponentName { get; set; } = string.Empty;

        /// <summary>
        /// Sá»‘ tÃ­n chá»‰ cá»§a Ä‘á»£t kiá»ƒm tra nÃ y (vÃ­ dá»¥: 0.15, 0.4, 0.8, 1.2, 1.6, 2.4...)
        /// </summary>
        public double Credits { get; set; } = 1.0;

        /// <summary>
        /// Thá»© tá»± hiá»ƒn thá»‹ cá»™t Ä‘iá»ƒm (tá»« trÃ¡i qua pháº£i)
        /// </summary>
        public int OrderIndex { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<CreditScoreRecord> ScoreRecords { get; set; } = new List<CreditScoreRecord>();

        /// <summary>
        /// Chuá»—i hiá»ƒn thá»‹ tiÃªu Ä‘á» cá»™t (vÃ­ dá»¥: "CNTT1 (0.4 TC)")
        /// </summary>
        public string DisplayHeader => $"{ComponentName} ({Credits:F2} TC)";
    }
}

