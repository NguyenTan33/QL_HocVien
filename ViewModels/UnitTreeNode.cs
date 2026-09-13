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
        public MilitaryClass? ClassItem { get; set; }
        public AcademicCohort? CohortItem { get; set; }
        public bool IsVirtualNode { get; set; }
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

        /// <summary>Mã Khóa học tổ tiên nếu đơn vị này thuộc một Khóa (ví dụ "K75")</summary>
        public string? AncestorCohortCode { get; set; }

        /// <summary>Đường dẫn mã đơn vị phân cấp từ cấp Tiểu đoàn trở xuống (ví dụ "dBB1/cBB1/bBB1")</summary>
        public string? HierarchyCodePath { get; set; }

        private string? _value;
        public string Value
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_value)) return _value;
                if (!string.IsNullOrWhiteSpace(HierarchyCodePath)) return HierarchyCodePath;
                if (!string.IsNullOrWhiteSpace(Code)) return Code;
                return Name;
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

        /// <summary>
        /// Đường dẫn đầy đủ từ cấp cha đến đơn vị này: "Tiểu đoàn 2 ➔ Đại đội 1 (cBB1)"
        /// Dùng để hiển thị trong dropdown UnitTreeComboBox tránh nhầm giữa 2 đơn vị cùng tên
        /// </summary>
        public string FullHierarchyPath
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                var p = ParentNode;
                var visited = new System.Collections.Generic.HashSet<UnitTreeNode>();
                while (p != null && !p.IsVirtualNode && visited.Add(p))
                {
                    parts.Insert(0, p.Name);
                    p = p.ParentNode;
                }
                if (parts.Count > 0)
                    return string.Join(" ➔ ", parts) + " ➔ " + DisplayText;
                return DisplayText;
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
        public bool CanEdit => Unit != null && !IsVirtualNode && CohortItem == null;
        public bool CanDelete => Unit != null || ClassItem != null || CohortItem != null || IsVirtualNode;

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
                   string.Equals(HierarchyCodePath, target, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Name, target, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Code, target, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(DisplayText, target, StringComparison.OrdinalIgnoreCase);
        }

        public void ExpandParents()
        {
            var p = ParentNode;
            var visited = new System.Collections.Generic.HashSet<UnitTreeNode>();
            while (p != null && visited.Add(p))
            {
                p.IsExpanded = true;
                p = p.ParentNode;
            }
        }
    }
}
