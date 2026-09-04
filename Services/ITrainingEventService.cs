using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface ITrainingEventService
    {
        Task<IEnumerable<TrainingEvent>> GetAllEventsAsync();
        Task<IEnumerable<TrainingEvent>> GetFilteredEventsAsync(string? category, string? status, int? month, int? year);
        Task<TrainingEvent?> GetByIdAsync(int id);
        Task<(bool Success, string Message, TrainingEvent? Event)> CreateEventAsync(TrainingEvent evt);
        Task<(bool Success, string Message)> UpdateEventAsync(TrainingEvent evt);
        Task<(bool Success, string Message)> DeleteEventAsync(int id);
        Task<(bool Success, string Message)> ToggleCompleteAsync(int id);
    }
}
