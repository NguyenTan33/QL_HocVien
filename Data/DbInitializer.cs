using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Tự động tạo cơ sở dữ liệu SQLite nếu chưa tồn tại
            context.Database.EnsureCreated();

            // Đảm bảo bảng MilitaryClasses tồn tại ngay cả khi cơ sở dữ liệu đã tạo từ phiên bản trước
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""MilitaryClasses"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MilitaryClasses"" PRIMARY KEY AUTOINCREMENT,
                        ""ClassCode"" TEXT NOT NULL,
                        ""ClassName"" TEXT NOT NULL,
                        ""Unit"" TEXT NOT NULL,
                        ""Major"" TEXT NOT NULL,
                        ""OfficerInCharge"" TEXT NOT NULL,
                        ""AcademicYear"" TEXT NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MilitaryClasses_ClassCode"" ON ""MilitaryClasses"" (""ClassCode"");
                ");
            }
            catch
            {
                // Bỏ qua nếu bảng đã tồn tại
            }

            // Đảm bảo các bảng Danh mục Tổ chức Quân sự và Cán bộ tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""MilitaryRanks"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MilitaryRanks"" PRIMARY KEY AUTOINCREMENT,
                        ""RankCode"" TEXT NOT NULL,
                        ""RankName"" TEXT NOT NULL,
                        ""RankGroup"" TEXT NOT NULL,
                        ""DisplayOrder"" INTEGER NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MilitaryRanks_RankCode"" ON ""MilitaryRanks"" (""RankCode"");

                    CREATE TABLE IF NOT EXISTS ""MilitaryPositions"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MilitaryPositions"" PRIMARY KEY AUTOINCREMENT,
                        ""PositionCode"" TEXT NOT NULL,
                        ""PositionName"" TEXT NOT NULL,
                        ""PositionGroup"" TEXT NOT NULL,
                        ""DisplayOrder"" INTEGER NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MilitaryPositions_PositionCode"" ON ""MilitaryPositions"" (""PositionCode"");

                    CREATE TABLE IF NOT EXISTS ""MilitaryUnits"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MilitaryUnits"" PRIMARY KEY AUTOINCREMENT,
                        ""UnitCode"" TEXT NOT NULL,
                        ""UnitName"" TEXT NOT NULL,
                        ""ParentUnit"" TEXT NOT NULL,
                        ""CommanderName"" TEXT NOT NULL,
                        ""ContactPhone"" TEXT NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MilitaryUnits_UnitCode"" ON ""MilitaryUnits"" (""UnitCode"");

                    CREATE TABLE IF NOT EXISTS ""MilitaryMajors"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MilitaryMajors"" PRIMARY KEY AUTOINCREMENT,
                        ""MajorCode"" TEXT NOT NULL,
                        ""MajorName"" TEXT NOT NULL,
                        ""TrainingDuration"" TEXT NOT NULL,
                        ""Department"" TEXT NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MilitaryMajors_MajorCode"" ON ""MilitaryMajors"" (""MajorCode"");

                    CREATE TABLE IF NOT EXISTS ""Officers"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Officers"" PRIMARY KEY AUTOINCREMENT,
                        ""OfficerCode"" TEXT NOT NULL,
                        ""FullName"" TEXT NOT NULL,
                        ""Rank"" TEXT NOT NULL,
                        ""Position"" TEXT NOT NULL,
                        ""Unit"" TEXT NOT NULL,
                        ""PhoneNumber"" TEXT NOT NULL,
                        ""Email"" TEXT NOT NULL,
                        ""Specialty"" TEXT NOT NULL,
                        ""DateOfBirth"" TEXT NULL,
                        ""EnlistmentDate"" TEXT NULL,
                        ""Notes"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UserId"" INTEGER NULL REFERENCES ""Users""(""Id"") ON DELETE SET NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Officers_OfficerCode"" ON ""Officers"" (""OfficerCode"");
                ");
            }
            catch
            {
                // Bỏ qua nếu bảng đã tồn tại
            }

            // Đảm bảo bảng TrainingEvents tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""TrainingEvents"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_TrainingEvents"" PRIMARY KEY AUTOINCREMENT,
                        ""Title"" TEXT NOT NULL,
                        ""Category"" TEXT NOT NULL,
                        ""StartDate"" TEXT NOT NULL,
                        ""EndDate"" TEXT NOT NULL,
                        ""TargetUnit"" TEXT NOT NULL,
                        ""Location"" TEXT NOT NULL,
                        ""Priority"" TEXT NOT NULL,
                        ""Status"" TEXT NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );
                ");
            }
            catch
            {
                // Bỏ qua nếu bảng đã tồn tại
            }

            // Đảm bảo cột ClassId tồn tại trong bảng Cadets
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Cadets"" ADD COLUMN ""ClassId"" INTEGER NULL REFERENCES ""MilitaryClasses""(""Id"") ON DELETE SET NULL;");
            }
            catch
            {
                // Bỏ qua nếu cột đã tồn tại
            }

            // Đảm bảo cột OfficerId tồn tại trong bảng MilitaryClasses
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""MilitaryClasses"" ADD COLUMN ""OfficerId"" INTEGER NULL REFERENCES ""Officers""(""Id"") ON DELETE SET NULL;");
            }
            catch
            {
                // Bỏ qua nếu cột đã tồn tại
            }

            // 1. Seed tài khoản Admin mặc định
            if (!context.Users.Any())
            {
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Quản Trị Viên Hệ Thống",
                    PhoneNumber = "0988888888",
                    Email = "admin@mod.gov.vn",
                    Role = "Admin",
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                var officerUser = new User
                {
                    Username = "canbo01",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Canbo@123"),
                    FullName = "Đại úy Trần Văn Quân",
                    PhoneNumber = "0912345678",
                    Email = "quan.tv@mod.gov.vn",
                    Role = "CanBo",
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                context.Users.AddRange(adminUser, officerUser);
                context.SaveChanges();
            }

            // 2. Seed danh mục môn rèn luyện thể lực theo Thông tư 32/2009/TTLT-BQP-BVHTTDL
            if (!context.Subjects.Any())
            {
                var subjects = new List<Subject>
                {
                    new Subject
                    {
                        SubjectCode = "XD",
                        SubjectName = "Co tay xà đơn",
                        Category = "Sức mạnh",
                        Unit = "lần",
                        Description = "Kiểm tra sức mạnh nhóm cơ chi trên và lưng xô.",
                        ExcellentThreshold = 23,
                        GoodThreshold = 19,
                        PassThreshold = 15,
                        IsHigherBetter = true
                    },
                    new Subject
                    {
                        SubjectCode = "XK",
                        SubjectName = "Chống tay xà kép",
                        Category = "Sức mạnh",
                        Unit = "lần",
                        Description = "Kiểm tra sức mạnh cơ tay sau, ngực và vai.",
                        ExcellentThreshold = 23,
                        GoodThreshold = 20,
                        PassThreshold = 17,
                        IsHigherBetter = true
                    },
                    new Subject
                    {
                        SubjectCode = "C100",
                        SubjectName = "Chạy 100m",
                        Category = "Sức nhanh",
                        Unit = "giây",
                        Description = "Kiểm tra tốc độ chạy bứt phá cự ly ngắn.",
                        ExcellentThreshold = 13.3,
                        GoodThreshold = 13.6,
                        PassThreshold = 14.0,
                        IsHigherBetter = false
                    },
                    new Subject
                    {
                        SubjectCode = "CV3000",
                        SubjectName = "Chạy vũ trang 3000m",
                        Category = "Sức bền",
                        Unit = "phút",
                        Description = "Kiểm tra sức bền rèn luyện dẻo dai toàn diện (mang súng tiểu liên AK).",
                        ExcellentThreshold = 12.5,  // 12 phút 30 giây
                        GoodThreshold = 13.16,     // 13 phút 10 giây
                        PassThreshold = 13.83,     // 13 phút 50 giây
                        IsHigherBetter = false
                    },
                    new Subject
                    {
                        SubjectCode = "BE",
                        SubjectName = "Bơi ếch / Bơi tự do (3 phút)",
                        Category = "Bơi tự do",
                        Unit = "mét",
                        Description = "Kiểm tra khả năng vượt chướng ngại vật mặt nước trong 3 phút.",
                        ExcellentThreshold = 100,
                        GoodThreshold = 80,
                        PassThreshold = 50,
                        IsHigherBetter = true
                    },
                    new Subject
                    {
                        SubjectCode = "VVC91",
                        SubjectName = "Vượt vật cản K91",
                        Category = "Bài tập tổng hợp",
                        Unit = "giây",
                        Description = "Kiểm tra kỹ năng vượt dải vật cản chiến đấu chuẩn K91.",
                        ExcellentThreshold = 53.0,
                        GoodThreshold = 58.0,
                        PassThreshold = 63.0,
                        IsHigherBetter = false
                    },
                    new Subject
                    {
                        SubjectCode = "C50X2",
                        SubjectName = "Chạy 50m x 2",
                        Category = "Sức nhanh",
                        Unit = "giây",
                        Description = "Kiểm tra độ linh hoạt và phản xạ chuyển hướng nhanh.",
                        ExcellentThreshold = 16.5,
                        GoodThreshold = 16.9,
                        PassThreshold = 17.4,
                        IsHigherBetter = false
                    },
                    new Subject
                    {
                        SubjectCode = "NXA",
                        SubjectName = "Nhảy xa có đà",
                        Category = "Sức mạnh",
                        Unit = "mét",
                        Description = "Kiểm tra sức bật và sự phối hợp vận động.",
                        ExcellentThreshold = 5.0,
                        GoodThreshold = 4.7,
                        PassThreshold = 4.4,
                        IsHigherBetter = true
                    }
                };

                context.Subjects.AddRange(subjects);
                context.SaveChanges();
            }

            // 3. Seed Danh mục Cấp bậc quân sự
            if (!context.MilitaryRanks.Any())
            {
                var ranks = new List<MilitaryRank>
                {
                    new() { RankCode = "BN", RankName = "Binh nhì", RankGroup = "Hạ sĩ quan - Binh sĩ", DisplayOrder = 1, Description = "Cấp bậc chiến sĩ mới" },
                    new() { RankCode = "BN1", RankName = "Binh nhất", RankGroup = "Hạ sĩ quan - Binh sĩ", DisplayOrder = 2, Description = "Chiến sĩ đủ niên hạn" },
                    new() { RankCode = "HS", RankName = "Hạ sĩ", RankGroup = "Hạ sĩ quan - Binh sĩ", DisplayOrder = 3, Description = "Phó tiểu đội trưởng" },
                    new() { RankCode = "TS", RankName = "Trung sĩ", RankGroup = "Hạ sĩ quan - Binh sĩ", DisplayOrder = 4, Description = "Tiểu đội trưởng" },
                    new() { RankCode = "ThS", RankName = "Thượng sĩ", RankGroup = "Hạ sĩ quan - Binh sĩ", DisplayOrder = 5, Description = "Học viên năm cuối / Trung đội phó" },
                    new() { RankCode = "CU", RankName = "Chuẩn úy", RankGroup = "Sĩ quan cấp Úy", DisplayOrder = 6, Description = "Quân nhân chuyên nghiệp" },
                    new() { RankCode = "TU", RankName = "Thiếu úy", RankGroup = "Sĩ quan cấp Úy", DisplayOrder = 7, Description = "Trung đội trưởng" },
                    new() { RankCode = "TrU", RankName = "Trung úy", RankGroup = "Sĩ quan cấp Úy", DisplayOrder = 8, Description = "Đại đội phó / Chính trị viên phó" },
                    new() { RankCode = "ThgU", RankName = "Thượng úy", RankGroup = "Sĩ quan cấp Úy", DisplayOrder = 9, Description = "Đại đội trưởng / Chính trị viên" },
                    new() { RankCode = "DU", RankName = "Đại úy", RankGroup = "Sĩ quan cấp Úy", DisplayOrder = 10, Description = "Tiểu đoàn phó / Trợ lý cơ quan" },
                    new() { RankCode = "ThTa", RankName = "Thiếu tá", RankGroup = "Sĩ quan cấp Tá", DisplayOrder = 11, Description = "Tiểu đoàn trưởng / Chủ nhiệm khoa" },
                    new() { RankCode = "TrTa", RankName = "Trung tá", RankGroup = "Sĩ quan cấp Tá", DisplayOrder = 12, Description = "Trung đoàn phó / Trưởng ban" },
                    new() { RankCode = "ThgTa", RankName = "Thượng tá", RankGroup = "Sĩ quan cấp Tá", DisplayOrder = 13, Description = "Trung đoàn trưởng / Phó viện trưởng" },
                    new() { RankCode = "DTa", RankName = "Đại tá", RankGroup = "Sĩ quan cấp Tá", DisplayOrder = 14, Description = "Sư đoàn trưởng / Viện trưởng" },
                    new() { RankCode = "ThTuong", RankName = "Thiếu tướng", RankGroup = "Sĩ quan cấp Tướng", DisplayOrder = 15, Description = "Tư lệnh / Giám đốc học viện" }
                };
                context.MilitaryRanks.AddRange(ranks);
                context.SaveChanges();
            }

            // 4. Seed Danh mục Chức vụ quân sự
            if (!context.MilitaryPositions.Any())
            {
                var positions = new List<MilitaryPosition>
                {
                    new() { PositionCode = "HV", PositionName = "Học viên", PositionGroup = "Học viên / Chiến sĩ", DisplayOrder = 1, Description = "Học viên đào tạo cơ bản" },
                    new() { PositionCode = "CS", PositionName = "Chiến sĩ", PositionGroup = "Học viên / Chiến sĩ", DisplayOrder = 2, Description = "Chiến sĩ nghĩa vụ" },
                    new() { PositionCode = "TDT", PositionName = "Tiểu đội trưởng", PositionGroup = "Cán bộ Phân đội", DisplayOrder = 3, Description = "Chỉ huy tiểu đội" },
                    new() { PositionCode = "LP", PositionName = "Lớp phó", PositionGroup = "Cán bộ Phân đội", DisplayOrder = 4, Description = "Quản lý nề nếp, học tập của lớp" },
                    new() { PositionCode = "LT", PositionName = "Lớp trưởng", PositionGroup = "Cán bộ Phân đội", DisplayOrder = 5, Description = "Chỉ huy toàn diện lớp học viên" },
                    new() { PositionCode = "CTP", PositionName = "Chính trị viên phó", PositionGroup = "Cán bộ Chỉ huy", DisplayOrder = 6, Description = "Phụ trách công tác tư tưởng, thanh niên" },
                    new() { PositionCode = "CTT", PositionName = "Chính trị viên", PositionGroup = "Cán bộ Chỉ huy", DisplayOrder = 7, Description = "Chủ trì công tác Đảng, công tác chính trị" },
                    new() { PositionCode = "DDP", PositionName = "Đại đội phó", PositionGroup = "Cán bộ Chỉ huy", DisplayOrder = 8, Description = "Phụ trách huấn luyện quân sự, thể lực" },
                    new() { PositionCode = "DDT", PositionName = "Đại đội trưởng", PositionGroup = "Cán bộ Chỉ huy", DisplayOrder = 9, Description = "Chỉ huy quân sự đại đội" },
                    new() { PositionCode = "CBQL", PositionName = "Cán bộ chủ nhiệm lớp", PositionGroup = "Cán bộ Phân đội", DisplayOrder = 10, Description = "Sĩ quan trực tiếp theo dõi, quản lý lớp" },
                    new() { PositionCode = "GV", PositionName = "Giảng viên quân sự", PositionGroup = "Cán bộ Giảng dạy", DisplayOrder = 11, Description = "Giảng viên khoa chiến thuật, kỹ thuật" },
                    new() { PositionCode = "TLHL", PositionName = "Trợ lý huấn luyện thể lực", PositionGroup = "Cán bộ Phân đội", DisplayOrder = 12, Description = "Kiểm tra, theo dõi rèn luyện thể lực TT32" }
                };
                context.MilitaryPositions.AddRange(positions);
                context.SaveChanges();
            }

            // 5. Seed Danh mục Đơn vị quân đội
            if (!context.MilitaryUnits.Any())
            {
                var units = new List<MilitaryUnit>
                {
                    new() { UnitCode = "c1", UnitName = "Đại đội 1", ParentUnit = "Tiểu đoàn 1", CommanderName = "Đại úy Nguyễn Văn Hùng", ContactPhone = "0981111001", Description = "Đại đội đào tạo Chỉ huy Tham mưu" },
                    new() { UnitCode = "c2", UnitName = "Đại đội 2", ParentUnit = "Tiểu đoàn 1", CommanderName = "Đại úy Trần Văn Quân", ContactPhone = "0981111002", Description = "Đại đội đào tạo Hậu cần Quân sự" },
                    new() { UnitCode = "c3", UnitName = "Đại đội 3", ParentUnit = "Tiểu đoàn 1", CommanderName = "Thiếu tá Lê Hồng Sơn", ContactPhone = "0981111003", Description = "Đại đội đào tạo Kỹ thuật Quân sự" },
                    new() { UnitCode = "c4", UnitName = "Đại đội 4", ParentUnit = "Tiểu đoàn 1", CommanderName = "Đại úy Phạm Văn Toàn", ContactPhone = "0981111004", Description = "Đại đội đào tạo Trinh sát Đặc nhiệm" },
                    new() { UnitCode = "d1", UnitName = "Tiểu đoàn 1", ParentUnit = "Trung đoàn 1", CommanderName = "Trung tá Hoàng Minh Tuấn", ContactPhone = "0981111010", Description = "Tiểu đoàn quản lý khóa K26" },
                    new() { UnitCode = "d2", UnitName = "Tiểu đoàn 2", ParentUnit = "Trung đoàn 1", CommanderName = "Trung tá Vũ Đình Cường", ContactPhone = "0981111020", Description = "Tiểu đoàn quản lý khóa K27" }
                };
                context.MilitaryUnits.AddRange(units);
                context.SaveChanges();
            }

            // 6. Seed Danh mục Chuyên ngành đào tạo
            if (!context.MilitaryMajors.Any())
            {
                var majors = new List<MilitaryMajor>
                {
                    new() { MajorCode = "CHTM", MajorName = "Chỉ huy Tham mưu Lục quân", TrainingDuration = "4 năm", Department = "Khoa Chiến thuật", Description = "Đào tạo sĩ quan chỉ huy tham mưu cấp phân đội" },
                    new() { MajorCode = "HCQS", MajorName = "Hậu cần Quân sự", TrainingDuration = "4 năm", Department = "Khoa Hậu cần", Description = "Đào tạo nghiệp vụ quân nhu, xăng dầu, doanh trại và vận tải" },
                    new() { MajorCode = "KTQS", MajorName = "Kỹ thuật Vũ khí - Khí tài", TrainingDuration = "4.5 năm", Department = "Khoa Kỹ thuật", Description = "Đào tạo kỹ sư chỉ huy kỹ thuật khai thác bảo đảm vũ khí" },
                    new() { MajorCode = "TSDN", MajorName = "Trinh sát Đặc nhiệm", TrainingDuration = "4 năm", Department = "Khoa Trinh sát", Description = "Đào tạo sĩ quan trinh sát cơ động, đặc nhiệm luồn sâu" },
                    new() { MajorCode = "TTLN", MajorName = "Thông tin Liên lạc", TrainingDuration = "4 năm", Department = "Khoa Thông tin", Description = "Đào tạo chỉ huy bảo đảm mạng lưới thông tin tác chiến" }
                };
                context.MilitaryMajors.AddRange(majors);
                context.SaveChanges();
            }

            // 7. Seed Cán bộ quân sự mẫu
            if (!context.Officers.Any())
            {
                var canbo01User = context.Users.FirstOrDefault(u => u.Username == "canbo01");
                var officers = new List<Officer>
                {
                    new()
                    {
                        OfficerCode = "CB-001",
                        FullName = "Nguyễn Văn Bình",
                        Rank = "Thiếu tá",
                        Position = "Chính trị viên",
                        Unit = "Đại đội 1",
                        PhoneNumber = "0981234001",
                        Email = "binh.nv@mod.gov.vn",
                        Specialty = "Công tác Đảng, chính trị & Quản lý học viên",
                        DateOfBirth = new DateTime(1985, 4, 12),
                        EnlistmentDate = new DateTime(2003, 9, 1),
                        Notes = "Cán bộ chủ nhiệm phụ trách lớp K26A"
                    },
                    new()
                    {
                        OfficerCode = "CB-002",
                        FullName = "Trần Văn Quân",
                        Rank = "Đại úy",
                        Position = "Đại đội trưởng",
                        Unit = "Đại đội 2",
                        PhoneNumber = "0912345678",
                        Email = "quan.tv@mod.gov.vn",
                        Specialty = "Chỉ huy tham mưu & Huấn luyện thể lực",
                        DateOfBirth = new DateTime(1988, 7, 24),
                        EnlistmentDate = new DateTime(2006, 9, 1),
                        UserId = canbo01User?.Id,
                        Notes = "Cán bộ chủ nhiệm phụ trách lớp K26B"
                    },
                    new()
                    {
                        OfficerCode = "CB-003",
                        FullName = "Lê Hồng Sơn",
                        Rank = "Thiếu tá",
                        Position = "Trợ lý Huấn luyện",
                        Unit = "Đại đội 3",
                        PhoneNumber = "0981234003",
                        Email = "son.lh@mod.gov.vn",
                        Specialty = "Khai thác bảo đảm vũ khí & Kiểm tra thể lực",
                        DateOfBirth = new DateTime(1986, 11, 30),
                        EnlistmentDate = new DateTime(2004, 9, 1),
                        Notes = "Cán bộ phụ trách lớp K26C"
                    }
                };
                context.Officers.AddRange(officers);
                context.SaveChanges();
            }

            // 8. Seed danh mục lớp học quân đội mẫu
            if (!context.MilitaryClasses.Any())
            {
                var offBinh = context.Officers.FirstOrDefault(o => o.OfficerCode == "CB-001");
                var offQuan = context.Officers.FirstOrDefault(o => o.OfficerCode == "CB-002");
                var offSon = context.Officers.FirstOrDefault(o => o.OfficerCode == "CB-003");

                var classes = new List<MilitaryClass>
                {
                    new MilitaryClass
                    {
                        ClassCode = "K26A",
                        ClassName = "K26A - Chỉ huy Tham mưu",
                        Unit = "Đại đội 1",
                        Major = "Chỉ huy Tham mưu Lục quân",
                        OfficerInCharge = "Thiếu tá Nguyễn Văn Bình",
                        OfficerId = offBinh?.Id,
                        AcademicYear = "2023 - 2027",
                        Description = "Đào tạo sĩ quan chỉ huy tham mưu cấp phân đội"
                    },
                    new MilitaryClass
                    {
                        ClassCode = "K26B",
                        ClassName = "K26B - Hậu cần Quân sự",
                        Unit = "Đại đội 2",
                        Major = "Hậu cần Quân sự",
                        OfficerInCharge = "Đại úy Trần Văn Quân",
                        OfficerId = offQuan?.Id,
                        AcademicYear = "2023 - 2027",
                        Description = "Đào tạo chuyên môn đảm bảo hậu cần, quân nhu, xăng dầu"
                    },
                    new MilitaryClass
                    {
                        ClassCode = "K26C",
                        ClassName = "K26C - Kỹ thuật Quân sự",
                        Unit = "Đại đội 3",
                        Major = "Kỹ thuật Vũ khí - Khí tài",
                        OfficerInCharge = "Thiếu tá Lê Hồng Sơn",
                        OfficerId = offSon?.Id,
                        AcademicYear = "2023 - 2027",
                        Description = "Đào tạo kỹ sư chỉ huy kỹ thuật khai thác bảo đảm vũ khí"
                    }
                };

                context.MilitaryClasses.AddRange(classes);
                context.SaveChanges();
            }

            // Đồng bộ liên kết lớp cho các học viên đã có từ trước
            try
            {
                var unlinkedCadets = context.Cadets.Where(c => c.ClassId == null).ToList();
                if (unlinkedCadets.Any())
                {
                    var allClasses = context.MilitaryClasses.ToList();
                    foreach (var c in unlinkedCadets)
                    {
                        var matchedClass = allClasses.FirstOrDefault(mc => 
                            mc.ClassName.Equals(c.ClassName, StringComparison.OrdinalIgnoreCase) ||
                            mc.ClassCode.Equals(c.ClassName, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(c.ClassName) && c.ClassName.StartsWith(mc.ClassCode, StringComparison.OrdinalIgnoreCase)));
                        if (matchedClass != null)
                        {
                            c.ClassId = matchedClass.Id;
                        }
                    }
                    context.SaveChanges();
                }
            }
            catch
            {
                // Bỏ qua nếu có lỗi
            }

            // 4. Seed học viên mẫu
            if (!context.Cadets.Any())
            {
                var classK26A = context.MilitaryClasses.FirstOrDefault(c => c.ClassCode == "K26A");
                var classK26B = context.MilitaryClasses.FirstOrDefault(c => c.ClassCode == "K26B");

                var cadets = new List<Cadet>
                {
                    new Cadet
                    {
                        CadetCode = "HV-2026-001",
                        FullName = "Nguyễn Văn An",
                        Rank = "Trung sĩ",
                        Position = "Lớp trưởng",
                        Unit = "Đại đội 1",
                        ClassId = classK26A?.Id,
                        ClassName = classK26A?.ClassName ?? "K26A - Chỉ huy Tham mưu",
                        PhoneNumber = "0971000001",
                        Email = "an.nv@hocvien.edu.vn",
                        DateOfBirth = new DateTime(2003, 5, 15),
                        Age = 23,
                        Gender = "Nam"
                    },
                    new Cadet
                    {
                        CadetCode = "HV-2026-002",
                        FullName = "Lê Thị Bích",
                        Rank = "Hạ sĩ",
                        Position = "Lớp phó",
                        Unit = "Đại đội 1",
                        ClassId = classK26A?.Id,
                        ClassName = classK26A?.ClassName ?? "K26A - Chỉ huy Tham mưu",
                        PhoneNumber = "0971000002",
                        Email = "bich.lt@hocvien.edu.vn",
                        DateOfBirth = new DateTime(2004, 8, 20),
                        Age = 22,
                        Gender = "Nữ"
                    },
                    new Cadet
                    {
                        CadetCode = "HV-2026-003",
                        FullName = "Phạm Hoàng Dũng",
                        Rank = "Binh nhất",
                        Position = "Chiến sĩ",
                        Unit = "Đại đội 2",
                        ClassId = classK26B?.Id,
                        ClassName = classK26B?.ClassName ?? "K26B - Hậu cần Quân sự",
                        PhoneNumber = "0971000003",
                        Email = "dung.ph@hocvien.edu.vn",
                        DateOfBirth = new DateTime(2004, 1, 10),
                        Age = 22,
                        Gender = "Nam"
                    },
                    new Cadet
                    {
                        CadetCode = "HV-2026-004",
                        FullName = "Trần Minh Quang",
                        Rank = "Binh nhì",
                        Position = "Chiến sĩ",
                        Unit = "Đại đội 2",
                        ClassId = classK26B?.Id,
                        ClassName = classK26B?.ClassName ?? "K26B - Hậu cần Quân sự",
                        PhoneNumber = "0971000004",
                        Email = "quang.tm@hocvien.edu.vn",
                        DateOfBirth = new DateTime(2005, 11, 28),
                        Age = 21,
                        Gender = "Nam"
                    }
                };

                context.Cadets.AddRange(cadets);
                context.SaveChanges();
            }

            // Seed kết quả kiểm tra 2 đợt (Quý 3/2026 và Quý 4/2026) để phục vụ so sánh và phân tích
            var xdSub = context.Subjects.FirstOrDefault(s => s.SubjectCode == "XD");
            var c100Sub = context.Subjects.FirstOrDefault(s => s.SubjectCode == "C100");
            var cv3000Sub = context.Subjects.FirstOrDefault(s => s.SubjectCode == "CV3000");

            if (xdSub != null && c100Sub != null && cv3000Sub != null)
            {
                var allCadets = context.Cadets.Take(4).ToList();
                if (allCadets.Count >= 4 && !context.PhysicalExamRecords.Any(r => r.ExamSession == "Kiểm tra Quý 4/2026"))
                {
                    var c1 = allCadets[0]; // Nguyễn Văn An (c1 - K26A) -> TĂNG TRƯỞNG (▲)
                    var c2 = allCadets[1]; // Lê Thị Bích (c1 - K26A) -> TĂNG TRƯỞNG (▲)
                    var c3 = allCadets[2]; // Phạm Hoàng Dũng (c2 - K26B) -> GIỮ NGUYÊN (—)
                    var c4 = allCadets[3]; // Trần Minh Quang (c2 - K26B) -> THỤT LÙI (▼)

                    var examRecords = new List<PhysicalExamRecord>();

                    // Nếu chưa có Quý 3 thì thêm Quý 3
                    if (!context.PhysicalExamRecords.Any(r => r.ExamSession == "Kiểm tra Quý 3/2026"))
                    {
                        var d3 = DateTime.Today.AddDays(-60);
                        // Cadet 1 - Quý 3
                        examRecords.Add(new PhysicalExamRecord { CadetId = c1.Id, SubjectId = xdSub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 24, Grade = "Giỏi", Notes = "Động tác chuẩn" });
                        examRecords.Add(new PhysicalExamRecord { CadetId = c1.Id, SubjectId = c100Sub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 13.1, Grade = "Giỏi", Notes = "Nước rút tốt" });
                        examRecords.Add(new PhysicalExamRecord { CadetId = c1.Id, SubjectId = cv3000Sub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 12.3, Grade = "Giỏi", Notes = "Duy trì tốc độ đều" });

                        // Cadet 2 - Quý 3
                        examRecords.Add(new PhysicalExamRecord { CadetId = c2.Id, SubjectId = xdSub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 16, Grade = "Đạt", Notes = "Cần tăng sức kéo xà" });
                        examRecords.Add(new PhysicalExamRecord { CadetId = c2.Id, SubjectId = c100Sub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 14.8, Grade = "Khá", Notes = "Tốc độ trung bình" });
                        examRecords.Add(new PhysicalExamRecord { CadetId = c2.Id, SubjectId = cv3000Sub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 14.5, Grade = "Đạt", Notes = "Hơi hụt hơi vòng cuối" });

                        // Cadet 3 - Quý 3
                        examRecords.Add(new PhysicalExamRecord { CadetId = c3.Id, SubjectId = xdSub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 20, Grade = "Khá", Notes = "Kỹ thuật ổn định" });
                        examRecords.Add(new PhysicalExamRecord { CadetId = c3.Id, SubjectId = c100Sub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 13.5, Grade = "Giỏi", Notes = "Xuất phát nhanh" });
                        examRecords.Add(new PhysicalExamRecord { CadetId = c3.Id, SubjectId = cv3000Sub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 13.0, Grade = "Khá", Notes = "Thể lực tốt" });

                        // Cadet 4 - Quý 3
                        examRecords.Add(new PhysicalExamRecord { CadetId = c4.Id, SubjectId = xdSub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 18, Grade = "Khá", Notes = "Đạt yêu cầu" });
                        examRecords.Add(new PhysicalExamRecord { CadetId = c4.Id, SubjectId = c100Sub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 13.8, Grade = "Khá", Notes = "Khá tốt" });
                        examRecords.Add(new PhysicalExamRecord { CadetId = c4.Id, SubjectId = cv3000Sub.Id, ExamDate = d3, ExamSession = "Kiểm tra Quý 3/2026", ScoreValue = 13.5, Grade = "Khá", Notes = "Duy trì được" });
                    }

                    // Thêm Quý 4/2026:
                    var d4 = DateTime.Today.AddDays(-7);
                    // Cadet 1: XD 24 -> 26 (Tăng), C100 13.1 -> 12.8 (Tăng - thời gian giảm), CV3000 12.3 -> 12.0 (Tăng) => TĂNG TRƯỞNG (▲)
                    examRecords.Add(new PhysicalExamRecord { CadetId = c1.Id, SubjectId = xdSub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 26, Grade = "Xuất sắc", Notes = "Tiến bộ vượt bậc" });
                    examRecords.Add(new PhysicalExamRecord { CadetId = c1.Id, SubjectId = c100Sub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 12.8, Grade = "Xuất sắc", Notes = "Tốc độ bứt phá" });
                    examRecords.Add(new PhysicalExamRecord { CadetId = c1.Id, SubjectId = cv3000Sub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 12.0, Grade = "Xuất sắc", Notes = "Sức bền tuyệt vời" });

                    // Cadet 2: XD 16 -> 19 (Tăng), C100 14.8 -> 14.8 (Giữ), CV3000 14.5 -> 13.8 (Tăng) => TĂNG TRƯỞNG (▲)
                    examRecords.Add(new PhysicalExamRecord { CadetId = c2.Id, SubjectId = xdSub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 19, Grade = "Khá", Notes = "Tiến bộ rõ rệt xà đơn" });
                    examRecords.Add(new PhysicalExamRecord { CadetId = c2.Id, SubjectId = c100Sub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 14.8, Grade = "Khá", Notes = "Giữ vững phong độ" });
                    examRecords.Add(new PhysicalExamRecord { CadetId = c2.Id, SubjectId = cv3000Sub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 13.8, Grade = "Khá", Notes = "Cải thiện sức bền chạy dài" });

                    // Cadet 3: XD 20 -> 20 (Giữ), C100 13.5 -> 13.5 (Giữ), CV3000 13.0 -> 13.0 (Giữ) => GIỮ NGUYÊN (—)
                    examRecords.Add(new PhysicalExamRecord { CadetId = c3.Id, SubjectId = xdSub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 20, Grade = "Khá", Notes = "Phong độ ổn định" });
                    examRecords.Add(new PhysicalExamRecord { CadetId = c3.Id, SubjectId = c100Sub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 13.5, Grade = "Giỏi", Notes = "Duy trì thành tích tốt" });
                    examRecords.Add(new PhysicalExamRecord { CadetId = c3.Id, SubjectId = cv3000Sub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 13.0, Grade = "Khá", Notes = "Thể lực đều" });

                    // Cadet 4: XD 18 -> 14 (Giảm), C100 13.8 -> 14.5 (Giảm - thời gian tăng), CV3000 13.5 -> 14.8 (Giảm) => THỤT LÙI (▼)
                    examRecords.Add(new PhysicalExamRecord { CadetId = c4.Id, SubjectId = xdSub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 14, Grade = "Không đạt", Notes = "Sút giảm thể lực, cần tăng cường rèn luyện" });
                    examRecords.Add(new PhysicalExamRecord { CadetId = c4.Id, SubjectId = c100Sub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 14.5, Grade = "Đạt", Notes = "Nước rút giảm sút" });
                    examRecords.Add(new PhysicalExamRecord { CadetId = c4.Id, SubjectId = cv3000Sub.Id, ExamDate = d4, ExamSession = "Kiểm tra Quý 4/2026", ScoreValue = 14.8, Grade = "Đạt", Notes = "Hụt hơi ở đoạn dốc" });

                    context.PhysicalExamRecords.AddRange(examRecords);
                    context.SaveChanges();
                }
            }

            // Seed các mốc thời gian huấn luyện và sự kiện quân sự (TrainingEvents)
            if (!context.TrainingEvents.Any())
            {
                var events = new List<TrainingEvent>
                {
                    new TrainingEvent
                    {
                        Title = "Kiểm tra thể lực định kỳ Quý 4/2026",
                        Category = "Kiểm tra thể lực",
                        StartDate = DateTime.Today.AddDays(-7),
                        EndDate = DateTime.Today.AddDays(-5),
                        TargetUnit = "Toàn đơn vị",
                        Location = "Bãi tập thể lực & Thao trường 1",
                        Priority = "Cao",
                        Status = "Đã hoàn thành",
                        Description = "Kiểm tra 4 môn thể lực tiêu chuẩn TT 32/2009 cho toàn thể học viên."
                    },
                    new TrainingEvent
                    {
                        Title = "Kiểm tra bắn súng AK bài 1 (Ban ngày)",
                        Category = "Thi cử quân sự",
                        StartDate = DateTime.Today.AddDays(2),
                        EndDate = DateTime.Today.AddDays(3),
                        TargetUnit = "Đại đội 1",
                        Location = "Trường bắn TB1",
                        Priority = "Khẩn cấp",
                        Status = "Đang chuẩn bị",
                        Description = "Kiểm tra bắn mục tiêu bia số 4, 7, 8 ẩn hiện ban ngày cự ly 100m."
                    },
                    new TrainingEvent
                    {
                        Title = "Hành quân rèn luyện dã ngoại 25km mang vác nặng",
                        Category = "Tập luyện / Rèn luyện",
                        StartDate = DateTime.Today.AddDays(7),
                        EndDate = DateTime.Today.AddDays(8),
                        TargetUnit = "Toàn đơn vị",
                        Location = "Tuyến thao trường dã ngoại Ba Vì",
                        Priority = "Cao",
                        Status = "Đang chuẩn bị",
                        Description = "Hành quân rèn sức bền, mang vũ khí trang bị 25kg, vượt dốc và sông suối."
                    },
                    new TrainingEvent
                    {
                        Title = "Hội thao Chiến sĩ Khỏe & Vượt vật cản K91",
                        Category = "Hội thao / Sự kiện",
                        StartDate = DateTime.Today.AddDays(15),
                        EndDate = DateTime.Today.AddDays(17),
                        TargetUnit = "Toàn đơn vị",
                        Location = "Bãi vật cản K91 & Sân vận động trung tâm",
                        Priority = "Bình thường",
                        Status = "Đang chuẩn bị",
                        Description = "Hội thao thể thao quân sự chào mừng ngày truyền thống học viện."
                    },
                    new TrainingEvent
                    {
                        Title = "Sát hạch bơi vũ trang 100m vượt sông ngòi",
                        Category = "Kiểm tra thể lực",
                        StartDate = DateTime.Today.AddDays(22),
                        EndDate = DateTime.Today.AddDays(23),
                        TargetUnit = "Đại đội 2",
                        Location = "Bể bơi quân sự & Khu vực hồ thao trường",
                        Priority = "Bình thường",
                        Status = "Đang chuẩn bị",
                        Description = "Kiểm tra bơi bao gói vũ khí trang bị đảm bảo an toàn tuyệt đối."
                    }
                };

                context.TrainingEvents.AddRange(events);
                context.SaveChanges();
            }
        }
    }
}
