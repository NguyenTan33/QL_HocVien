using Microsoft.Win32;

namespace QL_HocVien.Services
{
    public class FileDialogService : IFileDialogService
    {
        public string? ShowOpenFileDialog(string filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", string title = "Chọn tệp Excel")
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = false
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveFileDialog(string defaultFileName, string filter = "Excel Files (*.xlsx)|*.xlsx", string title = "Lưu tệp Excel")
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                Title = title,
                DefaultExt = ".xlsx",
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
