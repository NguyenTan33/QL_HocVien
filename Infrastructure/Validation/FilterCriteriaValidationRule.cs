using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;
using QL_HocVien.Models.Filters;

namespace QL_HocVien.Infrastructure.Validation
{
    /// <summary>
    /// Quy tắc kiểm duyệt an ninh cho các bộ lọc tìm kiếm học viên (CadetFilterCriteria).
    /// Chống chèn mã độc, SQLi trong ô tìm kiếm và lọc nâng cao.
    /// </summary>
    public class CadetFilterCriteriaValidationRule : IValidationRule<CadetFilterCriteria>
    {
        private readonly ISecuritySanitizer _sanitizer;

        public CadetFilterCriteriaValidationRule(ISecuritySanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public Task ValidateAsync(CadetFilterCriteria criteria, CancellationToken cancellationToken = default)
        {
            if (criteria == null) return Task.CompletedTask;

            _sanitizer.EnsureSafeInput(criteria.Keyword, "Từ khóa tìm kiếm học viên");
            _sanitizer.EnsureSafeInput(criteria.Rank, "Bộ lọc cấp bậc");
            _sanitizer.EnsureSafeInput(criteria.Unit, "Bộ lọc đơn vị");
            _sanitizer.EnsureSafeInput(criteria.ClassName, "Bộ lọc lớp");
            _sanitizer.EnsureSafeInput(criteria.Position, "Bộ lọc chức vụ");

            return Task.CompletedTask;
        }
    }
}
