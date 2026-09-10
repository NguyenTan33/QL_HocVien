namespace QL_HocVien.Services.Interfaces
{
    public interface IFileDialogService
    {
        string? ShowOpenFileDialog(string filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", string title = "Chá»n tá»‡p Excel");
        string? ShowSaveFileDialog(string defaultFileName, string filter = "Excel Files (*.xlsx)|*.xlsx", string title = "LÆ°u tá»‡p Excel");
    }
}

