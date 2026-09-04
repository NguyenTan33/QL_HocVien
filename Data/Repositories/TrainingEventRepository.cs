using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public class TrainingEventRepository : Repository<TrainingEvent>, ITrainingEventRepository
    {
        public TrainingEventRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TrainingEvent>> GetFilteredEventsAsync(string? category, string? status, int? month, int? year)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
            {
                query = query.Where(e => e.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(e => e.Status == status);
            }

            if (year.HasValue && year.Value > 0)
            {
                query = query.Where(e => e.StartDate.Year == year.Value || e.EndDate.Year == year.Value);
            }

            if (month.HasValue && month.Value > 0)
            {
                query = query.Where(e => e.StartDate.Month == month.Value || e.EndDate.Month == month.Value);
            }

            return await query.OrderBy(e => e.StartDate).ToListAsync();
        }

        public async Task<IEnumerable<TrainingEvent>> GetUpcomingEventsAsync(int count)
        {
            return await _dbSet
                .Where(e => e.Status != "Đã hoàn thành")
                .OrderBy(e => e.StartDate)
                .Take(count)
                .ToListAsync();
        }
    }
}
