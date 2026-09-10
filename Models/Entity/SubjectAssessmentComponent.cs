using System;
using System.Collections.Generic;

namespace QL_HocVien.Models.Entity
{
    /// <summary>
    /// Đại diện cho một đợt thi / đợt kiểm tra / cột điểm thành phần trực thuộc một Môn học chính
    /// Ví dụ môn CNTT có các đợt kiểm tra: CNTT1 (0.4 TC), CNTT2 (0.4 TC), CNTT (0.8 TC), Thi CNTT (2.4 TC)
    /// </summary>
    public class SubjectAssessmentComponent
    {
        public int Id { get; set; }

        public int CreditSubjectId { get; set; }
        public CreditSubject? CreditSubject { get; set; }

        /// <summary>
        /// Tên đợt kiểm tra / cột điểm (do người dùng đặt tùy ý, ví dụ: CNTT1, CNTT2, KTTX, Thi kết thúc...)
        /// </summary>
        public string ComponentName { get; set; } = string.Empty;

        /// <summary>
        /// Số tín chỉ của đợt kiểm tra này (ví dụ: 0.15, 0.4, 0.8, 1.2, 1.6, 2.4...)
        /// </summary>
        public double Credits { get; set; } = 1.0;

        /// <summary>
        /// Thứ tự hiển thị cột điểm (từ trái qua phải)
        /// </summary>
        public int OrderIndex { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<CreditScoreRecord> ScoreRecords { get; set; } = new List<CreditScoreRecord>();

        /// <summary>
        /// Chuỗi hiển thị tiêu đề cột (ví dụ: "CNTT1 (0.4 TC)")
        /// </summary>
        public string DisplayHeader => $"{ComponentName} ({Credits:F2} TC)";
    }
}
