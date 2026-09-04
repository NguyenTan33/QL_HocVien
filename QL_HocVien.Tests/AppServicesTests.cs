using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Data;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;
using QL_HocVien.Services;
using Xunit;

namespace QL_HocVien.Tests
{
    public class AppServicesTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ICadetRepository _cadetRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly IPhysicalExamRepository _examRepository;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;
        private readonly ICadetService _cadetService;
        private readonly ISubjectService _subjectService;
        private readonly IEvaluationService _evaluationService;
        private readonly IPhysicalExamService _examService;
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
            _cadetRepository = new CadetRepository(_context);
            _subjectRepository = new SubjectRepository(_context);
            _examRepository = new PhysicalExamRepository(_context);
            _emailService = new EmailService();

            _authService = new AuthService(_userRepository, _cadetRepository, _context, _emailService);
            _cadetService = new CadetService(_cadetRepository);
            _subjectService = new SubjectService(_subjectRepository);
            _evaluationService = new EvaluationService();
            _examService = new PhysicalExamService(_examRepository, _subjectRepository, _evaluationService);
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
    }
}
