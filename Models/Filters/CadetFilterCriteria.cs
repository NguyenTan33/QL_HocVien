using System;

namespace QL_HocVien.Models.Filters
{
    public class CadetFilterCriteria
    {
        public string? Keyword { get; set; }
        public string? Rank { get; set; } = "Tất cả";
        public string? Unit { get; set; } = "Tất cả";
        public string? ClassName { get; set; } = "Tất cả";
        public string? Position { get; set; } = "Tất cả";
        public string? Gender { get; set; } = "Tất cả";
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public bool? HasAccount { get; set; }
        public string? FitnessGrade { get; set; } = "Tất cả";

        public bool HasAdvancedFilters()
        {
            return (!string.IsNullOrWhiteSpace(Position) && Position != "Tất cả") ||
                   (!string.IsNullOrWhiteSpace(Gender) && Gender != "Tất cả") ||
                   MinAge.HasValue ||
                   MaxAge.HasValue ||
                   HasAccount.HasValue ||
                   (!string.IsNullOrWhiteSpace(FitnessGrade) && FitnessGrade != "Tất cả");
        }

        public void Reset()
        {
            Keyword = string.Empty;
            Rank = "Tất cả";
            Unit = "Tất cả";
            ClassName = "Tất cả";
            Position = "Tất cả";
            Gender = "Tất cả";
            MinAge = null;
            MaxAge = null;
            HasAccount = null;
            FitnessGrade = "Tất cả";
        }
    }
}
