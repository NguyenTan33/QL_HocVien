using System;
using System.Collections.Generic;

namespace QL_HocVien.Models.DTOs
{
    public class UntestedCadetDto
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        
        // Môn học / Nội dung còn thiếu chưa thi hoặc chưa kiểm tra
        public string MissingSubjects { get; set; } = string.Empty;
        public int MissingCount { get; set; }
        public string ExamType { get; set; } = "Môn Tín chỉ & Thể lực";
        public string Status { get; set; } = "Chưa hoàn thành";
        public string Note { get; set; } = "Cần sắp xếp kiểm tra bù";
    }
}
