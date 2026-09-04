using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services
{
    public interface ITrainingRecommendationService
    {
        Task<TrainingRecommendationSummaryDto> GenerateRecommendationsAsync(
            IEnumerable<PhysicalExamRecord> filteredRecords,
            IEnumerable<Cadet> allCadets,
            string? unit = null);
    }
}
