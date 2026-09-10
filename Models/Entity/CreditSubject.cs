using System;
using System.Collections.Generic;

namespace QL_HocVien.Models.Entity
{
    public class CreditSubject
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public double Credits { get; set; } = 2.0; // Sá»‘ tÃ­n chá»‰ (há»— trá»£ sá»‘ thá»±c: 0.15, 0.4, 0.8, 1.6, 2.4...)
        
        // HÃ¬nh thá»©c Ä‘Ã¡nh giÃ¡: "Kiá»ƒm tra thÆ°á»ng xuyÃªn" hoáº·c "Kiá»ƒm tra vÃ  thi"
        public string AssessmentType { get; set; } = "Kiá»ƒm tra vÃ  thi"; 
        
        // NhÃ³m mÃ´n há»c lá»›n (vÃ­ dá»¥ "CNTT" cho CNTT1, CNTT2, CNTT, Thi CNTT)
        public string? SubjectGroup { get; set; } = string.Empty;

        // ÄÃ¡nh dáº¥u Ä‘Ã¢y lÃ  mÃ´n thÃ nh pháº§n con hay mÃ´n Ä‘á»™c láº­p
        public bool IsComponent { get; set; } = false;
        
        public string? Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<CreditScoreRecord> ScoreRecords { get; set; } = new List<CreditScoreRecord>();

        /// <summary>
        /// Danh sÃ¡ch cÃ¡c Ä‘á»£t kiá»ƒm tra / Ä‘á»£t thi trá»±c thuá»™c mÃ´n há»c chÃ­nh nÃ y
        /// </summary>
        public ICollection<SubjectAssessmentComponent> Components { get; set; } = new List<SubjectAssessmentComponent>();

        // TÃªn mÃ´n lá»›n hiá»ƒn thá»‹
        public string DisplayGroupName => string.IsNullOrWhiteSpace(SubjectGroup) ? SubjectName : SubjectGroup;

        /// <summary>
        /// Sá»‘ Ä‘á»£t kiá»ƒm tra / cá»™t Ä‘iá»ƒm
        /// </summary>
        public int ComponentCount => Components?.Count ?? 0;

        /// <summary>
        /// Tá»•ng sá»‘ tÃ­n chá»‰ tá»± Ä‘á»™ng tÃ­nh tá»« cÃ¡c Ä‘á»£t kiá»ƒm tra trá»±c thuá»™c (hoáº·c giÃ¡ trá»‹ Credits gá»‘c náº¿u chÆ°a cÃ³ Ä‘á»£t kiá»ƒm tra)
        /// </summary>
        public double CalculatedTotalCredits
        {
            get
            {
                if (Components != null && Components.Count > 0)
                    return Math.Round(System.Linq.Enumerable.Sum(Components, c => c.Credits), 2);
                return Credits;
            }
        }
    }
}

