using System;

namespace QL_HocVien.Models.Filters
{
    public class CatalogFilterCriteria
    {
        public string? Keyword { get; set; }
        public string? Group { get; set; } = "Tất cả";
        public string? ParentUnit { get; set; } = "Tất cả";
        public string? Department { get; set; } = "Tất cả";

        public bool HasAdvancedFilters()
        {
            return (!string.IsNullOrWhiteSpace(Group) && Group != "Tất cả") ||
                   (!string.IsNullOrWhiteSpace(ParentUnit) && ParentUnit != "Tất cả") ||
                   (!string.IsNullOrWhiteSpace(Department) && Department != "Tất cả");
        }

        public void Reset()
        {
            Keyword = string.Empty;
            Group = "Tất cả";
            ParentUnit = "Tất cả";
            Department = "Tất cả";
        }
    }
}
