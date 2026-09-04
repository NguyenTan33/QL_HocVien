using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<MilitaryClass> MilitaryClasses => Set<MilitaryClass>();
        public DbSet<Cadet> Cadets => Set<Cadet>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<PhysicalExamRecord> PhysicalExamRecords => Set<PhysicalExamRecord>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<MilitaryRank> MilitaryRanks => Set<MilitaryRank>();
        public DbSet<MilitaryPosition> MilitaryPositions => Set<MilitaryPosition>();
        public DbSet<MilitaryUnit> MilitaryUnits => Set<MilitaryUnit>();
        public DbSet<MilitaryMajor> MilitaryMajors => Set<MilitaryMajor>();
        public DbSet<Officer> Officers => Set<Officer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            });

            // Cấu hình MilitaryClass
            modelBuilder.Entity<MilitaryClass>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ClassCode).IsUnique();
                entity.Property(e => e.ClassCode).IsRequired().HasMaxLength(30);
                entity.Property(e => e.ClassName).IsRequired().HasMaxLength(100);

                entity.HasOne(e => e.Officer)
                      .WithMany(o => o.ManagedClasses)
                      .HasForeignKey(e => e.OfficerId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Cấu hình Officer
            modelBuilder.Entity<Officer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OfficerCode).IsUnique();
                entity.Property(e => e.OfficerCode).IsRequired().HasMaxLength(30);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Cấu hình MilitaryRank
            modelBuilder.Entity<MilitaryRank>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RankCode).IsUnique();
                entity.Property(e => e.RankCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RankName).IsRequired().HasMaxLength(50);
            });

            // Cấu hình MilitaryPosition
            modelBuilder.Entity<MilitaryPosition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PositionCode).IsUnique();
                entity.Property(e => e.PositionCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PositionName).IsRequired().HasMaxLength(100);
            });

            // Cấu hình MilitaryUnit
            modelBuilder.Entity<MilitaryUnit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UnitCode).IsUnique();
                entity.Property(e => e.UnitCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.UnitName).IsRequired().HasMaxLength(100);
            });

            // Cấu hình MilitaryMajor
            modelBuilder.Entity<MilitaryMajor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MajorCode).IsUnique();
                entity.Property(e => e.MajorCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.MajorName).IsRequired().HasMaxLength(100);
            });

            // Cấu hình Cadet
            modelBuilder.Entity<Cadet>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CadetCode).IsUnique();
                entity.Property(e => e.CadetCode).IsRequired().HasMaxLength(30);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.MilitaryClass)
                      .WithMany(m => m.Cadets)
                      .HasForeignKey(e => e.ClassId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Cấu hình Subject
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SubjectCode).IsUnique();
                entity.Property(e => e.SubjectCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SubjectName).IsRequired().HasMaxLength(100);
            });

            // Cấu hình PhysicalExamRecord
            modelBuilder.Entity<PhysicalExamRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Cadet)
                      .WithMany(c => c.ExamRecords)
                      .HasForeignKey(e => e.CadetId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Subject)
                      .WithMany(s => s.ExamRecords)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình PasswordResetToken
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email);
            });
        }
    }
}
