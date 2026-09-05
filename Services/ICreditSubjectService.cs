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
    }
}
