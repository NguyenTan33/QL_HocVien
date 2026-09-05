using System;
using System.Collections.Generic;

namespace QL_HocVien.Models
{
    public class CreditSubject
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int Credits { get; set; } = 2; // Số tín chỉ (1, 2, 3...)
        
        // Hình thức đánh giá: "Kiểm tra thường xuyên" hoặc "Kiểm tra và thi"
        public string AssessmentType { get; set; } = "Kiểm tra và thi"; 
        
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<CreditScoreRecord> ScoreRecords { get; set; } = new List<CreditScoreRecord>();
    }
}
