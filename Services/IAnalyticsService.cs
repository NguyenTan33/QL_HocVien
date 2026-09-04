using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services
{
    public interface IAnalyticsService
    {
        Task<List<string>> GetAvailableSessionsAsync();
        Task<ExamComparisonResultDto> CompareSessionsAsync(string baselineSession, string compareSession, string? unit = null, int? classId = null, string? keyword = null);
        Task<List<CadetTrendDto>> CompareCadetsAsync(string baselineSession, string compareSession, string? unit = null, int? classId = null, string? keyword = null, TrendDirection? trendFilter = null);
        Task<List<UnitComparisonDto>> CompareUnitsAsync(string baselineSession, string compareSession);
        Task<List<ClassComparisonDto>> CompareClassesAsync(string baselineSession, string compareSession, string? unit = null);
    }
}
