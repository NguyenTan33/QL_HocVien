using System;

namespace QL_HocVien.Models.Filters
{
    public class PhysicalExamFilterCriteria
    {
        public string? CadetKeyword { get; set; }
        public int? SubjectId { get; set; }
        public string? Grade { get; set; } = "Tất cả";
        public string? ExamSession { get; set; } = "Tất cả";
        public string? Unit { get; set; } = "Tất cả";
        public string? ClassName { get; set; } = "Tất cả";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool HasAdvancedFilters()
        {
            return (SubjectId.HasValue && SubjectId.Value > 0) ||
                   (!string.IsNullOrWhiteSpace(ExamSession) && ExamSession != "Tất cả") ||
                   (!string.IsNullOrWhiteSpace(Unit) && Unit != "Tất cả") ||
                   (!string.IsNullOrWhiteSpace(ClassName) && ClassName != "Tất cả") ||
                   FromDate.HasValue ||
                   ToDate.HasValue;
        }

        public void Reset()
        {
            CadetKeyword = string.Empty;
            SubjectId = null;
            Grade = "Tất cả";
            ExamSession = "Tất cả";
            Unit = "Tất cả";
            ClassName = "Tất cả";
            FromDate = null;
            ToDate = null;
        }
    }
}
