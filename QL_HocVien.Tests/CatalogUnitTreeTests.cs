using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;
using QL_HocVien.Models.Entity;
using QL_HocVien.Services;
using QL_HocVien.ViewModels;
using Xunit;

namespace QL_HocVien.Tests
{
    public class CatalogUnitTreeTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ICatalogService _catalogService;
        private readonly IClassService _classService;
        private readonly IExcelService _excelService;
        private readonly string _dbName;

        public CatalogUnitTreeTests()
        {
            _dbName = $"Test_Tree_Db_{Guid.NewGuid():N}.db";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbName}")
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            var rankRepo = new RankRepository(_context);
            var posRepo = new PositionRepository(_context);
            var unitRepo = new UnitRepository(_context);
            var majorRepo = new MajorRepository(_context);
            var classRepo = new ClassRepository(_context);
            var cadetRepo = new CadetRepository(_context);
            var subjectRepo = new SubjectRepository(_context);
            var examRepo = new PhysicalExamRepository(_context);
            var officerRepo = new OfficerRepository(_context);
            var evalService = new EvaluationService();

            _catalogService = new CatalogService(rankRepo, posRepo, unitRepo, majorRepo);
            _classService = new ClassService(classRepo);
            _excelService = new ExcelService(
                _context,
                cadetRepo,
                classRepo,
                subjectRepo,
                examRepo,
                evalService,
                officerRepo,
                rankRepo,
                posRepo,
                unitRepo,
                majorRepo);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public void Test_UnitTreeNode_Properties_And_ToggleExpand()
        {
            var node = new UnitTreeNode
            {
                Name = "Đại đội 1",
                Code = "c1",
                Level = 3,
                LevelName = "CẤP ĐẠI ĐỘI",
                Commander = "Đại úy Nguyễn Văn Hùng",
                IsExpanded = true
            };

            Assert.True(node.IsExpanded);
            Assert.False(node.HasChildren);
            Assert.Equal("Đơn vị cơ sở", node.ChildCountText);

            node.ToggleExpand();
            Assert.False(node.IsExpanded);

            node.ToggleExpand();
            Assert.True(node.IsExpanded);

            // Thêm node con
            var child = new UnitTreeNode
            {
                Name = "K26A - Chỉ huy Tham mưu",
                Code = "K26A",
                Level = 4,
                IsClassLeaf = true
            };
            node.Children.Add(child);

            Assert.True(node.HasChildren);
            Assert.Equal("1 đơn vị trực thuộc", node.ChildCountText);
            Assert.Equal("Lớp đào tạo", child.ChildCountText);
        }

        [Fact]
        public async Task Test_CatalogManagementViewModel_BuildUnitTree_Hierarchy()
        {
            // Seed Units vào Db
            var units = new List<MilitaryUnit>
            {
                new() { UnitCode = "e1", UnitName = "Trung đoàn 1", ParentUnit = "Học viện", CommanderName = "Thượng tá Nguyễn Mạnh Hùng" },
                new() { UnitCode = "d1", UnitName = "Tiểu đoàn 1", ParentUnit = "Trung đoàn 1", CommanderName = "Trung tá Hoàng Minh Tuấn" },
                new() { UnitCode = "d2", UnitName = "Tiểu đoàn 2", ParentUnit = "Trung đoàn 1", CommanderName = "Trung tá Vũ Đình Cường" },
                new() { UnitCode = "c1", UnitName = "Đại đội 1", ParentUnit = "Tiểu đoàn 1", CommanderName = "Đại úy Nguyễn Văn Hùng" },
                new() { UnitCode = "c2", UnitName = "Đại đội 2", ParentUnit = "Tiểu đoàn 1", CommanderName = "Đại úy Trần Văn Quân" },
            };
            _context.MilitaryUnits.AddRange(units);

            // Seed Class vào Db
            var cls = new MilitaryClass
            {
                ClassCode = "K26A",
                ClassName = "K26A - Chỉ huy Tham mưu",
                Unit = "Đại đội 1",
                Major = "Chỉ huy Tham mưu Lục quân",
                AcademicYear = "2023 - 2027"
            };
            _context.MilitaryClasses.Add(cls);
            await _context.SaveChangesAsync();

            var fakeGate = new QL_HocVien.Tests.TestDoubles.FakeSecurityGateService();
            var fileDialog = new FileDialogService();
            var vm = new CatalogManagementViewModel(_catalogService, _excelService, fileDialog, fakeGate, _classService);
            await vm.LoadAllDataAsync();

            // Kiểm tra cấu trúc rễ cây
            Assert.NotEmpty(vm.UnitTreeNodes);

            var root = vm.UnitTreeNodes.FirstOrDefault(n => n.Name.Contains("Trung đoàn 1"));
            Assert.NotNull(root);
            Assert.Equal(1, root.Level);
            Assert.Equal("CẤP TRUNG ĐOÀN", root.LevelName);
            Assert.True(root.HasChildren);

            // Tiểu đoàn 1 và Tiểu đoàn 2 là con của Trung đoàn 1
            var d1Node = root.Children.FirstOrDefault(c => c.Name.Contains("Tiểu đoàn 1"));
            var d2Node = root.Children.FirstOrDefault(c => c.Name.Contains("Tiểu đoàn 2"));
            Assert.NotNull(d1Node);
            Assert.NotNull(d2Node);
            Assert.Equal(2, d1Node.Level);

            // Đại đội 1 là con của Tiểu đoàn 1
            var c1Node = d1Node.Children.FirstOrDefault(c => c.Name.Contains("Đại đội 1"));
            Assert.NotNull(c1Node);
            Assert.Equal(3, c1Node.Level);

            // Lớp K26A là con của Đại đội 1
            var k26aNode = c1Node.Children.FirstOrDefault(c => c.Name.Contains("K26A"));
            Assert.NotNull(k26aNode);
            Assert.Equal(4, k26aNode.Level);
            Assert.True(k26aNode.IsClassLeaf);
        }

        [Fact]
        public async Task Test_CatalogManagementViewModel_Expand_Collapse_And_Select()
        {
            var units = new List<MilitaryUnit>
            {
                new() { UnitCode = "d1", UnitName = "Tiểu đoàn 1", ParentUnit = "Trung đoàn 1", CommanderName = "Trung tá Tuấn" },
                new() { UnitCode = "c1", UnitName = "Đại đội 1", ParentUnit = "Tiểu đoàn 1", CommanderName = "Đại úy Hùng" },
            };
            _context.MilitaryUnits.AddRange(units);
            await _context.SaveChangesAsync();

            var fakeGate = new QL_HocVien.Tests.TestDoubles.FakeSecurityGateService();
            var fileDialog = new FileDialogService();
            var vm = new CatalogManagementViewModel(_catalogService, _excelService, fileDialog, fakeGate, _classService);
            await vm.LoadAllDataAsync();

            // Test ExpandAll
            vm.ExpandAllTree();
            foreach (var n in vm.UnitTreeNodes)
            {
                Assert.True(n.IsExpanded);
                foreach (var c in n.Children) Assert.True(c.IsExpanded);
            }

            // Test CollapseAll
            vm.CollapseAllTree();
            foreach (var n in vm.UnitTreeNodes)
            {
                Assert.False(n.IsExpanded);
                foreach (var c in n.Children) Assert.False(c.IsExpanded);
            }

            // Test Select Node
            var targetNode = vm.UnitTreeNodes.First();
            vm.SelectTreeNode(targetNode);
            Assert.True(targetNode.IsSelected);
            Assert.Equal(targetNode, vm.SelectedTreeNode);

            // Test Toggle View Mode
            Assert.True(vm.IsTreeViewMode);
            vm.ToggleViewMode();
            Assert.False(vm.IsTreeViewMode);
            vm.ToggleViewMode();
            Assert.True(vm.IsTreeViewMode);
        }

        [Fact]
        public async Task Test_UnitHierarchyService_Builds_Complete_Military_Hierarchy()
        {
            var hierarchyService = new QL_HocVien.Services.Implementations.UnitHierarchyService(_catalogService);
            var tree = await hierarchyService.GetUnitTreeAsync(isFilterMode: false);

            Assert.NotEmpty(tree);

            // Cấp Trung đoàn (Level 1)
            var reg = tree.FirstOrDefault(n => n.Level == 1);
            Assert.NotNull(reg);
            Assert.True(reg.HasChildren);

            // Cấp Tiểu đoàn (Level 2)
            var d1 = reg.Children.FirstOrDefault(c => c.Name.Contains("Tiểu đoàn 1") || c.Code == "d1");
            Assert.NotNull(d1);
            Assert.Equal(2, d1.Level);
            Assert.True(d1.HasChildren);

            // Cấp Đại đội (Level 3)
            var c1 = d1.Children.FirstOrDefault(c => c.Name.Contains("Đại đội 1") || c.Code == "c1");
            Assert.NotNull(c1);
            Assert.Equal(3, c1.Level);
            Assert.True(c1.HasChildren);

            // Cấp Trung đội (Level 4: b1, b2, b3)
            var b3 = c1.Children.FirstOrDefault(c => c.Code == "b3" || c.Name.Contains("Trung đội 3"));
            Assert.NotNull(b3);
            Assert.Equal(4, b3.Level);
            Assert.True(b3.MatchesValue("b3"));
            Assert.True(b3.MatchesValue("Trung đội 3"));

            // Cấp Tiểu đội (Level 5) dưới Trung đội 1
            var b1 = c1.Children.FirstOrDefault(c => c.Code == "b1" || c.Name.Contains("Trung đội 1"));
            Assert.NotNull(b1);
            var a1 = b1.Children.FirstOrDefault(c => c.Code == "a1" || c.Name.Contains("Tiểu đội 1"));
            Assert.NotNull(a1);
            Assert.Equal(5, a1.Level);

            // Cấp Nhóm (Level 6) dưới Tiểu đội 1
            var n1 = a1.Children.FirstOrDefault(c => c.Code == "n1" || c.Name.Contains("Nhóm 1"));
            Assert.NotNull(n1);
            Assert.Equal(6, n1.Level);
        }

        [Fact]
        public async Task Test_UnitHierarchyService_FilterMode_And_Cloning()
        {
            var hierarchyService = new QL_HocVien.Services.Implementations.UnitHierarchyService(_catalogService);
            var filterTree = await hierarchyService.GetUnitTreeAsync(isFilterMode: true);

            // Filter mode phải có node Tất cả ở đầu
            Assert.NotEmpty(filterTree);
            var firstNode = filterTree[0];
            Assert.Equal("Tất cả", firstNode.Name);
            Assert.Equal(0, firstNode.Level);

            // Kiểm tra tính độc lập (Cloning) giữa 2 lần lấy cây
            var tree1 = await hierarchyService.GetUnitTreeAsync(isFilterMode: false);
            var tree2 = await hierarchyService.GetUnitTreeAsync(isFilterMode: false);

            tree1[0].IsExpanded = false;
            tree2[0].IsExpanded = true;
            Assert.False(tree1[0].IsExpanded);
            Assert.True(tree2[0].IsExpanded);
        }

        [Fact]
        public void Test_UnitTreeNode_ExpandParents()
        {
            var root = new UnitTreeNode { Name = "Gốc", IsExpanded = false };
            var child = new UnitTreeNode { Name = "Con", ParentNode = root, IsExpanded = false };
            var grandchild = new UnitTreeNode { Name = "Cháu", ParentNode = child, IsExpanded = false };

            Assert.False(root.IsExpanded);
            Assert.False(child.IsExpanded);

            grandchild.ExpandParents();

            Assert.True(root.IsExpanded);
            Assert.True(child.IsExpanded);
        }
    }
}
