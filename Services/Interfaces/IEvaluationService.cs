using QL_HocVien.Models;

namespace QL_HocVien.Services.Interfaces
{
    public interface IEvaluationService
    {
        string EvaluateGrade(Subject subject, double score);
    }
}
