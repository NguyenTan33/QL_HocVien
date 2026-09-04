using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;
using QL_HocVien.Models.Filters;
using QL_HocVien.Services;
using Xunit;

namespace QL_HocVien.Tests
{
    public class AppServicesTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IClassRepository _classRepository;
        private readonly ICadetRepository _cadetRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly IPhysicalExamRepository _examRepository;
        private readonly IOfficerRepository _officerRepository;
        private readonly IRankRepository _rankRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IMajorRepository _majorRepository;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;
        private readonly IClassService _classService;
        private readonly ICadetService _cadetService;
        private readonly ISubjectService _subjectService;
        private readonly IEvaluationService _evaluationService;
        private readonly IPhysicalExamService _examService;
        private readonly IOfficerService _officerService;
        private readonly ICatalogService _catalogService;
        private readonly IExcelService _excelService;
        private readonly string _dbName;

        public AppServicesTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbName}.db")
                .Options;

            _context = new AppDbContext(options);
            DbInitializer.Initialize(_context);

            _userRepository = new UserRepository(_context);
            _classRepository = new ClassRepository(_context);
            _cadetRepository = new CadetRepository(_context);
            _subjectRepository = new SubjectRepository(_context);
            _examRepository = new PhysicalExamRepository(_context);
            _officerRepository = new OfficerRepository(_context);
            _rankRepository = new RankRepository(_context);
            _positionRepository = new PositionRepository(_context);
            _unitRepository = new UnitRepository(_context);
            _majorRepository = new MajorRepository(_context);
            _emailService = new EmailService();

            _authService = new AuthService(_userRepository, _cadetRepository, _context, _emailService);
            _classService = new ClassService(_classRepository);
            _cadetService = new CadetService(_cadetRepository);
            _subjectService = new SubjectService(_subjectRepository);
            _evaluationService = new EvaluationService();
            _examService = new PhysicalExamService(_examRepository, _subjectRepository, _evaluationService);
            _officerService = new OfficerService(_officerRepository, _userRepository);
            _catalogService = new CatalogService(_rankRepository, _positionRepository, _unitRepository, _majorRepository);
            _excelService = new ExcelService(
                _context,
                _cadetRepository,
                _classRepository,
                _subjectRepository,
                _examRepository,
                _evaluationService,
                _officerRepository,
                _rankRepository,
                _positionRepository,
                _unitRepository,
                _majorRepository);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Test_Login_With_Username_And_Phone()
        {
            // 1. Đăng nhập bằng Username
            var loginWithUser = await _authService.LoginAsync("admin", "Admin@123");
            Assert.True(loginWithUser.Success, "Login with username failed: " + loginWithUser.Message);
            Assert.NotNull(loginWithUser.User);
            Assert.Equal("admin", loginWithUser.User.Username);

            // 2. Đăng nhập bằng Số điện thoại (admin có sđt 0988888888)
            var loginWithPhone = await _authService.LoginAsync("0988888888", "Admin@123");
            Assert.True(loginWithPhone.Success, "Login with phone failed: " + loginWithPhone.Message);
            Assert.NotNull(loginWithPhone.User);
            Assert.Equal("admin", loginWithPhone.User.Username);

            // 3. Đăng nhập sai mật khẩu
            var loginWrong = await _authService.LoginAsync("admin", "SaiMatKhau");
            Assert.False(loginWrong.Success);
        }

        [Fact]
        public async Task Test_Register_And_Validation()
        {
            // Đăng ký mới thành công
            var reg = await _authService.RegisterAsync("hocvien99", "Nguyễn Văn Chiến", "0933999999", "chien.nv@mod.gov.vn", "Chien@123");
            Assert.True(reg.Success, reg.Message);

            // Đăng ký trùng username
            var regDupUser = await _authService.RegisterAsync("hocvien99", "Tên Khác", "0933888888", "other@mod.gov.vn", "Pass@123");
            Assert.False(regDupUser.Success);

            // Đăng ký trùng SĐT
            var regDupPhone = await _authService.RegisterAsync("hocvien100", "Tên Khác 2", "0933999999", "other2@mod.gov.vn", "Pass@123");
            Assert.False(regDupPhone.Success);

            // Đăng nhập bằng tài khoản vừa đăng ký bằng SĐT
            var loginNew = await _authService.LoginAsync("0933999999", "Chien@123");
            Assert.True(loginNew.Success);
            Assert.Equal("Nguyễn Văn Chiến", loginNew.User?.FullName);
        }

        [Fact]
        public async Task Test_ForgotPassword_With_OTP_And_Reset()
        {
            // Yêu cầu gửi OTP qua email của admin
            var otpResult = await _authService.RequestPasswordResetOtpAsync("admin@mod.gov.vn");
            Assert.True(otpResult.Success, otpResult.Message);
            Assert.NotNull(otpResult.Otp);
            Assert.Equal(6, otpResult.Otp.Length);

            // Đổi mật khẩu mới bằng mã OTP
            var resetResult = await _authService.ResetPasswordWithOtpAsync("admin@mod.gov.vn", otpResult.Otp, "NewAdminPass@2026");
            Assert.True(resetResult.Success, resetResult.Message);

            // Đăng nhập lại với mật khẩu mới
            var loginNewPass = await _authService.LoginAsync("admin", "NewAdminPass@2026");
            Assert.True(loginNewPass.Success);

            // Đăng nhập với mật khẩu cũ phải thất bại
            var loginOldPass = await _authService.LoginAsync("admin", "Admin@123");
            Assert.False(loginOldPass.Success);
        }

        [Fact]
        public async Task Test_Cadet_CRUD_And_Search_And_CodeSuggestion()
        {
            // Gợi ý mã
            var suggestedCode = await _cadetService.GenerateSuggestedCadetCodeAsync();
            Assert.StartsWith("HV-", suggestedCode);

            // Thêm học viên
            var newCadet = new Cadet
            {
                CadetCode = "HV-TEST-01",
                FullName = "Đồng chí Kiểm Thử",
                PhoneNumber = "0944555666",
                ClassName = "K26C",
                Unit = "Đại đội 1",
                Rank = "Hạ sĩ",
                Position = "Chiến sĩ"
            };
            var addRes = await _cadetService.AddCadetAsync(newCadet);
            Assert.True(addRes.Success, addRes.Message);

            // Tìm kiếm
            var search = await _cadetService.SearchCadetsAsync("Kiểm Thử", "Tất cả", "Tất cả", null);
            Assert.NotEmpty(search);
            Assert.Equal("HV-TEST-01", search.First().CadetCode);

            // Lọc theo đơn vị
            var filterUnit = await _cadetService.SearchCadetsAsync(null, "Tất cả", "Đại đội 1", null);
            Assert.All(filterUnit, c => Assert.Equal("Đại đội 1", c.Unit));

            // Đặt lại mật khẩu học viên
            var resetCadetPass = await _authService.ResetCadetPasswordAsync(newCadet.Id, "HocVienPass@123");
            Assert.True(resetCadetPass.Success, resetCadetPass.Message);
        }

        [Fact]
        public async Task Test_Subject_Management_And_Advanced_Filter()
        {
            // Kiểm tra danh mục môn đã seed
            var subjects = await _subjectService.GetAllSubjectsAsync();
            Assert.True(subjects.Count() >= 5);

            // Lọc nâng cao theo mã môn "XD"
            var searchCode = await _subjectService.SearchSubjectsAsync("XD", "Tất cả");
            Assert.Contains(searchCode, s => s.SubjectCode == "XD");

            // Lọc theo tên môn "xà kép"
            var searchName = await _subjectService.SearchSubjectsAsync("xà kép", "Tất cả");
            Assert.Contains(searchName, s => s.SubjectCode == "XK");

            // Lọc theo nhóm "Sức bền"
            var filterCat = await _subjectService.SearchSubjectsAsync(null, "Sức bền");
            Assert.All(filterCat, s => Assert.Equal("Sức bền", s.Category));
        }

        [Fact]
        public async Task Test_Evaluation_Standards_TT32()
        {
            var xdSubject = (await _subjectService.GetAllSubjectsAsync()).First(s => s.SubjectCode == "XD");
            var c100Subject = (await _subjectService.GetAllSubjectsAsync()).First(s => s.SubjectCode == "C100");

            // Xà đơn: Tiêu chuẩn Giỏi >= 23, Khá >= 19, Đạt >= 15
            Assert.Equal("Xuất sắc", _evaluationService.EvaluateGrade(xdSubject, 26));
            Assert.Equal("Giỏi", _evaluationService.EvaluateGrade(xdSubject, 23));
            Assert.Equal("Khá", _evaluationService.EvaluateGrade(xdSubject, 20));
            Assert.Equal("Đạt", _evaluationService.EvaluateGrade(xdSubject, 15));
            Assert.Equal("Không đạt", _evaluationService.EvaluateGrade(xdSubject, 10));

            // Chạy 100m: Tiêu chuẩn Giỏi <= 13.3s, Khá <= 13.6s, Đạt <= 14.0s
            Assert.Equal("Xuất sắc", _evaluationService.EvaluateGrade(c100Subject, 12.4));
            Assert.Equal("Giỏi", _evaluationService.EvaluateGrade(c100Subject, 13.2));
            Assert.Equal("Khá", _evaluationService.EvaluateGrade(c100Subject, 13.5));
            Assert.Equal("Đạt", _evaluationService.EvaluateGrade(c100Subject, 14.0));
            Assert.Equal("Không đạt", _evaluationService.EvaluateGrade(c100Subject, 15.2));
        }

        [Fact]
        public async Task Test_Excel_Cadet_Export_And_Import()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"Cadets_Test_{Guid.NewGuid():N}.xlsx");
            try
            {
                var cadets = (await _cadetService.GetAllCadetsAsync()).ToList();
                Assert.NotEmpty(cadets);

                // Xuất Excel
                var exportRes = await _excelService.ExportCadetsToExcelAsync(cadets, tempFile);
                Assert.True(exportRes.Success, exportRes.Message);
                Assert.True(File.Exists(tempFile));

                // Nhập lại từ Excel
                var importRes = await _excelService.ImportCadetsFromExcelAsync(tempFile);
                Assert.True(importRes.Success, importRes.Message);
                Assert.NotEmpty(importRes.Cadets);
                Assert.Equal(cadets.Count, importRes.Cadets.Count);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task Test_Excel_Full_System_Export_And_Import()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"FullBackup_Test_{Guid.NewGuid():N}.xlsx");
            try
            {
                // Xuất toàn bộ dữ liệu ra Excel (multi-sheet)
                var exportRes = await _excelService.ExportAllDataToExcelAsync(tempFile);
                Assert.True(exportRes.Success, exportRes.Message);
                Assert.True(File.Exists(tempFile));

                // Nhập lại toàn bộ dữ liệu từ Excel
                var importRes = await _excelService.ImportAllDataFromExcelAsync(tempFile);
                Assert.True(importRes.Success, importRes.Message);
                Assert.True(importRes.CadetsCount > 0);
                Assert.True(importRes.SubjectsCount > 0);
                Assert.True(importRes.OfficersCount > 0);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task Test_MilitaryClass_CRUD_And_Cadet_Association()
        {
            // 1. Kiểm tra seed lớp học
            var classes = (await _classService.GetAllClassesAsync()).ToList();
            Assert.True(classes.Count >= 3, "Phải có ít nhất 3 lớp được seed.");
            Assert.Contains(classes, c => c.ClassCode == "K26A");

            // 2. Kiểm tra học viên seed đã liên kết với lớp
            var classK26A = classes.First(c => c.ClassCode == "K26A");
            var k26AWithCadets = await _classService.GetClassWithCadetsAsync(classK26A.Id);
            Assert.NotNull(k26AWithCadets);
            Assert.True(k26AWithCadets.Cadets.Count >= 1, "Lớp K26A phải có học viên liên kết.");

            // 3. Thêm lớp mới
            var newClass = new MilitaryClass
            {
                ClassCode = "K26D",
                ClassName = "K26D - Trinh sát Đặc nhiệm",
                Unit = "Đại đội 4",
                Major = "Trinh sát đặc nhiệm",
                OfficerInCharge = "Thiếu tá Nguyễn Văn Bình",
                AcademicYear = "2024 - 2028",
                Description = "Lớp đào tạo trinh sát cơ động"
            };
            var addRes = await _classService.AddClassAsync(newClass);
            Assert.True(addRes.Success, addRes.Message);
            Assert.NotNull(addRes.Class);

            // 4. Thử thêm trùng mã lớp -> phải báo lỗi
            var dupClass = new MilitaryClass
            {
                ClassCode = "K26D",
                ClassName = "Lớp trùng mã",
                Unit = "Đại đội 1"
            };
            var dupRes = await _classService.AddClassAsync(dupClass);
            Assert.False(dupRes.Success, "Không được phép thêm trùng mã lớp.");

            // 5. Tìm kiếm lớp học
            var searchByKw = await _classService.SearchClassesAsync("Trinh sát", "Tất cả", "Tất cả");
            Assert.Single(searchByKw);
            Assert.Equal("K26D", searchByKw.First().ClassCode);

            // 6. Cập nhật lớp học
            addRes.Class.Description = "Cập nhật mô tả chuyên sâu";
            var updateRes = await _classService.UpdateClassAsync(addRes.Class);
            Assert.True(updateRes.Success, updateRes.Message);

            // 7. Thêm học viên vào lớp mới này
            var cadet = new Cadet
            {
                CadetCode = "HV-CLASS-TEST-01",
                FullName = "Trần Trinh Sát",
                ClassId = addRes.Class.Id,
                ClassName = addRes.Class.ClassName,
                Unit = "Đại đội 4",
                PhoneNumber = "0987654321"
            };
            var addCadetRes = await _cadetService.AddCadetAsync(cadet);
            Assert.True(addCadetRes.Success);

            // Kiểm tra quân số lớp đã tăng
            var updatedClassWithCadets = await _classService.GetClassWithCadetsAsync(addRes.Class.Id);
            Assert.NotNull(updatedClassWithCadets);
            Assert.Single(updatedClassWithCadets.Cadets);

            // 8. Xóa lớp học -> học viên vẫn còn nguyên trong hệ thống
            var delRes = await _classService.DeleteClassAsync(addRes.Class.Id);
            Assert.True(delRes.Success, delRes.Message);

            var checkCadetStillExists = await _cadetService.GetCadetByIdAsync(cadet.Id);
            Assert.NotNull(checkCadetStillExists);
        }

        [Fact]
        public async Task Test_Excel_Class_Export_And_Import()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"Classes_Test_{Guid.NewGuid():N}.xlsx");
            try
            {
                var classes = (await _classService.GetAllClassesAsync()).ToList();
                Assert.NotEmpty(classes);

                // Xuất Excel danh sách lớp
                var exportRes = await _excelService.ExportClassesToExcelAsync(classes, tempFile);
                Assert.True(exportRes.Success, exportRes.Message);
                Assert.True(File.Exists(tempFile));

                // Nhập lại từ Excel
                var importRes = await _excelService.ImportClassesFromExcelAsync(tempFile);
                Assert.True(importRes.Success, importRes.Message);
                Assert.NotEmpty(importRes.Classes);
                Assert.Equal(classes.Count, importRes.Classes.Count);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task Test_MilitaryCatalogs_CRUD()
        {
            // 1. Kiểm tra seed danh mục tổ chức
            var ranks = (await _catalogService.GetAllRanksAsync()).ToList();
            var positions = (await _catalogService.GetAllPositionsAsync()).ToList();
            var units = (await _catalogService.GetAllUnitsAsync()).ToList();
            var majors = (await _catalogService.GetAllMajorsAsync()).ToList();

            Assert.True(ranks.Count >= 15, $"Seed cấp bậc phải >= 15 (hiện tại: {ranks.Count})");
            Assert.True(positions.Count >= 12, $"Seed chức vụ phải >= 12 (hiện tại: {positions.Count})");
            Assert.True(units.Count >= 6, $"Seed đơn vị phải >= 6 (hiện tại: {units.Count})");
            Assert.True(majors.Count >= 5, $"Seed chuyên ngành phải >= 5 (hiện tại: {majors.Count})");

            // 2. Thêm Cấp bậc mới
            var newRank = new MilitaryRank
            {
                RankCode = "TH_TUONG",
                RankName = "Thượng tướng",
                RankGroup = "Sĩ quan cấp Tướng",
                DisplayOrder = 18,
                Description = "Cấp bậc Tướng lĩnh cấp cao"
            };
            var addRankRes = await _catalogService.AddRankAsync(newRank);
            Assert.True(addRankRes.Success, addRankRes.Message);
            Assert.NotNull(addRankRes.Rank);

            // Tìm kiếm
            var searchRank = (await _catalogService.SearchRanksAsync("Thượng tướng", null)).ToList();
            Assert.Single(searchRank);

            // Cập nhật
            addRankRes.Rank.Description = "Cập nhật mô tả";
            var updateRankRes = await _catalogService.UpdateRankAsync(addRankRes.Rank);
            Assert.True(updateRankRes.Success, updateRankRes.Message);

            // Xóa
            var delRankRes = await _catalogService.DeleteRankAsync(addRankRes.Rank.Id);
            Assert.True(delRankRes.Success, delRankRes.Message);

            // 3. Thêm Chức vụ mới
            var newPos = new MilitaryPosition
            {
                PositionCode = "PHO_SU_DOAN",
                PositionName = "Phó Sư đoàn trưởng",
                PositionGroup = "Chỉ huy Chiến thuật",
                DisplayOrder = 14,
                Description = "Chỉ huy cấp Sư đoàn"
            };
            var addPosRes = await _catalogService.AddPositionAsync(newPos);
            Assert.True(addPosRes.Success, addPosRes.Message);

            // Xóa chức vụ
            var delPosRes = await _catalogService.DeletePositionAsync(addPosRes.Position!.Id);
            Assert.True(delPosRes.Success, delPosRes.Message);

            // 4. Thêm Đơn vị mới
            var newUnit = new MilitaryUnit
            {
                UnitCode = "TD2",
                UnitName = "Tiểu đoàn 2",
                ParentUnit = "Trung đoàn 1",
                CommanderName = "Đồng chí Thiếu tá Trần Văn A",
                ContactPhone = "0977112233"
            };
            var addUnitRes = await _catalogService.AddUnitAsync(newUnit);
            Assert.True(addUnitRes.Success, addUnitRes.Message);

            // Xóa đơn vị
            var delUnitRes = await _catalogService.DeleteUnitAsync(addUnitRes.Unit!.Id);
            Assert.True(delUnitRes.Success, delUnitRes.Message);

            // 5. Thêm Chuyên ngành mới
            var newMajor = new MilitaryMajor
            {
                MajorCode = "TAC_DIEN",
                MajorName = "Tác chiến Điện tử",
                TrainingDuration = "4 năm",
                Department = "Khoa Vô tuyến Điện tử"
            };
            var addMajorRes = await _catalogService.AddMajorAsync(newMajor);
            Assert.True(addMajorRes.Success, addMajorRes.Message);

            // Xóa chuyên ngành
            var delMajorRes = await _catalogService.DeleteMajorAsync(addMajorRes.Major!.Id);
            Assert.True(delMajorRes.Success, delMajorRes.Message);

            // 6. Kiểm tra các dropdown name helpers
            var rankNames = await _catalogService.GetRankDropdownAsync();
            var posNames = await _catalogService.GetPositionDropdownAsync();
            var unitNames = await _catalogService.GetUnitDropdownAsync();
            var majorNames = await _catalogService.GetMajorDropdownAsync();

            Assert.NotEmpty(rankNames);
            Assert.NotEmpty(posNames);
            Assert.NotEmpty(unitNames);
            Assert.NotEmpty(majorNames);
        }

        [Fact]
        public async Task Test_Officer_CRUD_And_Account_Reset()
        {
            // 1. Kiểm tra seed cán bộ
            var officers = (await _officerService.GetAllOfficersAsync()).ToList();
            Assert.True(officers.Count >= 3, $"Phải seed ít nhất 3 cán bộ (hiện tại: {officers.Count})");
            Assert.Contains(officers, o => o.OfficerCode == "CB-001");

            // 2. Thêm cán bộ mới kèm cấp tài khoản đăng nhập
            var newOfficer = new Officer
            {
                OfficerCode = "CB-099",
                FullName = "Nguyễn Văn Chiến Thắng",
                Rank = "Thiếu tá",
                Position = "Phó Tiểu đoàn trưởng",
                Unit = "Tiểu đoàn 1",
                PhoneNumber = "0911223344",
                Email = "thang.nv@qdnd.vn",
                Specialty = "Chỉ huy Tham mưu Binh chủng",
                DateOfBirth = new DateTime(1988, 5, 15),
                EnlistmentDate = new DateTime(2006, 9, 1)
            };

            var addRes = await _officerService.AddOfficerAsync(newOfficer, createLoginAccount: true, rawPassword: "OfficerPass123@");
            Assert.True(addRes.Success, addRes.Message);
            Assert.NotNull(addRes.Officer);
            Assert.NotNull(addRes.Officer.UserId);

            // 3. Đăng nhập thử bằng tài khoản vừa tạo
            var loginRes = await _authService.LoginAsync("cb-099", "OfficerPass123@");
            Assert.True(loginRes.Success, "Cán bộ đăng nhập thất bại: " + loginRes.Message);
            Assert.NotNull(loginRes.User);
            Assert.Equal("CanBo", loginRes.User.Role);

            // 4. Tìm kiếm cán bộ
            var searchRes = (await _officerService.SearchOfficersAsync("Chiến Thắng", null, null, null)).ToList();
            Assert.Single(searchRes);
            Assert.Equal("CB-099", searchRes[0].OfficerCode);

            // 5. Cập nhật thông tin cán bộ
            addRes.Officer.FullName = "Nguyễn Văn Chiến Thắng (Đã Cập Nhật)";
            addRes.Officer.Rank = "Trung tá";
            var updateRes = await _officerService.UpdateOfficerAsync(addRes.Officer);
            Assert.True(updateRes.Success, updateRes.Message);

            var updatedOfficer = await _officerService.GetOfficerByIdAsync(addRes.Officer.Id);
            Assert.NotNull(updatedOfficer);
            Assert.Equal("Trung tá", updatedOfficer.Rank);

            // 6. Đặt lại mật khẩu cán bộ
            var resetRes = await _officerService.ResetOfficerPasswordAsync(addRes.Officer.Id, "NewSecretPass456@");
            Assert.True(resetRes.Success, resetRes.Message);

            // Đăng nhập lại với mật khẩu mới
            var loginNewRes = await _authService.LoginAsync("cb-099", "NewSecretPass456@");
            Assert.True(loginNewRes.Success, "Đăng nhập với mật khẩu mới thất bại: " + loginNewRes.Message);

            // 7. Gán cán bộ vào lớp học
            var classes = (await _classService.GetAllClassesAsync()).ToList();
            if (classes.Count > 0)
            {
                var targetClass = classes[0];
                targetClass.OfficerId = addRes.Officer.Id;
                targetClass.OfficerInCharge = $"{addRes.Officer.Rank} {addRes.Officer.FullName}";
                await _classService.UpdateClassAsync(targetClass);

                var officerWithDetails = await _officerService.GetOfficerWithDetailsAsync(addRes.Officer.Id);
                Assert.NotNull(officerWithDetails);
                Assert.NotEmpty(officerWithDetails.ManagedClasses);
            }

            // 8. Xóa cán bộ -> Lớp phụ trách được gỡ liên kết
            var delRes = await _officerService.DeleteOfficerAsync(addRes.Officer.Id);
            Assert.True(delRes.Success, delRes.Message);

            var checkDeleted = await _officerService.GetOfficerByIdAsync(addRes.Officer.Id);
            Assert.Null(checkDeleted);
        }

        [Fact]
        public async Task Test_Excel_Officer_And_Catalog_Export_Import()
        {
            var officerFile = Path.Combine(Path.GetTempPath(), $"Officers_Test_{Guid.NewGuid():N}.xlsx");
            var catalogFile = Path.Combine(Path.GetTempPath(), $"Catalogs_Test_{Guid.NewGuid():N}.xlsx");

            try
            {
                // 1. Xuất & Nhập Cán bộ
                var officers = (await _officerService.GetAllOfficersAsync()).ToList();
                Assert.NotEmpty(officers);

                var expOffRes = await _excelService.ExportOfficersToExcelAsync(officers, officerFile);
                Assert.True(expOffRes.Success, expOffRes.Message);
                Assert.True(File.Exists(officerFile));

                var impOffRes = await _excelService.ImportOfficersFromExcelAsync(officerFile);
                Assert.True(impOffRes.Success, impOffRes.Message);
                Assert.NotEmpty(impOffRes.Officers);
                Assert.Equal(officers.Count, impOffRes.Officers.Count);

                // 2. Xuất & Nhập Danh mục tổ chức (4 sheets)
                var expCatRes = await _excelService.ExportCatalogsToExcelAsync(catalogFile);
                Assert.True(expCatRes.Success, expCatRes.Message);
                Assert.True(File.Exists(catalogFile));

                var impCatRes = await _excelService.ImportCatalogsFromExcelAsync(catalogFile);
                Assert.True(impCatRes.Success, impCatRes.Message);
                Assert.True(impCatRes.RanksCount >= 15);
                Assert.True(impCatRes.PositionsCount >= 12);
                Assert.True(impCatRes.UnitsCount >= 6);
                Assert.True(impCatRes.MajorsCount >= 5);
            }
            finally
            {
                if (File.Exists(officerFile)) File.Delete(officerFile);
                if (File.Exists(catalogFile)) File.Delete(catalogFile);
            }
        }

        [Fact]
        public async Task Test_Cadet_Advanced_Filtering_MultiCriteria()
        {
            var allCadets = (await _cadetService.GetAllCadetsAsync()).ToList();
            Assert.NotEmpty(allCadets);

            // 1. Filter by Gender and Unit
            var criteria1 = new CadetFilterCriteria
            {
                Gender = "Nam",
                Unit = allCadets[0].Unit
            };
            var result1 = (await _cadetService.SearchCadetsAsync(criteria1)).ToList();
            Assert.All(result1, c =>
            {
                Assert.Equal("Nam", c.Gender);
                Assert.Equal(allCadets[0].Unit, c.Unit);
            });

            // 2. Filter by Age Range
            var criteria2 = new CadetFilterCriteria
            {
                MinAge = 18,
                MaxAge = 25
            };
            var result2 = (await _cadetService.SearchCadetsAsync(criteria2)).ToList();
            var today = DateTime.Today;
            Assert.All(result2, c =>
            {
                Assert.NotNull(c.DateOfBirth);
                int age = today.Year - c.DateOfBirth.Value.Year;
                if (c.DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                Assert.True(age >= 18 && age <= 25);
            });

            // 3. Filter by HasAccount
            var criteria3 = new CadetFilterCriteria
            {
                HasAccount = true
            };
            var result3 = (await _cadetService.SearchCadetsAsync(criteria3)).ToList();
            Assert.All(result3, c => Assert.NotNull(c.UserId));
        }

        [Fact]
        public async Task Test_Officer_Advanced_Filtering_MultiCriteria()
        {
            var allOfficers = (await _officerService.GetAllOfficersAsync()).ToList();
            Assert.NotEmpty(allOfficers);

            // 1. Filter by Rank & Unit
            var sample = allOfficers[0];
            var criteria1 = new OfficerFilterCriteria
            {
                Rank = sample.Rank,
                Unit = sample.Unit
            };
            var result1 = (await _officerService.SearchOfficersAsync(criteria1)).ToList();
            Assert.All(result1, o =>
            {
                Assert.Equal(sample.Rank, o.Rank);
                Assert.Equal(sample.Unit, o.Unit);
            });

            // 2. Filter by HasAccount
            var criteria2 = new OfficerFilterCriteria
            {
                HasAccount = true
            };
            var result2 = (await _officerService.SearchOfficersAsync(criteria2)).ToList();
            Assert.All(result2, o => Assert.NotNull(o.UserId));
        }

        [Fact]
        public async Task Test_Class_Advanced_Filtering_MultiCriteria()
        {
            var allClasses = (await _classService.GetAllClassesAsync()).ToList();
            Assert.NotEmpty(allClasses);

            var sample = allClasses[0];
            // 1. Filter by Unit and Major
            var criteria1 = new ClassFilterCriteria
            {
                Unit = sample.Unit,
                Major = sample.Major
            };
            var result1 = (await _classService.SearchClassesAsync(criteria1)).ToList();
            Assert.All(result1, c =>
            {
                Assert.Equal(sample.Unit, c.Unit);
                Assert.Equal(sample.Major, c.Major);
            });

            // 2. Filter by AcademicYear
            if (!string.IsNullOrEmpty(sample.AcademicYear))
            {
                var criteria2 = new ClassFilterCriteria
                {
                    AcademicYear = sample.AcademicYear
                };
                var result2 = (await _classService.SearchClassesAsync(criteria2)).ToList();
                Assert.All(result2, c => Assert.Equal(sample.AcademicYear, c.AcademicYear));
            }
        }

        [Fact]
        public async Task Test_Subject_Advanced_Filtering_MultiCriteria()
        {
            var allSubjects = (await _subjectService.GetAllSubjectsAsync()).ToList();
            Assert.NotEmpty(allSubjects);

            // 1. Filter by Category and IsHigherBetter
            var criteria1 = new SubjectFilterCriteria
            {
                Category = allSubjects[0].Category,
                IsHigherBetter = allSubjects[0].IsHigherBetter
            };
            var result1 = (await _subjectService.SearchSubjectsAsync(criteria1)).ToList();
            Assert.All(result1, s =>
            {
                Assert.Equal(allSubjects[0].Category, s.Category);
                Assert.Equal(allSubjects[0].IsHigherBetter, s.IsHigherBetter);
            });

            // 2. Filter by SubjectCode
            var criteria2 = new SubjectFilterCriteria
            {
                SubjectCode = allSubjects[0].SubjectCode
            };
            var result2 = (await _subjectService.SearchSubjectsAsync(criteria2)).ToList();
            Assert.Contains(result2, s => s.SubjectCode == allSubjects[0].SubjectCode);
        }

        [Fact]
        public async Task Test_PhysicalExam_Advanced_Filtering_MultiCriteria()
        {
            var allRecords = (await _examService.GetAllRecordsAsync()).ToList();
            if (allRecords.Count > 0)
            {
                var sample = allRecords[0];
                var criteria1 = new PhysicalExamFilterCriteria
                {
                    SubjectId = sample.SubjectId,
                    Grade = sample.Grade
                };
                var result1 = (await _examService.SearchRecordsAsync(criteria1)).ToList();
                Assert.All(result1, r =>
                {
                    Assert.Equal(sample.SubjectId, r.SubjectId);
                    Assert.Equal(sample.Grade, r.Grade);
                });
            }
            else
            {
                var result = await _examService.SearchRecordsAsync(new PhysicalExamFilterCriteria());
                Assert.NotNull(result);
            }
        }

        [Fact]
        public async Task Test_Catalog_Advanced_Filtering_MultiCriteria()
        {
            // 1. Test Ranks with Group
            var ranks = (await _catalogService.GetAllRanksAsync()).ToList();
            Assert.NotEmpty(ranks);
            var rankCriteria = new CatalogFilterCriteria
            {
                Group = ranks[0].RankGroup
            };
            var rankResult = (await _catalogService.SearchRanksAsync(rankCriteria)).ToList();
            Assert.All(rankResult, r => Assert.Equal(ranks[0].RankGroup, r.RankGroup));

            // 2. Test Positions with Group
            var positions = (await _catalogService.GetAllPositionsAsync()).ToList();
            Assert.NotEmpty(positions);
            var posCriteria = new CatalogFilterCriteria
            {
                Group = positions[0].PositionGroup
            };
            var posResult = (await _catalogService.SearchPositionsAsync(posCriteria)).ToList();
            Assert.All(posResult, p => Assert.Equal(positions[0].PositionGroup, p.PositionGroup));

            // 3. Test Units with ParentUnit
            var units = (await _catalogService.GetAllUnitsAsync()).ToList();
            Assert.NotEmpty(units);
            var unitWithParent = units.FirstOrDefault(u => !string.IsNullOrEmpty(u.ParentUnit));
            if (unitWithParent != null)
            {
                var unitCriteria = new CatalogFilterCriteria
                {
                    ParentUnit = unitWithParent.ParentUnit
                };
                var unitResult = (await _catalogService.SearchUnitsAsync(unitCriteria)).ToList();
                Assert.All(unitResult, u => Assert.Equal(unitWithParent.ParentUnit, u.ParentUnit));
            }

            // 4. Test Majors with Department
            var majors = (await _catalogService.GetAllMajorsAsync()).ToList();
            Assert.NotEmpty(majors);
            var majorWithDept = majors.FirstOrDefault(m => !string.IsNullOrEmpty(m.Department));
            if (majorWithDept != null)
            {
                var majorCriteria = new CatalogFilterCriteria
                {
                    Department = majorWithDept.Department
                };
                var majorResult = (await _catalogService.SearchMajorsAsync(majorCriteria)).ToList();
                Assert.All(majorResult, m => Assert.Equal(majorWithDept.Department, m.Department));
            }
        }
    }
}
