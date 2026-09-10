namespace QL_HocVien.Services
{
    public interface IFileDialogService
    {
        string? ShowOpenFileDialog(string filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", string title = "Chọn tệp Excel");
        string? ShowSaveFileDialog(string defaultFileName, string filter = "Excel Files (*.xlsx)|*.xlsx", string title = "Lưu tệp Excel");
    }
}
