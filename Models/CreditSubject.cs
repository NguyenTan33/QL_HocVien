using System;
using System.Collections.Generic;

namespace QL_HocVien.Models
{
    public class CreditSubject
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public double Credits { get; set; } = 2.0; // Số tín chỉ (hỗ trợ số thực: 0.15, 0.4, 0.8, 1.6, 2.4...)
        
        // Hình thức đánh giá: "Kiểm tra thường xuyên" hoặc "Kiểm tra và thi"
        public string AssessmentType { get; set; } = "Kiểm tra và thi"; 
        
        // Nhóm môn học lớn (ví dụ "CNTT" cho CNTT1, CNTT2, CNTT, Thi CNTT)
        public string? SubjectGroup { get; set; } = string.Empty;

        // Đánh dấu đây là môn thành phần con hay môn độc lập
        public bool IsComponent { get; set; } = false;
        
        public string? Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<CreditScoreRecord> ScoreRecords { get; set; } = new List<CreditScoreRecord>();

        /// <summary>
        /// Danh sách các đợt kiểm tra / đợt thi trực thuộc môn học chính này
        /// </summary>
        public ICollection<SubjectAssessmentComponent> Components { get; set; } = new List<SubjectAssessmentComponent>();

        // Tên môn lớn hiển thị
        public string DisplayGroupName => string.IsNullOrWhiteSpace(SubjectGroup) ? SubjectName : SubjectGroup;

        /// <summary>
        /// Số đợt kiểm tra / cột điểm
        /// </summary>
        public int ComponentCount => Components?.Count ?? 0;

        /// <summary>
        /// Tổng số tín chỉ tự động tính từ các đợt kiểm tra trực thuộc (hoặc giá trị Credits gốc nếu chưa có đợt kiểm tra)
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
