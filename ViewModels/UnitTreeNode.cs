using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CadetCountText))]
        private int _cadetCount;

        public string CadetCountText => $"{CadetCount} HV";

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

        /// <summary>
        /// Phân bổ và cộng dồn quân số học viên theo cấu trúc cây phân cấp (Post-order hierarchical rollup)
        /// </summary>
        public static void AssignCadetCounts(IEnumerable<UnitTreeNode> rootNodes, IEnumerable<Cadet> allCadets)
        {
            var allNodes = new List<UnitTreeNode>();
            void Flatten(UnitTreeNode n)
            {
                allNodes.Add(n);
                foreach (var c in n.Children) Flatten(c);
            }
            foreach (var r in rootNodes) Flatten(r);

            var directCounts = new Dictionary<UnitTreeNode, int>();
            foreach (var n in allNodes) directCounts[n] = 0;

            foreach (var cadet in allCadets)
            {
                var match = FindBestMatchingNode(cadet, allNodes);
                if (match != null && directCounts.ContainsKey(match))
                {
                    directCounts[match]++;
                }
            }

            int CalculateRollup(UnitTreeNode node)
            {
                int childSum = 0;
                foreach (var ch in node.Children)
                {
                    childSum += CalculateRollup(ch);
                }
                int total = directCounts[node] + childSum;
                node.CadetCount = total;
                return total;
            }

            foreach (var r in rootNodes)
            {
                CalculateRollup(r);
            }
        }

        /// <summary>
        /// Tìm kiếm mắt xích (Node) phù hợp nhất trong sơ đồ cơ cấu tổ chức để phân bổ học viên
        /// </summary>
        public static UnitTreeNode? FindBestMatchingNode(Cadet cadet, IEnumerable<UnitTreeNode> allNodes)
        {
            var nodesList = allNodes as IList<UnitTreeNode> ?? allNodes.ToList();
            string cCohort = (cadet.Cohort ?? "").Trim();
            string cUnit = (cadet.Unit ?? "").Trim().Replace('\\', '/');

            // 1. Ưu tiên khớp theo Lớp học (ClassId hoặc ClassName) nếu có node ClassLeaf
            if (cadet.ClassId.HasValue && cadet.ClassId.Value > 0)
            {
                var classMatch = nodesList.FirstOrDefault(n => n.ClassItem != null && n.ClassItem.Id == cadet.ClassId.Value);
                if (classMatch != null) return classMatch;
            }
            if (!string.IsNullOrWhiteSpace(cadet.ClassName))
            {
                var classMatch = nodesList.FirstOrDefault(n => n.ClassItem != null &&
                    (string.Equals(n.Code, cadet.ClassName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(n.Name, cadet.ClassName.Trim(), StringComparison.OrdinalIgnoreCase)));
                if (classMatch != null) return classMatch;
            }

            // 2. Thu hẹp danh sách ứng viên theo Khóa học (Cohort) nếu có
            IEnumerable<UnitTreeNode> candidates = nodesList;
            if (!string.IsNullOrWhiteSpace(cCohort))
            {
                var cohortFiltered = nodesList.Where(n =>
                    (n.CohortItem != null && (string.Equals(n.Code, cCohort, StringComparison.OrdinalIgnoreCase) || string.Equals(n.Name, cCohort, StringComparison.OrdinalIgnoreCase))) ||
                    (!string.IsNullOrWhiteSpace(n.AncestorCohortCode) && string.Equals(n.AncestorCohortCode, cCohort, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (cohortFiltered.Count > 0)
                {
                    candidates = cohortFiltered;
                }
            }

            // 3. Khớp theo Đường dẫn đơn vị hoặc Phân cấp mã
            UnitTreeNode? bestNode = null;
            int bestScore = -1;

            var segments = cUnit.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

            foreach (var n in candidates)
            {
                int score = 0;

                // Khớp chính xác toàn bộ HierarchyCodePath (ví dụ "dbb7/cbb2")
                if (!string.IsNullOrWhiteSpace(cUnit) && !string.IsNullOrWhiteSpace(n.HierarchyCodePath))
                {
                    if (string.Equals(n.HierarchyCodePath, cUnit, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 1000;
                    }
                    else if (n.HierarchyCodePath.EndsWith("/" + cUnit, StringComparison.OrdinalIgnoreCase) ||
                             cUnit.EndsWith("/" + n.HierarchyCodePath, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 800;
                    }
                }

                // Khớp theo phân đoạn (segments)
                if (segments.Length > 0)
                {
                    string lastSeg = segments[segments.Length - 1];

                    // Khớp mã đơn vị con ở cuối đường dẫn (ví dụ cbb2)
                    if (string.Equals(n.Code, lastSeg, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 500;
                        if (segments.Length > 1 && n.ParentNode != null &&
                            string.Equals(n.ParentNode.Code, segments[segments.Length - 2], StringComparison.OrdinalIgnoreCase))
                        {
                            score += 300;
                        }
                    }
                    else if (string.Equals(n.Name, lastSeg, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 400;
                    }

                    if (segments.Length == 1 && string.Equals(n.Code, segments[0], StringComparison.OrdinalIgnoreCase))
                    {
                        score += 500;
                    }

                    // Khớp tên tiếng Việt thông dụng (Tiểu đoàn 1 -> dBB1, Đại đội 1 -> cBB1...)
                    var mNum = System.Text.RegularExpressions.Regex.Match(cUnit, @"\d+");
                    if (mNum.Success)
                    {
                        string num = mNum.Value;
                        if ((cUnit.Contains("tiểu đoàn", StringComparison.OrdinalIgnoreCase) && n.Code.Equals($"dBB{num}", StringComparison.OrdinalIgnoreCase)) ||
                            (cUnit.Contains("đại đội", StringComparison.OrdinalIgnoreCase) && n.Code.Equals($"cBB{num}", StringComparison.OrdinalIgnoreCase)) ||
                            (cUnit.Contains("tiểu đội", StringComparison.OrdinalIgnoreCase) && n.Code.Equals($"bBB{num}", StringComparison.OrdinalIgnoreCase)))
                        {
                            score += 450;
                        }
                    }
                }

                // Nếu học viên chỉ có Khóa học mà không có thông tin Đơn vị
                if (string.IsNullOrWhiteSpace(cUnit) && n.CohortItem != null &&
                    string.Equals(n.Code, cCohort, StringComparison.OrdinalIgnoreCase))
                {
                    score += 200;
                }

                // Ưu tiên cấp sâu hơn (phân đội / lớp học / tiểu đội)
                if (score > 0)
                {
                    score += n.Level * 10;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestNode = n;
                }
            }

            // Fallback: Nếu không tìm thấy node phù hợp trong các ứng viên, gán vào node Khóa học nếu có
            if ((bestNode == null || bestScore <= 0) && !string.IsNullOrWhiteSpace(cCohort))
            {
                bestNode = nodesList.FirstOrDefault(n => n.CohortItem != null &&
                    (string.Equals(n.Code, cCohort, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(n.Name, cCohort, StringComparison.OrdinalIgnoreCase)));
            }

            return bestNode;
        }
    }
}
