using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.DTOs
{
    /// <summary>
    /// ViewModel đại diện cho 1 dòng đợt kiểm tra trong Form Thêm/Sửa Môn học chính
    /// </summary>
    public partial class SubjectComponentItemViewModel : ObservableObject
    {
        public int Id { get; set; }

        private string _componentName = string.Empty;
        public string ComponentName
        {
            get => _componentName;
            set => SetProperty(ref _componentName, value);
        }

        private double _credits = 1.0;
        public double Credits
        {
            get => _credits;
            set
            {
                if (SetProperty(ref _credits, value))
                    OnCreditsChangedAction?.Invoke();
            }
        }

        public Action? OnCreditsChangedAction { get; set; }
    }
}
