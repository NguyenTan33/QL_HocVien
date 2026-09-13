using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.ViewModels;

namespace QL_HocVien.Services.Interfaces
{
    public interface IUnitHierarchyService
    {
        /// <summary>
        /// Lấy cây phân cấp đơn vị hoàn chỉnh (Tiểu đoàn > Đại đội > Trung đội > Tiểu đội > Nhóm)
        /// Mỗi lần gọi trả về cấu trúc cây độc lập (cloned) để không ảnh hưởng trạng thái mở rộng/chọn giữa các ComboBox khác nhau.
        /// </summary>
        /// <param name="isFilterMode">Nếu true, thêm tùy chọn [ Tất cả đơn vị ] ở đầu</param>
        Task<List<UnitTreeNode>> GetUnitTreeAsync(bool isFilterMode = false);

        /// <summary>
        /// Xóa cache cây khi danh mục đơn vị có thay đổi (Thêm/Sửa/Xóa đơn vị)
        /// </summary>
        void InvalidateCache();

        /// <summary>
        /// Sự kiện phát ra khi cây đơn vị có sự cập nhật trong cơ sở dữ liệu
        /// </summary>
        event Action? OnHierarchyChanged;
    }
}
