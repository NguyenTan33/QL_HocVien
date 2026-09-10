using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
{
    public class EvaluationService : IEvaluationService
    {
        public string EvaluateGrade(Subject subject, double score)
        {
            if (subject.IsHigherBetter)
            {
                // CÃ ng cao cÃ ng tá»‘t (vÃ­ dá»¥: xÃ  Ä‘Æ¡n, xÃ  kÃ©p, nháº£y xa, bÆ¡i)
                if (score >= subject.ExcellentThreshold + (subject.ExcellentThreshold * 0.1))
                    return "Xuáº¥t sáº¯c";
                if (score >= subject.ExcellentThreshold)
                    return "Giá»i";
                if (score >= subject.GoodThreshold)
                    return "KhÃ¡";
                if (score >= subject.PassThreshold)
                    return "Äáº¡t";
                return "KhÃ´ng Ä‘áº¡t";
            }
            else
            {
                // CÃ ng Ã­t thá»i gian cÃ ng tá»‘t (cháº¡y 100m, 3000m, vÆ°á»£t váº­t cáº£n)
                if (score <= subject.ExcellentThreshold * 0.95)
                    return "Xuáº¥t sáº¯c";
                if (score <= subject.ExcellentThreshold)
                    return "Giá»i";
                if (score <= subject.GoodThreshold)
                    return "KhÃ¡";
                if (score <= subject.PassThreshold)
                    return "Äáº¡t";
                return "KhÃ´ng Ä‘áº¡t";
            }
        }
    }
}

