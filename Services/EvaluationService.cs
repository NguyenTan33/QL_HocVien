using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public class EvaluationService : IEvaluationService
    {
        public string EvaluateGrade(Subject subject, double score)
        {
            if (subject.IsHigherBetter)
            {
                // Càng cao càng tốt (ví dụ: xà đơn, xà kép, nhảy xa, bơi)
                if (score >= subject.ExcellentThreshold + (subject.ExcellentThreshold * 0.1))
                    return "Xuất sắc";
                if (score >= subject.ExcellentThreshold)
                    return "Giỏi";
                if (score >= subject.GoodThreshold)
                    return "Khá";
                if (score >= subject.PassThreshold)
                    return "Đạt";
                return "Không đạt";
            }
            else
            {
                // Càng ít thời gian càng tốt (chạy 100m, 3000m, vượt vật cản)
                if (score <= subject.ExcellentThreshold * 0.95)
                    return "Xuất sắc";
                if (score <= subject.ExcellentThreshold)
                    return "Giỏi";
                if (score <= subject.GoodThreshold)
                    return "Khá";
                if (score <= subject.PassThreshold)
                    return "Đạt";
                return "Không đạt";
            }
        }
    }
}
