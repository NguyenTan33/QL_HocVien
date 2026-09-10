using System;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>
        /// Lấy phiên bản hiện tại của ứng dụng đang chạy.
        /// </summary>
        Version GetCurrentVersion();

        /// <summary>
        /// Kiểm tra cập nhật từ máy chủ/GitHub chứa tệp version.json.
        /// </summary>
        Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    }
}
