using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Infrastructure.Security;
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

                    CREATE TABLE IF NOT EXISTS ""CreditSubjects"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CreditSubjects"" PRIMARY KEY AUTOINCREMENT,
                        ""SubjectCode"" TEXT NOT NULL,
                        ""SubjectName"" TEXT NOT NULL,
                        ""Credits"" REAL NOT NULL,
                        ""AssessmentType"" TEXT NOT NULL,
                        ""SubjectGroup"" TEXT NULL,
                        ""IsComponent"" INTEGER NOT NULL DEFAULT 0,
                        ""Description"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CreditSubjects_SubjectCode"" ON ""CreditSubjects"" (""SubjectCode"");

                    CREATE TABLE IF NOT EXISTS ""CreditScoreRecords"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CreditScoreRecords"" PRIMARY KEY AUTOINCREMENT,
                        ""CadetId"" INTEGER NOT NULL,
                        ""CreditSubjectId"" INTEGER NOT NULL,
                        ""RegularScore"" REAL NULL,
                        ""ExamScore"" REAL NULL,
                        ""FinalScore"" REAL NOT NULL,
                        ""ExamSession"" TEXT NOT NULL,
                        ""SchoolYear"" TEXT NOT NULL DEFAULT '',
                        ""ExamDate"" TEXT NOT NULL,
                        ""Notes"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL,
                        CONSTRAINT ""FK_CreditScoreRecords_Cadets"" FOREIGN KEY (""CadetId"") REFERENCES ""Cadets"" (""Id"") ON DELETE CASCADE,
                        CONSTRAINT ""FK_CreditScoreRecords_CreditSubjects"" FOREIGN KEY (""CreditSubjectId"") REFERENCES ""CreditSubjects"" (""Id"") ON DELETE CASCADE
                    );
                ");
            }
            catch
            {
                // Bỏ qua nếu bảng đã tồn tại
            }

            // Tự động bổ sung các cột mới cho CreditSubjects nếu CSDL đã tồn tại từ trước
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""CreditSubjects"" ADD COLUMN ""SubjectGroup"" TEXT NULL;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""CreditSubjects"" ADD COLUMN ""IsComponent"" INTEGER NOT NULL DEFAULT 0;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"UPDATE ""CreditSubjects"" SET ""SubjectGroup"" = '' WHERE ""SubjectGroup"" IS NULL;");
                context.Database.ExecuteSqlRaw(@"UPDATE ""CreditSubjects"" SET ""Description"" = '' WHERE ""Description"" IS NULL;");
            }
            catch { }

            // Tạo bảng SubjectAssessmentComponents (Đợt kiểm tra / đợt thi trực thuộc môn chính)
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""SubjectAssessmentComponents"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SubjectAssessmentComponents"" PRIMARY KEY AUTOINCREMENT,
                        ""CreditSubjectId"" INTEGER NOT NULL,
                        ""ComponentName"" TEXT NOT NULL,
                        ""Credits"" REAL NOT NULL DEFAULT 1.0,
                        ""OrderIndex"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        CONSTRAINT ""FK_SubjectAssessmentComponents_CreditSubjects"" FOREIGN KEY (""CreditSubjectId"") REFERENCES ""CreditSubjects"" (""Id"") ON DELETE CASCADE
                    );
                ");
            }
            catch { }

            // Thêm cột ComponentId vào bảng CreditScoreRecords
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""CreditScoreRecords"" ADD COLUMN ""ComponentId"" INTEGER NULL REFERENCES ""SubjectAssessmentComponents""(""Id"") ON DELETE CASCADE;");
            }
            catch { }

            // Đảm bảo cột ClassId, Cohort, EnrollmentYear, AcademicYear tồn tại trong bảng Cadets
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Cadets"" ADD COLUMN ""ClassId"" INTEGER NULL REFERENCES ""MilitaryClasses""(""Id"") ON DELETE SET NULL;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Cadets"" ADD COLUMN ""Cohort"" TEXT NOT NULL DEFAULT '';");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Cadets"" ADD COLUMN ""EnrollmentYear"" INTEGER NULL;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Cadets"" ADD COLUMN ""AcademicYear"" TEXT NOT NULL DEFAULT '';");
            }
            catch { }

            // Đảm bảo cột SchoolYear tồn tại trong bảng CreditScoreRecords
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""CreditScoreRecords"" ADD COLUMN ""SchoolYear"" TEXT NOT NULL DEFAULT '';");
            }
            catch { }

            // Đảm bảo cột OfficerId tồn tại trong bảng MilitaryClasses
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""MilitaryClasses"" ADD COLUMN ""OfficerId"" INTEGER NULL REFERENCES ""Officers""(""Id"") ON DELETE SET NULL;");
            }
            catch
            {
                // Bỏ qua nếu cột đã tồn tại
            }

            // ===== NÂNG CẤP KHÓA HỌC (AcademicCohorts) =====
            // Tạo bảng AcademicCohorts (Quản lý Khóa học K75, K26...) nếu chưa có
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""AcademicCohorts"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_AcademicCohorts"" PRIMARY KEY AUTOINCREMENT,
                        ""CohortCode"" TEXT NOT NULL,
                        ""CohortName"" TEXT NOT NULL,
                        ""CohortNumber"" INTEGER NOT NULL DEFAULT 0,
                        ""EnrollmentYear"" INTEGER NULL,
                        ""GraduationYear"" INTEGER NULL,
                        ""AcademicYear"" TEXT NOT NULL DEFAULT '',
                        ""Description"" TEXT NOT NULL DEFAULT '',
                        ""CreatedAt"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AcademicCohorts_CohortCode"" ON ""AcademicCohorts"" (""CohortCode"");
                ");
            }
            catch { }

            // Thêm cột CohortId vào bảng Cadets (liên kết học viên với Khóa học)
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Cadets"" ADD COLUMN ""CohortId"" INTEGER NULL REFERENCES ""AcademicCohorts""(""Id"") ON DELETE SET NULL;");
            }
            catch { }

            // Thêm cột CohortId vào bảng MilitaryClasses (liên kết lớp học với Khóa học)
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""MilitaryClasses"" ADD COLUMN ""CohortId"" INTEGER NULL REFERENCES ""AcademicCohorts""(""Id"") ON DELETE SET NULL;");
            }
            catch { }

            // Đảm bảo bảng AccountPasskeys và các cột bản quyền tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""AccountPasskeys"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_AccountPasskeys"" PRIMARY KEY AUTOINCREMENT,
                        ""Passkey"" TEXT NOT NULL,
                        ""IsUsed"" INTEGER NOT NULL DEFAULT 0,
                        ""UsedByUsername"" TEXT NULL,
                        ""ActivatedAt"" TEXT NULL,
                        ""Remarks"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AccountPasskeys_Passkey"" ON ""AccountPasskeys"" (""Passkey"");
                ");
            }
            catch { }

            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN ""HasPasskeyActivated"" INTEGER NOT NULL DEFAULT 0;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN ""ActivatedPasskey"" TEXT NULL;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN ""PasskeyActivatedAt"" TEXT NULL;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN ""SecurityQuestion"" TEXT NULL;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN ""SecurityAnswerHash"" TEXT NULL;");
            }
            catch { }
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN ""PasswordHint"" TEXT NULL;");
            }
            catch { }

            // Khởi tạo 10 Passkey bản quyền gắn liền theo tài khoản nếu chưa có
            if (!context.AccountPasskeys.Any())
            {
                var initialPasskeys = new List<AccountPasskey>
                {
                    new AccountPasskey { Passkey = "QD-2026-HQ88-K01A", Remarks = "Mã Passkey bản quyền số 01 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-TL32-B02C", Remarks = "Mã Passkey bản quyền số 02 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-QS99-D03E", Remarks = "Mã Passkey bản quyền số 03 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-CH01-F04G", Remarks = "Mã Passkey bản quyền số 04 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-QDND-H05J", Remarks = "Mã Passkey bản quyền số 05 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-BQP8-K06L", Remarks = "Mã Passkey bản quyền số 06 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-VN26-M07N", Remarks = "Mã Passkey bản quyền số 07 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-VT32-P08R", Remarks = "Mã Passkey bản quyền số 08 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-TT32-S09T", Remarks = "Mã Passkey bản quyền số 09 (Vĩnh viễn theo tài khoản)" },
                    new AccountPasskey { Passkey = "QD-2026-QT99-X10Z", Remarks = "Mã Passkey bản quyền số 10 (Vĩnh viễn theo tài khoản)" },
                };
                context.AccountPasskeys.AddRange(initialPasskeys);
                context.SaveChanges();
            }

            // Kiểm tra xem CSDL có phải là vừa khởi tạo hoàn toàn mới hay không
            bool isBrandNewDb = !context.Users.Any();

            // 1. Seed tài khoản Admin mặc định
            if (isBrandNewDb)
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
                    IsActive = true,
                    SecurityQuestion = "Mã xác minh bí mật của đơn vị chỉ huy là gì?",
                    SecurityAnswerHash = AuthSecurityHelper.HashSecurityAnswer("quanlyhocvien"),
                    PasswordHint = "Mật khẩu mặc định do đơn vị chỉ huy bàn giao"
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
                    IsActive = true,
                    SecurityQuestion = "Tên đơn vị công tác hiện tại của đồng chí?",
                    SecurityAnswerHash = AuthSecurityHelper.HashSecurityAnswer("hocvien"),
                    PasswordHint = "Mật khẩu viết hoa chữ cái đầu và có ký tự đặc biệt"
                };

                context.Users.AddRange(adminUser, officerUser);
                context.SaveChanges();
            }
            else
            {
                // Cập nhật câu hỏi bảo mật mặc định cho tài khoản admin/canbo nếu chưa có
                var existingAdmin = context.Users.FirstOrDefault(u => u.Username == "admin");
                if (existingAdmin != null)
                {
                    if (string.IsNullOrEmpty(existingAdmin.SecurityQuestion))
                    {
                        existingAdmin.SecurityQuestion = "Mã xác minh bí mật của đơn vị chỉ huy là gì?";
                        existingAdmin.SecurityAnswerHash = AuthSecurityHelper.HashSecurityAnswer("quanlyhocvien");
                    }
                    if (string.IsNullOrEmpty(existingAdmin.PasswordHint) || existingAdmin.PasswordHint.Contains("Admin@123"))
                    {
                        existingAdmin.PasswordHint = "Mật khẩu mặc định do đơn vị chỉ huy bàn giao";
                    }
                }

                var existingOfficer = context.Users.FirstOrDefault(u => u.Username == "canbo01");
                if (existingOfficer != null)
                {
                    if (string.IsNullOrEmpty(existingOfficer.SecurityQuestion))
                    {
                        existingOfficer.SecurityQuestion = "Tên đơn vị công tác hiện tại của đồng chí?";
                        existingOfficer.SecurityAnswerHash = AuthSecurityHelper.HashSecurityAnswer("hocvien");
                    }
                    if (string.IsNullOrEmpty(existingOfficer.PasswordHint) || existingOfficer.PasswordHint.Contains("Canbo@123"))
                    {
                        existingOfficer.PasswordHint = "Mật khẩu viết hoa chữ cái đầu và có ký tự đặc biệt";
                    }
                }
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

            // 3. Quản lý danh mục không tạo sẵn dữ liệu (Cấp bậc, Chức vụ, Đơn vị, Chuyên ngành, Lớp học, Cán bộ để người dùng tự quản lý theo thực tế)
            // (Đã loại bỏ toàn bộ dữ liệu mẫu hardcode theo yêu cầu người dùng)

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

            // Seed các mốc thời gian huấn luyện và sự kiện quân sự (TrainingEvents) chuẩn 100% Celandar.png
            if (!context.TrainingEvents.Any())
            {
                var events = new List<TrainingEvent>
                {
                    new TrainingEvent
                    {
                        Title = "Kiểm tra thể lực định kỳ Quý 4/2026",
                        Category = "Kiểm tra thể lực",
                        StartDate = new DateTime(2026, 8, 28),
                        EndDate = new DateTime(2026, 8, 30),
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
                        StartDate = new DateTime(2026, 9, 6),
                        EndDate = new DateTime(2026, 9, 7),
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
                        StartDate = new DateTime(2026, 9, 11),
                        EndDate = new DateTime(2026, 9, 12),
                        TargetUnit = "Toàn đơn vị",
                        Location = "Tuyến thao trường dã ngoại",
                        Priority = "Cao",
                        Status = "Đang chuẩn bị",
                        Description = "Rèn luyện hành quân đường dài 25km, mang vác nặng theo biên chế."
                    },
                    new TrainingEvent
                    {
                        Title = "Hội thao Chiến sĩ Khỏe & Vượt vật cản K91",
                        Category = "Hội thao / Sự kiện",
                        StartDate = new DateTime(2026, 9, 20),
                        EndDate = new DateTime(2026, 9, 22),
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
                        StartDate = new DateTime(2026, 9, 28),
                        EndDate = new DateTime(2026, 9, 29),
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
            else
            {
                // Đồng bộ ngày tháng chuẩn Celandar.png cho các sự kiện mẫu
                var ev1 = context.TrainingEvents.FirstOrDefault(e => e.Title.Contains("Kiểm tra thể lực định kỳ"));
                if (ev1 != null)
                {
                    ev1.StartDate = new DateTime(2026, 8, 28);
                    ev1.EndDate = new DateTime(2026, 8, 30);
                    ev1.Status = "Đã hoàn thành";
                    ev1.Priority = "Cao";
                }
                var ev2 = context.TrainingEvents.FirstOrDefault(e => e.Title.Contains("bắn súng AK"));
                if (ev2 != null)
                {
                    ev2.StartDate = new DateTime(2026, 9, 6);
                    ev2.EndDate = new DateTime(2026, 9, 7);
                    ev2.Priority = "Khẩn cấp";
                    ev2.Status = "Đang chuẩn bị";
                }
                var ev3 = context.TrainingEvents.FirstOrDefault(e => e.Title.Contains("Hành quân rèn luyện"));
                if (ev3 != null)
                {
                    ev3.StartDate = new DateTime(2026, 9, 11);
                    ev3.EndDate = new DateTime(2026, 9, 12);
                    ev3.Location = "Tuyến thao trường dã ngoại";
                    ev3.Description = "Rèn luyện hành quân đường dài 25km, mang vác nặng theo biên chế.";
                    ev3.Priority = "Cao";
                }
                context.SaveChanges();
            }

            // 7. Seed Môn học tín chỉ mẫu
            if (!context.CreditSubjects.Any())
            {
                var creditSubjects = new List<CreditSubject>
                {
                    new CreditSubject
                    {
                        SubjectCode = "TOAN01",
                        SubjectName = "Toán cao cấp",
                        Credits = 3,
                        AssessmentType = "Kiểm tra và thi",
                        Description = "Học phần toán cơ bản dành cho sĩ quan chỉ huy tham mưu kỹ thuật."
                    },
                    new CreditSubject
                    {
                        SubjectCode = "TRIET01",
                        SubjectName = "Triết học Mác - Lênin",
                        Credits = 2,
                        AssessmentType = "Kiểm tra thường xuyên",
                        Description = "Lý luận chính trị quân sự và nền tảng tư tưởng cách mạng."
                    },
                    new CreditSubject
                    {
                        SubjectCode = "ANH01",
                        SubjectName = "Ngoại ngữ quân sự",
                        Credits = 3,
                        AssessmentType = "Kiểm tra và thi",
                        Description = "Giao tiếp và thuật ngữ quân sự đối ngoại quốc phòng."
                    },
                    new CreditSubject
                    {
                        SubjectCode = "CHIEN01",
                        SubjectName = "Chiến thuật bộ binh",
                        Credits = 3,
                        AssessmentType = "Kiểm tra và thi",
                        Description = "Kỹ chiến thuật phân đội bộ binh trong chiến đấu tiến công và phòng ngự."
                    },
                    new CreditSubject
                    {
                        SubjectCode = "PHAP01",
                        SubjectName = "Pháp luật & Điều lệnh",
                        Credits = 2,
                        AssessmentType = "Kiểm tra thường xuyên",
                        Description = "Điều lệnh quản lý bộ đội và pháp luật đại cương."
                    }
                };

                context.CreditSubjects.AddRange(creditSubjects);
                context.SaveChanges();

                // Seed điểm tín chỉ mẫu cho các học viên
                var cadets = context.Cadets.Take(15).ToList();
                var scores = new List<CreditScoreRecord>();
                var rnd = new Random(42);

                foreach (var cadet in cadets)
                {
                    foreach (var subj in creditSubjects.Take(3))
                    {
                        double regScore = Math.Round(6.5 + rnd.NextDouble() * 3.5, 1);
                        double examScore = Math.Round(6.0 + rnd.NextDouble() * 4.0, 1);
                        double finalScore = subj.AssessmentType == "Kiểm tra thường xuyên" 
                            ? regScore 
                            : Math.Round(regScore * 0.3 + examScore * 0.7, 1);

                        scores.Add(new CreditScoreRecord
                        {
                            CadetId = cadet.Id,
                            CreditSubjectId = subj.Id,
                            RegularScore = regScore,
                            ExamScore = subj.AssessmentType == "Kiểm tra thường xuyên" ? null : examScore,
                            FinalScore = finalScore,
                            ExamSession = "Học kỳ 1",
                            ExamDate = DateTime.Today.AddDays(-rnd.Next(1, 30)),
                            Notes = "Đạt yêu cầu học phần",
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                context.CreditScoreRecords.AddRange(scores);
                context.SaveChanges();
            }
        }
    }
}
