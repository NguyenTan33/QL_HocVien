namespace QL_HocVien.Models.DTOs
{
    public class CadetHonorDto
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int TotalExams { get; set; }
        public int ExcellentExams { get; set; }
        public int GoodExams { get; set; }
        public string HonorTitle { get; set; } = "Chiến sĩ Khỏe Quân sự";
        public string BestSubject { get; set; } = string.Empty;
        public string BestScore { get; set; } = string.Empty;
    }
}
