using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.Models.Entity;
using QL_HocVien.Services.Interfaces;
using QL_HocVien.ViewModels;

namespace QL_HocVien.Services.Implementations
{
    public class UnitHierarchyService : IUnitHierarchyService
    {
        private readonly ICatalogService _catalogService;
        private readonly IServiceScopeFactory? _scopeFactory;
        private List<UnitTreeNode>? _cachedTree;
        private readonly object _lock = new();
        private readonly System.Threading.SemaphoreSlim _semaphore = new(1, 1);

        public event Action? OnHierarchyChanged;

        public UnitHierarchyService(ICatalogService catalogService, IServiceScopeFactory? scopeFactory = null)
        {
            _catalogService = catalogService;
            _scopeFactory = scopeFactory;
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

            // 1. Nạp danh mục Khóa học thực tế trong CSDL
            List<AcademicCohort> cohorts = new();
            if (_scopeFactory != null)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var cohortService = scope.ServiceProvider.GetService<ICohortService>();
                    if (cohortService != null)
                    {
                        var cList = await cohortService.GetAllCohortsAsync();
                        cohorts = cList.ToList();
                    }
                }
                catch { }
            }

            var roots = new List<UnitTreeNode>();
            var cohortNodes = new Dictionary<string, UnitTreeNode>(StringComparer.OrdinalIgnoreCase);

            // Tạo node Cấp Khóa Học cho các Khóa học thực tế trong CSDL
            foreach (var cohort in cohorts)
            {
                var cNode = new UnitTreeNode
                {
                    CohortItem = cohort,
                    NodeId = $"cohort_{cohort.Id}",
                    Name = !string.IsNullOrWhiteSpace(cohort.CohortName) ? cohort.CohortName : cohort.CohortCode,
                    Code = cohort.CohortCode,
                    AncestorCohortCode = cohort.CohortCode,
                    HierarchyCodePath = string.Empty,
                    Value = cohort.CohortCode,
                    Level = 0,
                    LevelName = "CẤP KHÓA HỌC",
                    Commander = "Ban Chỉ huy Khóa học",
                    Phone = cohort.AcademicYear ?? "---",
                    Description = $"Khóa đào tạo: {cohort.CohortCode} ({cohort.AcademicYear})",
                    Icon = "🎓",
                    BadgeBrush = "#1E40AF",
                    IsExpanded = true
                };

                bool hasChildUnits = unitsFromDb.Any(u => !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                    (string.Equals(u.ParentUnit.Trim(), cohort.CohortCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(u.ParentUnit.Trim(), cohort.CohortName.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (hasChildUnits)
                {
                    cohortNodes[cohort.CohortCode] = cNode;
                    cohortNodes[cohort.CohortName] = cNode;
                    roots.Add(cNode);
                }
            }

            var allAttachedIds = new HashSet<int>();

            // Gắn các đơn vị con trực thuộc Khóa học (Tiểu đoàn)
            foreach (var cNode in cohortNodes.Values.Distinct())
            {
                var childUnits = unitsFromDb
                    .Where(u => u.Id > 0 &&
                                !allAttachedIds.Contains(u.Id) &&
                                !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                                (string.Equals(u.ParentUnit.Trim(), cNode.Code.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(u.ParentUnit.Trim(), cNode.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var cu in childUnits)
                {
                    allAttachedIds.Add(cu.Id);
                    int nextLevel = DetermineLevel(cu);
                    if (nextLevel == 0) nextLevel = 2; // Cấp Tiểu đoàn
                    var childNode = CreateUnitNode(cu, nextLevel);
                    childNode.ParentNode = cNode;
                    childNode.AncestorCohortCode = cNode.AncestorCohortCode;
                    childNode.HierarchyCodePath = cu.UnitCode;
                    childNode.Value = cu.UnitCode;
                    cNode.Children.Add(childNode);
                    AttachChildrenRecursive(childNode, unitsFromDb, null, allAttachedIds, 0);
                }
            }

            var existingUnitNames = new HashSet<string>(unitsFromDb.Select(u => (u.UnitName ?? string.Empty).Trim()), StringComparer.OrdinalIgnoreCase);
            var existingUnitCodes = new HashSet<string>(unitsFromDb.Select(u => (u.UnitCode ?? string.Empty).Trim()), StringComparer.OrdinalIgnoreCase);
            var cohortKeys = new HashSet<string>(cohortNodes.Keys, StringComparer.OrdinalIgnoreCase);

            // Tìm các đơn vị cấp trên (ParentUnit) được tham chiếu nhưng chưa có bản ghi tương ứng để tạo virtual root
            var missingParents = unitsFromDb
                .Where(u => (!u.ParentUnitId.HasValue || u.ParentUnitId.Value <= 0) &&
                            !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                            !existingUnitNames.Contains(u.ParentUnit.Trim()) &&
                            !existingUnitCodes.Contains(u.ParentUnit.Trim()) &&
                            !cohortKeys.Contains(u.ParentUnit.Trim()) &&
                            !(u.ParentUnit.Contains('/') && (existingUnitCodes.Contains(u.ParentUnit.Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim()) ||
                                                             existingUnitNames.Contains(u.ParentUnit.Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim()))) &&
                            !u.ParentUnit.Equals("Học viện", StringComparison.OrdinalIgnoreCase) &&
                            !u.ParentUnit.Equals("Bộ chỉ huy", StringComparison.OrdinalIgnoreCase))
                .Select(u => u.ParentUnit.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var createdRoots = new Dictionary<string, UnitTreeNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var pName in missingParents)
            {
                int pLevel = DetermineLevel(new MilitaryUnit { UnitName = pName });
                string? ancCohort = (pLevel == 0 || pName.StartsWith("K", StringComparison.OrdinalIgnoreCase)) ? pName : null;
                var virtualRoot = new UnitTreeNode
                {
                    NodeId = $"root_{pName}",
                    Name = pName,
                    Code = pName,
                    Value = pName,
                    AncestorCohortCode = ancCohort,
                    HierarchyCodePath = string.Empty,
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
                u.Id > 0 &&
                !allAttachedIds.Contains(u.Id) &&
                (string.IsNullOrWhiteSpace(u.ParentUnit) ||
                 u.ParentUnit.Equals("Học viện", StringComparison.OrdinalIgnoreCase) ||
                 u.ParentUnit.Equals("Bộ chỉ huy", StringComparison.OrdinalIgnoreCase) ||
                 (!existingUnitNames.Contains(u.ParentUnit.Trim()) && 
                  !existingUnitCodes.Contains(u.ParentUnit.Trim()) && 
                  !cohortKeys.Contains(u.ParentUnit.Trim())))
            ).ToList();

            foreach (var ru in rootUnits)
            {
                if (!string.IsNullOrWhiteSpace(ru.ParentUnit) && createdRoots.TryGetValue(ru.ParentUnit.Trim(), out var parentNode))
                {
                    int level = DetermineLevel(ru);
                    var node = CreateUnitNode(ru, level);
                    node.ParentNode = parentNode;
                    node.AncestorCohortCode = parentNode.AncestorCohortCode;
                    node.HierarchyCodePath = ru.UnitCode;
                    node.Value = ru.UnitCode;
                    parentNode.Children.Add(node);
                    allAttachedIds.Add(ru.Id);
                    AttachChildrenRecursive(node, unitsFromDb, null, allAttachedIds, 0);
                }
                else
                {
                    int level = DetermineLevel(ru);
                    var node = CreateUnitNode(ru, level);
                    node.HierarchyCodePath = ru.UnitCode;
                    node.Value = ru.UnitCode;
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
                    orphanNode.HierarchyCodePath = u.UnitCode;
                    orphanNode.Value = u.UnitCode;
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
                childNode.AncestorCohortCode = parentNode.AncestorCohortCode;

                // Ghép HierarchyCodePath: ví dụ "dBB1/cBB1", "dBB1/cBB1/bBB1"
                string currentPath = !string.IsNullOrWhiteSpace(parentNode.HierarchyCodePath)
                    ? $"{parentNode.HierarchyCodePath}/{cu.UnitCode}"
                    : cu.UnitCode;
                childNode.HierarchyCodePath = currentPath;
                childNode.Value = currentPath;

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
            var pUnit = u.ParentUnit.Trim();
            if (pUnit.Equals(parentName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(parentCode) && pUnit.Equals(parentCode.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (pUnit.Contains('/'))
            {
                var lastSeg = pUnit.Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
                if (lastSeg.Equals(parentName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(parentCode) && lastSeg.Equals(parentCode.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
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
                // Value = UnitCode để filter khớp đúng với Cadet.Unit dạng "dBB1/cBB1/bBB1"
                // Khi người dùng chọn "Tiểu đoàn 1" (UnitCode=dBB1), SelectedUnit="dBB1"
                // → filter "dBB1" sẽ khớp "dBB1/cBB1/bBB1" qua StartsWith("dBB1/")
                Value = !string.IsNullOrWhiteSpace(u.UnitCode) ? u.UnitCode : u.UnitName,
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
                CohortItem = source.CohortItem,
                ParentNode = parent,
                NodeId = source.NodeId,
                Name = source.Name,
                Code = source.Code,
                Value = source.Value,
                AncestorCohortCode = source.AncestorCohortCode,
                HierarchyCodePath = source.HierarchyCodePath,
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

        public async Task EnsureUnitHierarchyStructureAsync(string cohortCode, string unitPath)
        {
            if (string.IsNullOrWhiteSpace(unitPath)) return;

            string cleanPath = unitPath.Trim().Trim('/');
            if (string.IsNullOrWhiteSpace(cleanPath)) return;

            var segments = cleanPath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .ToArray();

            if (segments.Length == 0) return;

            // Nạp danh sách đơn vị hiện có từ CatalogService
            var allUnits = (await _catalogService.GetAllUnitsAsync()).ToList();

            string cleanCohort = !string.IsNullOrWhiteSpace(cohortCode) ? cohortCode.Trim() : "K75";

            // Cấp 1: Tiểu đoàn (ví dụ dBB1, d1)
            string dCode = segments[0];
            var dUnit = allUnits.FirstOrDefault(u =>
                u.UnitCode.Equals(dCode, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(u.ParentUnit) &&
                u.ParentUnit.Equals(cleanCohort, StringComparison.OrdinalIgnoreCase));

            if (dUnit == null)
            {
                // Thử tìm bất kỳ đơn vị nào có mã dCode và ParentUnitId == null
                dUnit = allUnits.FirstOrDefault(u =>
                    u.UnitCode.Equals(dCode, StringComparison.OrdinalIgnoreCase) &&
                    (!u.ParentUnitId.HasValue || u.ParentUnitId.Value <= 0));
            }

            if (dUnit == null)
            {
                int num = ExtractNumber(dCode);
                var newD = new MilitaryUnit
                {
                    UnitCode = dCode,
                    UnitName = num > 0 ? $"Tiểu đoàn {num}" : dCode,
                    ParentUnit = cleanCohort,
                    ParentUnitId = null,
                    Description = $"Tiểu đoàn thuộc {cleanCohort}",
                    CreatedAt = DateTime.Now
                };
                var addRes = await _catalogService.AddUnitAsync(newD);
                if (addRes.Success && addRes.Unit != null)
                {
                    dUnit = addRes.Unit;
                    allUnits.Add(dUnit);
                }
            }
            else if (string.IsNullOrWhiteSpace(dUnit.ParentUnit) || !dUnit.ParentUnit.Equals(cleanCohort, StringComparison.OrdinalIgnoreCase))
            {
                dUnit.ParentUnit = cleanCohort;
                await _catalogService.UpdateUnitAsync(dUnit);
            }

            if (dUnit == null || segments.Length < 2)
            {
                InvalidateCache();
                return;
            }

            // Cấp 2: Đại đội (ví dụ cBB1, c1)
            string cCode = segments[1];
            var cUnit = allUnits.FirstOrDefault(u =>
                u.UnitCode.Equals(cCode, StringComparison.OrdinalIgnoreCase) &&
                (u.ParentUnitId == dUnit.Id ||
                 (!string.IsNullOrWhiteSpace(u.ParentUnit) && (u.ParentUnit.Equals(dUnit.UnitName, StringComparison.OrdinalIgnoreCase) || u.ParentUnit.Equals(dUnit.UnitCode, StringComparison.OrdinalIgnoreCase)))));

            if (cUnit == null)
            {
                int num = ExtractNumber(cCode);
                var newC = new MilitaryUnit
                {
                    UnitCode = cCode,
                    UnitName = num > 0 ? $"Đại đội {num}" : cCode,
                    ParentUnit = dUnit.UnitName,
                    ParentUnitId = dUnit.Id,
                    Description = $"Đại đội thuộc {dUnit.UnitName}",
                    CreatedAt = DateTime.Now
                };
                var addRes = await _catalogService.AddUnitAsync(newC);
                if (addRes.Success && addRes.Unit != null)
                {
                    cUnit = addRes.Unit;
                    allUnits.Add(cUnit);
                }
            }
            else if (cUnit.ParentUnitId != dUnit.Id)
            {
                cUnit.ParentUnitId = dUnit.Id;
                cUnit.ParentUnit = dUnit.UnitName;
                await _catalogService.UpdateUnitAsync(cUnit);
            }

            if (cUnit == null || segments.Length < 3)
            {
                InvalidateCache();
                return;
            }

            // Cấp 3: Tiểu đội / Phân đội (ví dụ bBB1, dBB1, b1)
            string bCode = segments[2];
            var bUnit = allUnits.FirstOrDefault(u =>
                u.UnitCode.Equals(bCode, StringComparison.OrdinalIgnoreCase) &&
                (u.ParentUnitId == cUnit.Id ||
                 (!string.IsNullOrWhiteSpace(u.ParentUnit) && (u.ParentUnit.Equals(cUnit.UnitName, StringComparison.OrdinalIgnoreCase) || u.ParentUnit.Equals(cUnit.UnitCode, StringComparison.OrdinalIgnoreCase)))));

            if (bUnit == null)
            {
                int num = ExtractNumber(bCode);
                var newB = new MilitaryUnit
                {
                    UnitCode = bCode,
                    UnitName = num > 0 ? $"Tiểu đội {num}" : bCode,
                    ParentUnit = cUnit.UnitName,
                    ParentUnitId = cUnit.Id,
                    Description = $"Tiểu đội thuộc {cUnit.UnitName}",
                    CreatedAt = DateTime.Now
                };
                var addRes = await _catalogService.AddUnitAsync(newB);
                if (addRes.Success && addRes.Unit != null)
                {
                    bUnit = addRes.Unit;
                    allUnits.Add(bUnit);
                }
            }
            else if (bUnit.ParentUnitId != cUnit.Id)
            {
                bUnit.ParentUnitId = cUnit.Id;
                bUnit.ParentUnit = cUnit.UnitName;
                await _catalogService.UpdateUnitAsync(bUnit);
            }

            InvalidateCache();
        }

        private static int ExtractNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
            return match.Success && int.TryParse(match.Value, out int n) ? n : 0;
        }
    }
}
