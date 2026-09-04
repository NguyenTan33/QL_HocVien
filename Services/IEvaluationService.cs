using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface IEvaluationService
    {
        string EvaluateGrade(Subject subject, double score);
    }
}
