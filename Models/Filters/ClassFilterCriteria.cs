using System;

namespace QL_HocVien.Models.Filters
{
    public class ClassFilterCriteria
    {
        public string? Keyword { get; set; }
        public string? Unit { get; set; } = "Tất cả";
        public string? Major { get; set; } = "Tất cả";
        public string? AcademicYear { get; set; } = "Tất cả";
        public bool? HasOfficerAssigned { get; set; }
        public int? MinCadets { get; set; }
        public int? MaxCadets { get; set; }

        public bool HasAdvancedFilters()
        {
            return (!string.IsNullOrWhiteSpace(AcademicYear) && AcademicYear != "Tất cả") ||
                   HasOfficerAssigned.HasValue ||
                   MinCadets.HasValue ||
                   MaxCadets.HasValue;
        }

        public void Reset()
        {
            Keyword = string.Empty;
            Unit = "Tất cả";
            Major = "Tất cả";
            AcademicYear = "Tất cả";
            HasOfficerAssigned = null;
            MinCadets = null;
            MaxCadets = null;
        }
    }
}
