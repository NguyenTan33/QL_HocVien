using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace QL_HocVien.Infrastructure.Factory
{
    /// <summary>
    /// Triển khai Nhà máy Lọc & Thẩm định Dữ liệu (Validation Factory).
    /// Tự động lấy danh sách IValidationRule<TRequest> từ Service Provider và thực thi tuần tự.
    /// Tuân thủ nguyên lý Open/Closed: Khi có Rule mới, chỉ cần thêm Class, Factory không cần thay đổi.
    /// </summary>
    public class ValidationFactory : IValidationFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new Exceptions.ValidationException("Dữ liệu yêu cầu kiểm duyệt không được để trống (null).");
            }

            // 1. Quét tìm tất cả các Rule đã đăng ký trong DI cho kiểu TRequest
            var rules = _serviceProvider.GetServices<IValidationRule<TRequest>>();

            // 2. Chạy lần lượt từng Rule
            foreach (var rule in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await rule.ValidateAsync(request, cancellationToken);
            }
        }
    }
}
