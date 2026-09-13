using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QL_HocVien.Models.Entity;

namespace QL_HocVien.ViewModels
{
    /// <summary>
    /// Đại diện cho một mắt xích (Node) trong sơ đồ rễ cây phân cấp bộ máy cơ cấu tổ chức quân sự
    /// (Trung đoàn ➔ Tiểu đoàn ➔ Đại đội ➔ Trung đội ➔ Tiểu đội ➔ Nhóm)
    /// </summary>
    public partial class UnitTreeNode : ObservableObject
    {
        public MilitaryUnit? Unit { get; set; }
        public UnitTreeNode? ParentNode { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string LevelName { get; set; } = "CẤP ĐẠI ĐỘI";
        public string Commander { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "🚩";
        public string BadgeBrush { get; set; } = "#8B1E1E";
        public int Level { get; set; } = 3;
        public bool IsClassLeaf { get; set; }

        private string? _value;
        public string Value
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_value)) return _value;
                if (!string.IsNullOrWhiteSpace(Name)) return Name;
                return Code;
            }
            set => _value = value;
        }

        public string DisplayText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return Code;
                if (!string.IsNullOrWhiteSpace(Code) && !Name.Contains(Code, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{Name} ({Code})";
                }
                return Name;
            }
        }

        [ObservableProperty]
        private bool _isExpanded = true;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isVisible = true;

        public ObservableCollection<UnitTreeNode> Children { get; } = new();

        public bool HasChildren => Children.Count > 0;
        public string ChildCountText => Children.Count > 0 
            ? $"{Children.Count} đơn vị trực thuộc" 
            : (IsClassLeaf ? "Lớp đào tạo" : "Đơn vị cơ sở");

        public bool CanAddChild => !IsClassLeaf;
        public bool CanEdit => Unit != null;
        public bool CanDelete => Unit != null;

        [RelayCommand]
        public void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        public bool MatchesValue(string? target)
        {
            if (string.IsNullOrWhiteSpace(target)) return false;
            target = target.Trim();
            return string.Equals(Value, target, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Name, target, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Code, target, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(DisplayText, target, StringComparison.OrdinalIgnoreCase);
        }

        public void ExpandParents()
        {
            var p = ParentNode;
            while (p != null)
            {
                p.IsExpanded = true;
                p = p.ParentNode;
            }
        }
    }
}
