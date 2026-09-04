using System;

namespace QL_HocVien.Models.Filters
{
    public class OfficerFilterCriteria
    {
        public string? Keyword { get; set; }
        public string? Rank { get; set; } = "Tất cả";
        public string? Position { get; set; } = "Tất cả";
        public string? Unit { get; set; } = "Tất cả";
        public string? Specialty { get; set; }
        public bool? HasAccount { get; set; }
        public bool? HasAssignedClasses { get; set; }

        public bool HasAdvancedFilters()
        {
            return !string.IsNullOrWhiteSpace(Specialty) ||
                   HasAccount.HasValue ||
                   HasAssignedClasses.HasValue;
        }

        public void Reset()
        {
            Keyword = string.Empty;
            Rank = "Tất cả";
            Position = "Tất cả";
            Unit = "Tất cả";
            Specialty = null;
            HasAccount = null;
            HasAssignedClasses = null;
        }
    }
}
