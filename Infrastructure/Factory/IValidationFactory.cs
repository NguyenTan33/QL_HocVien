using System.Threading;
using System.Threading.Tasks;

namespace QL_HocVien.Infrastructure.Factory
{
    /// <summary>
    /// Giao diện Nhà máy Lọc & Kiểm duyệt Dữ liệu (Validation Factory).
    /// Áp dụng mẫu Factory kết hợp Chain of Responsibility để tự động kích hoạt
    /// toàn bộ các IValidationRule đã đăng ký trong DI Container cho từng kiểu dữ liệu.
    /// </summary>
    public interface IValidationFactory
    {
        Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default);
    }
}
