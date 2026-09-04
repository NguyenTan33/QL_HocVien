using System.Threading;
using System.Threading.Tasks;

namespace QL_HocVien.Infrastructure.Factory
{
    /// <summary>
    /// Giao diện định nghĩa một quy tắc thẩm định (Rule) độc lập cho kiểu dữ liệu TRequest.
    /// Tuân thủ chuẩn SOLID:
    /// - Single Responsibility (Mỗi Rule kiểm tra 1 tập ràng buộc/nguy cơ cụ thể)
    /// - Liskov Substitution (Mọi Rule đều có thể hoán đổi và thực thi trong Pipeline)
    /// </summary>
    public interface IValidationRule<in TRequest>
    {
        Task ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
