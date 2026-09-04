using System;

namespace QL_HocVien.Models.Filters
{
    public class DashboardFilterCriteria
    {
        public string? Unit { get; set; } = "Tất cả";
        public string? ClassName { get; set; } = "Tất cả";
        public string? ExamSession { get; set; } = "Tất cả";
        public int? SubjectId { get; set; }
        public string? Grade { get; set; } = "Tất cả";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchKeyword { get; set; }

        public bool HasActiveFilters()
        {
            return (!string.IsNullOrWhiteSpace(Unit) && Unit != "Tất cả") ||
                   (!string.IsNullOrWhiteSpace(ClassName) && ClassName != "Tất cả") ||
                   (!string.IsNullOrWhiteSpace(ExamSession) && ExamSession != "Tất cả") ||
                   (SubjectId.HasValue && SubjectId.Value > 0) ||
                   (!string.IsNullOrWhiteSpace(Grade) && Grade != "Tất cả") ||
                   FromDate.HasValue ||
                   ToDate.HasValue ||
                   !string.IsNullOrWhiteSpace(SearchKeyword);
        }

        public void Reset()
        {
            Unit = "Tất cả";
            ClassName = "Tất cả";
            ExamSession = "Tất cả";
            SubjectId = null;
            Grade = "Tất cả";
            FromDate = null;
            ToDate = null;
            SearchKeyword = string.Empty;
        }
    }
}
