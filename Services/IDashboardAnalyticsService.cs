using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;
using QL_HocVien.Models.Filters;

namespace QL_HocVien.Services
{
    public interface IDashboardAnalyticsService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(DashboardFilterCriteria criteria);
        Task<List<PhysicalExamRecord>> GetFilteredRecordsAsync(DashboardFilterCriteria criteria);
        Task<List<UnitLeaderboardDto>> GetUnitLeaderboardAsync(DashboardFilterCriteria criteria);
        Task<List<SubjectPerformanceDto>> GetSubjectPerformancesAsync(DashboardFilterCriteria criteria);
        Task<List<CadetHonorDto>> GetHonoredCadetsAsync(DashboardFilterCriteria criteria, int topCount = 10);
        Task<List<PhysicalExamRecord>> GetFailedRecordsAsync(DashboardFilterCriteria criteria);
        Task<List<string>> GetAvailableUnitsAsync();
        Task<List<string>> GetAvailableClassesAsync(string? unit = null);
        Task<List<string>> GetAvailableSessionsAsync();
        Task<List<Subject>> GetAvailableSubjectsAsync();
    }
}
