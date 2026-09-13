using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QL_HocVien.Models.Entity;
using QL_HocVien.Services.Interfaces;
using QL_HocVien.ViewModels;

namespace QL_HocVien.Services.Implementations
{
    public class UnitHierarchyService : IUnitHierarchyService
    {
        private readonly ICatalogService _catalogService;
        private List<UnitTreeNode>? _cachedTree;
        private readonly object _lock = new();
        private readonly System.Threading.SemaphoreSlim _semaphore = new(1, 1);

        public event Action? OnHierarchyChanged;

        public UnitHierarchyService(ICatalogService catalogService)
        {
            _catalogService = catalogService;
            _catalogService.OnUnitsChanged += InvalidateCache;
        }

        public void InvalidateCache()
        {
            lock (_lock)
            {
                _cachedTree = null;
            }
            OnHierarchyChanged?.Invoke();
        }

        public async Task<List<UnitTreeNode>> GetUnitTreeAsync(bool isFilterMode = false)
        {
            List<UnitTreeNode>? masterRoots;
            lock (_lock)
            {
                masterRoots = _cachedTree;
            }

            if (masterRoots == null)
            {
                await _semaphore.WaitAsync();
                try
                {
                    lock (_lock)
                    {
                        masterRoots = _cachedTree;
                    }

                    if (masterRoots == null)
                    {
                        masterRoots = await BuildMasterTreeAsync();
                        lock (_lock)
                        {
                            _cachedTree = masterRoots;
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            // Tạo bản sao độc lập (Clone) để mỗi ComboBox / Dialog có trạng thái IsExpanded / IsSelected riêng
            var cloned = CloneTree(masterRoots);

            if (isFilterMode)
            {
                var allNode = new UnitTreeNode
                {
                    NodeId = "unit_all",
                    Name = "Tất cả",
                    Code = "ALL",
                    Value = "Tất cả",
                    Level = 0,
                    LevelName = "TOÀN THỂ ĐƠN VỊ",
                    Icon = "🌐",
                    BadgeBrush = "#475569",
                    IsExpanded = false
                };
                cloned.Insert(0, allNode);
            }

            return cloned;
        }

        private async Task<List<UnitTreeNode>> BuildMasterTreeAsync()
        {
            var unitsFromDb = (await _catalogService.GetAllUnitsAsync()).ToList();

            // Nếu cơ sở dữ liệu không có đơn vị nào, trả về danh sách rỗng (không tạo cây ảo)
            if (unitsFromDb.Count == 0)
            {
                return new List<UnitTreeNode>();
            }

            var existingUnitNames = new HashSet<string>(unitsFromDb.Select(u => (u.UnitName ?? string.Empty).Trim()), StringComparer.OrdinalIgnoreCase);
            var existingUnitCodes = new HashSet<string>(unitsFromDb.Select(u => (u.UnitCode ?? string.Empty).Trim()), StringComparer.OrdinalIgnoreCase);

            // Tìm các đơn vị cấp trên (ParentUnit) được tham chiếu nhưng chưa có bản ghi tương ứng để tạo virtual root
            var missingParents = unitsFromDb
                .Where(u => !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                            !existingUnitNames.Contains(u.ParentUnit.Trim()) &&
                            !existingUnitCodes.Contains(u.ParentUnit.Trim()) &&
                            !u.ParentUnit.Equals("Học viện", StringComparison.OrdinalIgnoreCase) &&
                            !u.ParentUnit.Equals("Bộ chỉ huy", StringComparison.OrdinalIgnoreCase))
                .Select(u => u.ParentUnit.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var roots = new List<UnitTreeNode>();
            var createdRoots = new Dictionary<string, UnitTreeNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var pName in missingParents)
            {
                int pLevel = DetermineLevel(new MilitaryUnit { UnitName = pName });
                var virtualRoot = new UnitTreeNode
                {
                    NodeId = $"root_{pName}",
                    Name = pName,
                    Code = pName,
                    Value = pName,
                    Level = pLevel,
                    LevelName = pLevel switch { 0 => "CẤP KHÓA HỌC", 1 => "CẤP TRUNG ĐOÀN", 2 => "CẤP TIỂU ĐOÀN", _ => "CẤP TRÊN" },
                    Commander = pLevel == 0 ? "Ban Chỉ huy Khóa" : "Chỉ huy trưởng",
                    Icon = pLevel == 0 ? "🎓" : pLevel == 1 ? "🏛️" : "🛡️",
                    BadgeBrush = pLevel == 0 ? "#1E40AF" : pLevel == 1 ? "#8B1E1E" : "#2E5A36",
                    IsExpanded = true
                };
                roots.Add(virtualRoot);
                createdRoots[pName] = virtualRoot;
            }

            var rootUnits = unitsFromDb.Where(u =>
                string.IsNullOrWhiteSpace(u.ParentUnit) ||
                u.ParentUnit.Equals("Học viện", StringComparison.OrdinalIgnoreCase) ||
                u.ParentUnit.Equals("Bộ chỉ huy", StringComparison.OrdinalIgnoreCase) ||
                (!existingUnitNames.Contains(u.ParentUnit.Trim()) && !existingUnitCodes.Contains(u.ParentUnit.Trim()))
            ).ToList();

            var allAttachedIds = new HashSet<int>();
            foreach (var ru in rootUnits)
            {
                if (!string.IsNullOrWhiteSpace(ru.ParentUnit) && createdRoots.TryGetValue(ru.ParentUnit.Trim(), out var parentNode))
                {
                    int level = DetermineLevel(ru);
                    var node = CreateUnitNode(ru, level);
                    node.ParentNode = parentNode;
                    parentNode.Children.Add(node);
                    allAttachedIds.Add(ru.Id);
                    AttachChildrenRecursive(node, unitsFromDb, null, allAttachedIds, 0);
                }
                else
                {
                    int level = DetermineLevel(ru);
                    var node = CreateUnitNode(ru, level);
                    roots.Add(node);
                    allAttachedIds.Add(ru.Id);
                    AttachChildrenRecursive(node, unitsFromDb, null, allAttachedIds, 0);
                }
            }

            // Gắn các đơn vị mồ côi nếu có
            foreach (var u in unitsFromDb)
            {
                if (u.Id > 0 && !allAttachedIds.Contains(u.Id))
                {
                    var orphanNode = CreateUnitNode(u, DetermineLevel(u));
                    roots.Add(orphanNode);
                    allAttachedIds.Add(u.Id);
                    AttachChildrenRecursive(orphanNode, unitsFromDb, null, allAttachedIds, 0);
                }
            }

            return roots;
        }

        private void AttachChildrenRecursive(
            UnitTreeNode parentNode,
            List<MilitaryUnit> allUnits,
            HashSet<int>? branchUnitIds = null,
            HashSet<int>? allAttachedIds = null,
            int depth = 0)
        {
            if (depth > 15) return; // Bảo vệ chống tràn stack tối đa 15 cấp

            branchUnitIds ??= new HashSet<int>();
            if (parentNode.Unit != null && parentNode.Unit.Id > 0)
            {
                branchUnitIds.Add(parentNode.Unit.Id);
            }

            allAttachedIds ??= new HashSet<int>();

            // Chỉ match khi parentNode thực sự là đơn vị có ID
            if (parentNode.Unit == null || parentNode.Unit.Id <= 0) return;
            int parentId = parentNode.Unit.Id;

            var childUnits = allUnits
                .Where(u => u.Id > 0 &&
                            !branchUnitIds.Contains(u.Id) &&
                            !allAttachedIds.Contains(u.Id) &&
                            IsChildOfUnit(u, parentId, parentNode.Name, parentNode.Code) &&
                            !string.Equals(u.UnitCode, parentNode.Code, StringComparison.OrdinalIgnoreCase) &&
                            !ReferenceEquals(u, parentNode.Unit))
                .ToList();

            foreach (var cu in childUnits)
            {
                allAttachedIds.Add(cu.Id);
                int nextLevel = parentNode.Level + 1;
                var childNode = CreateUnitNode(cu, nextLevel);
                childNode.ParentNode = parentNode;
                parentNode.Children.Add(childNode);

                var nextBranch = new HashSet<int>(branchUnitIds) { cu.Id };
                AttachChildrenRecursive(childNode, allUnits, nextBranch, allAttachedIds, depth + 1);
            }
        }

        /// <summary>
        /// Kiểm tra xem đơn vị u có phải là con trực tiếp của parentId không.
        /// Ưu tiên ParentUnitId (ID chính xác). Fallback sang tên/mã cho dữ liệu cũ.
        /// </summary>
        private static bool IsChildOfUnit(MilitaryUnit u, int parentId, string parentName, string parentCode)
        {
            // Ưu tiên 1: Khớp theo ID chính xác - không nhầm nhánh
            if (u.ParentUnitId.HasValue && u.ParentUnitId.Value > 0)
            {
                return u.ParentUnitId.Value == parentId;
            }

            // Fallback: Khớp theo tên/mã (dữ liệu cũ trước v1.5.3)
            if (string.IsNullOrWhiteSpace(u.ParentUnit)) return false;
            return u.ParentUnit.Trim().Equals(parentName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrWhiteSpace(parentCode) &&
                    u.ParentUnit.Trim().Equals(parentCode.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private void CollectAddedUnitIds(IEnumerable<UnitTreeNode> nodes, HashSet<int> ids, HashSet<UnitTreeNode>? visitedNodes = null)
        {
            visitedNodes ??= new HashSet<UnitTreeNode>();
            foreach (var n in nodes)
            {
                if (!visitedNodes.Add(n)) continue;
                if (n.Unit != null && n.Unit.Id > 0) ids.Add(n.Unit.Id);
                if (n.HasChildren) CollectAddedUnitIds(n.Children, ids, visitedNodes);
            }
        }

        private int DetermineLevel(MilitaryUnit u)
        {
            string name = (u.UnitName ?? "").ToLowerInvariant();
            string code = (u.UnitCode ?? "").ToLowerInvariant();

            if (name.Contains("khóa") || code.StartsWith("k") || name.StartsWith("k")) return 0;
            if (name.Contains("trung đoàn") || name.Contains("học viện") || name.Contains("sư đoàn") || code.StartsWith("e")) return 1;
            if (name.Contains("tiểu đoàn") || code.StartsWith("d")) return 2;
            if (name.Contains("đại đội") || code.StartsWith("c")) return 3;
            if (name.Contains("trung đội") || code.StartsWith("b")) return 4;
            if (name.Contains("tiểu đội") || code.StartsWith("a")) return 5;
            if (name.Contains("nhóm") || name.Contains("tổ") || code.StartsWith("n")) return 6;
            return 3;
        }

        private UnitTreeNode CreateUnitNode(MilitaryUnit u, int level)
        {
            string levelName = level switch
            {
                0 => "CẤP KHÓA HỌC",
                1 => "CẤP TRUNG ĐOÀN",
                2 => "CẤP TIỂU ĐOÀN",
                3 => "CẤP ĐẠI ĐỘI",
                4 => "CẤP TRUNG ĐỘI",
                5 => "CẤP TIỂU ĐOÀN / TIỂU ĐỘI",
                6 => "CẤP NHÓM / TỔ",
                _ => "PHÂN ĐỘI"
            };

            string icon = level switch
            {
                0 => "🎓",
                1 => "🏛️",
                2 => "🛡️",
                3 => "🚩",
                4 => "🎖️",
                5 => "🎯",
                6 => "🔹",
                _ => "🎖️"
            };

            string badgeBrush = level switch
            {
                0 => "#1E40AF",
                1 => "#8B1E1E", // Đỏ cờ
                2 => "#2E5A36", // Xanh lục quân
                3 => "#1E3A8A", // Xanh navy
                4 => "#B45309", // Nâu đồng
                5 => "#4338CA", // Tím chàm
                6 => "#047857", // Xanh mòng két
                _ => "#475569"
            };

            return new UnitTreeNode
            {
                Unit = u,
                NodeId = u.Id > 0 ? $"unit_{u.Id}" : $"unit_{u.UnitCode}",
                Name = u.UnitName,
                Code = u.UnitCode,
                Value = !string.IsNullOrWhiteSpace(u.UnitName) ? u.UnitName : u.UnitCode,
                Level = level,
                LevelName = levelName,
                Commander = !string.IsNullOrWhiteSpace(u.CommanderName) ? u.CommanderName : "Chưa biên chế",
                Phone = !string.IsNullOrWhiteSpace(u.ContactPhone) ? u.ContactPhone : "---",
                Description = u.Description ?? string.Empty,
                Icon = icon,
                BadgeBrush = badgeBrush,
                IsExpanded = level <= 2 // Mở sẵn cấp Trung đoàn và Tiểu đoàn
            };
        }

        private List<UnitTreeNode> CloneTree(IEnumerable<UnitTreeNode> source)
        {
            var result = new List<UnitTreeNode>();
            var visited = new HashSet<UnitTreeNode>();
            foreach (var node in source)
            {
                var cloned = CloneNodeRecursive(node, null, visited);
                if (cloned != null) result.Add(cloned);
            }
            return result;
        }

        private UnitTreeNode? CloneNodeRecursive(UnitTreeNode source, UnitTreeNode? parent, HashSet<UnitTreeNode> visited)
        {
            if (!visited.Add(source)) return null;

            var clone = new UnitTreeNode
            {
                Unit = source.Unit,
                ParentNode = parent,
                NodeId = source.NodeId,
                Name = source.Name,
                Code = source.Code,
                Value = source.Value,
                Level = source.Level,
                LevelName = source.LevelName,
                Commander = source.Commander,
                Phone = source.Phone,
                Description = source.Description,
                Icon = source.Icon,
                BadgeBrush = source.BadgeBrush,
                IsClassLeaf = source.IsClassLeaf,
                IsExpanded = source.IsExpanded,
                IsSelected = false,
                IsVisible = true
            };

            foreach (var child in source.Children)
            {
                var clonedChild = CloneNodeRecursive(child, clone, visited);
                if (clonedChild != null)
                {
                    clone.Children.Add(clonedChild);
                }
            }

            return clone;
        }
    }
}
