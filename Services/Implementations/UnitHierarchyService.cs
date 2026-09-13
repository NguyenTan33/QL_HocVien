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
                    LevelName = pLevel switch { 1 => "CẤP TRUNG ĐOÀN", 2 => "CẤP TIỂU ĐOÀN", _ => "CẤP TRÊN" },
                    Commander = "Chỉ huy trưởng",
                    Icon = pLevel == 1 ? "🏛️" : "🛡️",
                    BadgeBrush = pLevel == 1 ? "#8B1E1E" : "#2E5A36",
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

            foreach (var ru in rootUnits)
            {
                if (!string.IsNullOrWhiteSpace(ru.ParentUnit) && createdRoots.TryGetValue(ru.ParentUnit.Trim(), out var parentNode))
                {
                    int level = DetermineLevel(ru);
                    var node = CreateUnitNode(ru, level);
                    node.ParentNode = parentNode;
                    parentNode.Children.Add(node);
                    AttachChildrenRecursive(node, unitsFromDb);
                }
                else
                {
                    int level = DetermineLevel(ru);
                    var node = CreateUnitNode(ru, level);
                    roots.Add(node);
                    AttachChildrenRecursive(node, unitsFromDb);
                }
            }

            // Gắn các đơn vị mồ côi nếu có
            var addedIds = new HashSet<int>();
            CollectAddedUnitIds(roots, addedIds);
            foreach (var u in unitsFromDb)
            {
                if (u.Id > 0 && !addedIds.Contains(u.Id))
                {
                    var orphanNode = CreateUnitNode(u, DetermineLevel(u));
                    roots.Add(orphanNode);
                    AttachChildrenRecursive(orphanNode, unitsFromDb);
                }
            }

            return roots;
        }

        private void AttachChildrenRecursive(UnitTreeNode parentNode, List<MilitaryUnit> allUnits, HashSet<string>? branchKeys = null)
        {
            branchKeys ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(parentNode.Code)) branchKeys.Add(parentNode.Code.Trim());
            if (!string.IsNullOrWhiteSpace(parentNode.Name)) branchKeys.Add(parentNode.Name.Trim());

            var childUnits = allUnits
                .Where(u => !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                            (u.ParentUnit.Trim().Equals(parentNode.Name.Trim(), StringComparison.OrdinalIgnoreCase) ||
                             (!string.IsNullOrWhiteSpace(parentNode.Code) && u.ParentUnit.Trim().Equals(parentNode.Code.Trim(), StringComparison.OrdinalIgnoreCase))) &&
                            !string.Equals(u.UnitCode, parentNode.Code, StringComparison.OrdinalIgnoreCase) &&
                            !ReferenceEquals(u, parentNode.Unit) &&
                            (string.IsNullOrWhiteSpace(u.UnitCode) || !branchKeys.Contains(u.UnitCode.Trim())) &&
                            (string.IsNullOrWhiteSpace(u.UnitName) || !branchKeys.Contains(u.UnitName.Trim())))
                .ToList();

            foreach (var cu in childUnits)
            {
                int nextLevel = parentNode.Level + 1;
                var childNode = CreateUnitNode(cu, nextLevel);
                childNode.ParentNode = parentNode;
                parentNode.Children.Add(childNode);
                var nextBranch = new HashSet<string>(branchKeys, StringComparer.OrdinalIgnoreCase);
                AttachChildrenRecursive(childNode, allUnits, nextBranch);
            }
        }

        private void CollectAddedUnitIds(IEnumerable<UnitTreeNode> nodes, HashSet<int> ids)
        {
            foreach (var n in nodes)
            {
                if (n.Unit != null && n.Unit.Id > 0) ids.Add(n.Unit.Id);
                if (n.HasChildren) CollectAddedUnitIds(n.Children, ids);
            }
        }

        private int DetermineLevel(MilitaryUnit u)
        {
            string name = (u.UnitName ?? "").ToLowerInvariant();
            string code = (u.UnitCode ?? "").ToLowerInvariant();

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
                1 => "#8B1E1E", // Đỏ cờ
                2 => "#2E5A36", // Xanh lục quân
                3 => "#9C4116", // Nâu đồng
                4 => "#1E426D", // Xanh navy
                5 => "#4F46E5", // Tím chàm
                6 => "#0D9488", // Xanh mòng két
                _ => "#334155"
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
            foreach (var node in source)
            {
                result.Add(CloneNodeRecursive(node, null));
            }
            return result;
        }

        private UnitTreeNode CloneNodeRecursive(UnitTreeNode source, UnitTreeNode? parent)
        {
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
                clone.Children.Add(CloneNodeRecursive(child, clone));
            }

            return clone;
        }
    }
}
