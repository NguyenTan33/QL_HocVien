using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services
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
                return (false, "Tiêu đề mốc sự kiện không được để trống.", null);

            if (string.IsNullOrWhiteSpace(evt.Category))
                return (false, "Vui lòng chọn loại sự kiện huấn luyện.", null);

            if (evt.EndDate.Date < evt.StartDate.Date)
                return (false, "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", null);

            if (string.IsNullOrWhiteSpace(evt.Status))
                evt.Status = "Đang chuẩn bị";

            if (string.IsNullOrWhiteSpace(evt.Priority))
                evt.Priority = "Bình thường";

            if (string.IsNullOrWhiteSpace(evt.TargetUnit))
                evt.TargetUnit = "Toàn đơn vị";

            evt.CreatedAt = DateTime.Now;

            await _eventRepository.AddAsync(evt);
            await _eventRepository.SaveChangesAsync();

            return (true, "Thêm mốc sự kiện huấn luyện thành công!", evt);
        }

        public async Task<(bool Success, string Message)> UpdateEventAsync(TrainingEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.Title))
                return (false, "Tiêu đề mốc sự kiện không được để trống.");

            if (evt.EndDate.Date < evt.StartDate.Date)
                return (false, "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");

            var existing = await _eventRepository.GetByIdAsync(evt.Id);
            if (existing == null)
                return (false, "Không tìm thấy mốc sự kiện cần chỉnh sửa.");

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

            return (true, "Cập nhật mốc sự kiện huấn luyện thành công!");
        }

        public async Task<(bool Success, string Message)> DeleteEventAsync(int id)
        {
            var existing = await _eventRepository.GetByIdAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy mốc sự kiện cần xóa.");

            _eventRepository.Delete(existing);
            await _eventRepository.SaveChangesAsync();

            return (true, "Đã xóa mốc sự kiện thành công!");
        }

        public async Task<(bool Success, string Message)> ToggleCompleteAsync(int id)
        {
            var existing = await _eventRepository.GetByIdAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy sự kiện.");

            if (existing.Status == "Đã hoàn thành")
            {
                existing.Status = "Đang chuẩn bị";
            }
            else
            {
                existing.Status = "Đã hoàn thành";
            }

            _eventRepository.Update(existing);
            await _eventRepository.SaveChangesAsync();

            return (true, $"Đã cập nhật trạng thái sang: {existing.Status}");
        }
    }
}
