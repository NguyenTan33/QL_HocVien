using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
{
    public class TrainingEventService : ITrainingEventService
    {
        private readonly ITrainingEventRepository _eventRepository;

        public TrainingEventService(ITrainingEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<IEnumerable<TrainingEvent>> GetAllEventsAsync()
        {
            return await _eventRepository.GetAllAsync();
        }

        public async Task<IEnumerable<TrainingEvent>> GetFilteredEventsAsync(string? category, string? status, int? month, int? year)
        {
            return await _eventRepository.GetFilteredEventsAsync(category, status, month, year);
        }

        public async Task<TrainingEvent?> GetByIdAsync(int id)
        {
            return await _eventRepository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message, TrainingEvent? Event)> CreateEventAsync(TrainingEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.Title))
                return (false, "TiÃªu Ä‘á» má»‘c sá»± kiá»‡n khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            if (string.IsNullOrWhiteSpace(evt.Category))
                return (false, "Vui lÃ²ng chá»n loáº¡i sá»± kiá»‡n huáº¥n luyá»‡n.", null);

            if (evt.EndDate.Date < evt.StartDate.Date)
                return (false, "NgÃ y káº¿t thÃºc khÃ´ng Ä‘Æ°á»£c nhá» hÆ¡n ngÃ y báº¯t Ä‘áº§u.", null);

            if (string.IsNullOrWhiteSpace(evt.Status))
                evt.Status = "Äang chuáº©n bá»‹";

            if (string.IsNullOrWhiteSpace(evt.Priority))
                evt.Priority = "BÃ¬nh thÆ°á»ng";

            if (string.IsNullOrWhiteSpace(evt.TargetUnit))
                evt.TargetUnit = "ToÃ n Ä‘Æ¡n vá»‹";

            evt.CreatedAt = DateTime.Now;

            await _eventRepository.AddAsync(evt);
            await _eventRepository.SaveChangesAsync();

            return (true, "ThÃªm má»‘c sá»± kiá»‡n huáº¥n luyá»‡n thÃ nh cÃ´ng!", evt);
        }

        public async Task<(bool Success, string Message)> UpdateEventAsync(TrainingEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.Title))
                return (false, "TiÃªu Ä‘á» má»‘c sá»± kiá»‡n khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            if (evt.EndDate.Date < evt.StartDate.Date)
                return (false, "NgÃ y káº¿t thÃºc khÃ´ng Ä‘Æ°á»£c nhá» hÆ¡n ngÃ y báº¯t Ä‘áº§u.");

            var existing = await _eventRepository.GetByIdAsync(evt.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y má»‘c sá»± kiá»‡n cáº§n chá»‰nh sá»­a.");

            existing.Title = evt.Title.Trim();
            existing.Category = evt.Category;
            existing.StartDate = evt.StartDate;
            existing.EndDate = evt.EndDate;
            existing.TargetUnit = evt.TargetUnit;
            existing.Location = evt.Location;
            existing.Priority = evt.Priority;
            existing.Status = evt.Status;
            existing.Description = evt.Description;

            _eventRepository.Update(existing);
            await _eventRepository.SaveChangesAsync();

            return (true, "Cáº­p nháº­t má»‘c sá»± kiá»‡n huáº¥n luyá»‡n thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> DeleteEventAsync(int id)
        {
            var existing = await _eventRepository.GetByIdAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y má»‘c sá»± kiá»‡n cáº§n xÃ³a.");

            _eventRepository.Delete(existing);
            await _eventRepository.SaveChangesAsync();

            return (true, "ÄÃ£ xÃ³a má»‘c sá»± kiá»‡n thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> ToggleCompleteAsync(int id)
        {
            var existing = await _eventRepository.GetByIdAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y sá»± kiá»‡n.");

            if (existing.Status == "ÄÃ£ hoÃ n thÃ nh")
            {
                existing.Status = "Äang chuáº©n bá»‹";
            }
            else
            {
                existing.Status = "ÄÃ£ hoÃ n thÃ nh";
            }

            _eventRepository.Update(existing);
            await _eventRepository.SaveChangesAsync();

            return (true, $"ÄÃ£ cáº­p nháº­t tráº¡ng thÃ¡i sang: {existing.Status}");
        }
    }
}

