using System.Threading.Tasks;

namespace QL_HocVien.Services.Interfaces
{
    /// <summary>
    /// Service hiển thị hộp thoại nhập mật khẩu bảo mật cấp 2 (SOLID - DIP, SRP).
    /// Giúp tách biệt logic giao diện (UI) ra khỏi Service nghiệp vụ (SecurityGateService).
    /// </summary>
    public interface ISecurityDialogService
    {
        /// <summary>
        /// Hiển thị hộp thoại yêu cầu người dùng nhập mật khẩu bảo mật cấp 2.
        /// </summary>
        /// <param name="actionDescription">Mô tả hành động cần bảo vệ</param>
        /// <param name="verifier">Hàm kiểm tra mật khẩu do SecurityGateService cung cấp</param>
        /// <returns>True nếu người dùng xác thực thành công, False nếu hủy hoặc thất bại</returns>
        Task<bool> ShowPasswordVerificationDialogAsync(string actionDescription, System.Func<string, bool> verifier);
    }
}
