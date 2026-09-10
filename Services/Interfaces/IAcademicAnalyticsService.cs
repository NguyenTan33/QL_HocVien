using System.Threading.Tasks;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services.Interfaces
{
    public interface IAcademicAnalyticsService
    {
        Task<AcademicAnalyticsResultDto> GetAcademicAnalyticsAsync(
            string? unit = null, 
            string? className = null, 
            string? rating = null, 
            string? status = null, 
            string? keyword = null);

        Task<(bool Success, string Message)> ExportAcademicAnalyticsToExcelAsync(
            AcademicAnalyticsResultDto result, 
            string filePath);
    }
}

