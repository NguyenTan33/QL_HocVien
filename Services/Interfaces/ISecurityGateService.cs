using System;
using System.Threading.Tasks;

namespace QL_HocVien.Services
{
    /// <summary>
    /// Quản lý Khóa bảo mật cấp 2 (Khóa rương bảo vệ dữ liệu).
    /// Tuân thủ nguyên lý SOLID (SRP, ISP, DIP).
    /// </summary>
    public interface ISecurityGateService
    {
        /// <summary>
        /// Cho biết chức năng khóa bảo mật cấp 2 có đang được bật hay không.
        /// </summary>
        bool IsProtectionEnabled { get; }

        /// <summary>
        /// Cho biết hệ thống hiện tại có đang trong trạng thái Mở khóa (còn hiệu lực trong 5 phút) hay không.
        /// </summary>
        bool IsUnlocked { get; }

        /// <summary>
        /// Số giây còn lại trước khi tự động khóa lại (tối đa 300 giây = 5 phút).
        /// </summary>
        int RemainingSeconds { get; }

        /// <summary>
        /// Chuỗi định dạng thời gian còn lại (ví dụ: "04:59").
        /// </summary>
        string FormattedRemainingTime { get; }

        /// <summary>
        /// Kích hoạt tính năng bảo mật với mật khẩu mới.
        /// </summary>
        bool EnableProtection(string password);

        /// <summary>
        /// Tắt tính năng bảo mật (yêu cầu mật khẩu hiện tại).
        /// </summary>
        bool DisableProtection(string currentPassword);

        /// <summary>
        /// Đổi mật khẩu bảo mật cấp 2.
        /// </summary>
        bool ChangePassword(string oldPassword, string newPassword);

        /// <summary>
        /// Kiểm tra tính chính xác của mật khẩu.
        /// </summary>
        bool VerifyPassword(string password);

        /// <summary>
        /// Mở khóa cấp 2 cho thời gian tự do (5 phút = 300 giây).
        /// </summary>
        void UnlockForGracePeriod();

        /// <summary>
        /// Lập tức thu hồi quyền thao tác, khóa màn hình / dữ liệu ngay lập tức.
        /// </summary>
        void LockNow();

        /// <summary>
        /// Bảo vệ một hành động nghiệp vụ (Thêm, Sửa, Xóa, Xuất/Nhập Excel).
        /// Nếu bảo vệ chưa bật: Trả về true ngay lập tức.
        /// Nếu đang mở khóa (còn trong 5 phút): Trả về true ngay lập tức.
        /// Nếu đang khóa: Hiển thị Dialog yêu cầu nhập mật khẩu cấp 2.
        /// Nếu người dùng xác thực thành công: Kích hoạt 5 phút mở khóa và trả về true.
        /// Nếu thất bại hoặc người dùng bấm Hủy: Trả về false.
        /// </summary>
        Task<bool> EnsureUnlockedAsync(string actionDescription = "thực hiện thao tác này");

        /// <summary>
        /// Sự kiện phát sinh khi trạng thái bảo mật thay đổi (Bật/Tắt, Mở/Khóa, Đếm giây).
        /// </summary>
        event Action? OnSecurityStateChanged;
    }
}
