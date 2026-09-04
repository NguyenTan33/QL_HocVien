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

            // Đảm bảo cột ClassId tồn tại trong bảng Cadets
            try
            {
                context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Cadets"" ADD COLUMN ""ClassId"" INTEGER NULL REFERENCES ""MilitaryClasses""(""Id"") ON DELETE SET NULL;");
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

            // 3. Seed danh mục lớp học quân đội mẫu
            if (!context.MilitaryClasses.Any())
            {
                var classes = new List<MilitaryClass>
                {
                    new MilitaryClass
                    {
                        ClassCode = "K26A",
                        ClassName = "K26A - Chỉ huy Tham mưu",
                        Unit = "Đại đội 1",
                        Major = "Chỉ huy Tham mưu Lục quân",
                        OfficerInCharge = "Thiếu tá Nguyễn Văn Bình",
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

                // Seed kết quả kiểm tra mẫu
                var xdSubject = context.Subjects.FirstOrDefault(s => s.SubjectCode == "XD");
                var c100Subject = context.Subjects.FirstOrDefault(s => s.SubjectCode == "C100");
                var cv3000Subject = context.Subjects.FirstOrDefault(s => s.SubjectCode == "CV3000");
                var firstCadet = cadets[0];

                if (xdSubject != null && c100Subject != null && cv3000Subject != null)
                {
                    context.PhysicalExamRecords.AddRange(
                        new PhysicalExamRecord
                        {
                            CadetId = firstCadet.Id,
                            SubjectId = xdSubject.Id,
                            ExamDate = DateTime.Today.AddDays(-7),
                            ExamSession = "Kiểm tra Quý 3/2026",
                            ScoreValue = 24,
                            Grade = "Giỏi",
                            Notes = "Thực hiện đúng kỹ thuật, động tác chuẩn xác"
                        },
                        new PhysicalExamRecord
                        {
                            CadetId = firstCadet.Id,
                            SubjectId = c100Subject.Id,
                            ExamDate = DateTime.Today.AddDays(-7),
                            ExamSession = "Kiểm tra Quý 3/2026",
                            ScoreValue = 13.1,
                            Grade = "Giỏi",
                            Notes = "Xuất phát nhanh, nước rút tốt"
                        },
                        new PhysicalExamRecord
                        {
                            CadetId = firstCadet.Id,
                            SubjectId = cv3000Subject.Id,
                            ExamDate = DateTime.Today.AddDays(-7),
                            ExamSession = "Kiểm tra Quý 3/2026",
                            ScoreValue = 12.3,
                            Grade = "Giỏi",
                            Notes = "Thể lực tốt, duy trì tốc độ đều"
                        }
                    );
                    context.SaveChanges();
                }
            }
        }
    }
}
