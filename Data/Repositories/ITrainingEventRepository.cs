using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public interface ITrainingEventRepository : IRepository<TrainingEvent>
    {
        Task<IEnumerable<TrainingEvent>> GetFilteredEventsAsync(string? category, string? status, int? month, int? year);
        Task<IEnumerable<TrainingEvent>> GetUpcomingEventsAsync(int count);
    }
}
