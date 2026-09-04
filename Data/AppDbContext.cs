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
