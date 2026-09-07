using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services
{
    public interface ICreditSubjectService
    {
        Task<List<CreditSubject>> GetAllSubjectsAsync();
        Task<CreditSubject?> GetSubjectByIdAsync(int id);
        Task<(bool Success, string Message)> AddSubjectAsync(CreditSubject subject);
        Task<(bool Success, string Message)> UpdateSubjectAsync(CreditSubject subject);
        Task<(bool Success, string Message)> DeleteSubjectAsync(int id);

        Task<List<CreditScoreRecord>> GetAllScoresAsync();
        Task<List<CreditScoreRecord>> GetScoresByCadetIdAsync(int cadetId);
        Task<(bool Success, string Message)> SaveScoreAsync(CreditScoreRecord score);
        Task<(bool Success, string Message)> DeleteScoreAsync(int scoreId);

        Task<List<CadetAcademicSummaryDto>> GetCadetAcademicSummariesAsync(string? unit = null, string? className = null, string? keyword = null);
        Task<List<UntestedCadetDto>> GetUntestedCadetsAsync(string? unit = null, string? className = null, string? keyword = null);

        Task<(bool Success, string Message)> ExportAcademicReportAsync(string filePath, List<CadetAcademicSummaryDto> summaries, List<CreditSubject> subjects);
        Task<(bool Success, string Message, int ImportedCadets, int ImportedScores)> ImportStandardTbmExcelAsync(string filePath);
        Task<List<MajorSubjectBreakdownDto>> GetSubjectBreakdownForCadetAsync(int cadetId);
        Task<(bool Success, string Message)> SaveSubjectWithComponentsAsync(CreditSubject subject, IEnumerable<SubjectAssessmentComponent> components);
        Task<List<SubjectAssessmentComponent>> GetComponentsBySubjectIdAsync(int subjectId);
        Task<(CreditSubject? Subject, List<SubjectAssessmentComponent> Components, List<CadetSubjectGradeRowDto> Rows)> GetSubjectGradeMatrixAsync(int subjectId, string? unit = null, string? className = null);
        Task<(bool Success, string Message)> SaveSubjectGradeMatrixAsync(int subjectId, List<CadetSubjectGradeRowDto> rows);
        Task EnsureComponentsMigratedAsync();
        Task<List<CreditSubject>> GetMajorSubjectsAsync();
        Task ConsolidateMajorSubjectsAsync();
        Task<(CreditSubject? Subject, List<CadetSingleSubjectGradeDto> Components, double? CalculatedSubjectScore, bool HasMissingWarning)> GetCadetSubjectGradesAsync(int cadetId, int subjectId);
        Task<(bool Success, string Message)> SaveCadetSubjectGradesAsync(int cadetId, int subjectId, List<(int componentId, double? score)> componentScores);
    }
}
