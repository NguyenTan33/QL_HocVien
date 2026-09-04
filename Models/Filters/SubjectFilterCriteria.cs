using System;

namespace QL_HocVien.Models.Filters
{
    public class SubjectFilterCriteria
    {
        public string? Keyword { get; set; }
        public string? SubjectCode { get; set; }
        public string? SubjectName { get; set; }
        public string? Category { get; set; } = "Tất cả";
        public string? Unit { get; set; } = "Tất cả";
        public bool? IsHigherBetter { get; set; }

        public bool HasAdvancedFilters()
        {
            return !string.IsNullOrWhiteSpace(SubjectCode) ||
                   (!string.IsNullOrWhiteSpace(Unit) && Unit != "Tất cả") ||
                   IsHigherBetter.HasValue;
        }

        public void Reset()
        {
            Keyword = string.Empty;
            SubjectCode = string.Empty;
            SubjectName = string.Empty;
            Category = "Tất cả";
            Unit = "Tất cả";
            IsHigherBetter = null;
        }
    }
}
