using System;

namespace QL_HocVien.Models.Entity
{
    public class CreditScoreRecord
    {
        public int Id { get; set; }
        public int CadetId { get; set; }
        public Cadet? Cadet { get; set; }

        public int CreditSubjectId { get; set; }
        public CreditSubject? CreditSubject { get; set; }

        /// <summary>
        /// LiÃªn káº¿t Ä‘áº¿n Ä‘á»£t kiá»ƒm tra / Ä‘á»£t thi trá»±c thuá»™c (náº¿u cÃ³)
        /// </summary>
        public int? ComponentId { get; set; }
        public SubjectAssessmentComponent? Component { get; set; }

        // Äiá»ƒm thÆ°á»ng xuyÃªn (náº¿u cÃ³)
        public double? RegularScore { get; set; }

        // Äiá»ƒm thi (náº¿u cÃ³)
        public double? ExamScore { get; set; }

        // Äiá»ƒm tá»•ng káº¿t mÃ´n thang Ä‘iá»ƒm 10
        public double FinalScore { get; set; }

        // Äá»£t kiá»ƒm tra / Há»c ká»³
        public string ExamSession { get; set; } = "Há»c ká»³ 1";

        public DateTime ExamDate { get; set; } = DateTime.Today;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

