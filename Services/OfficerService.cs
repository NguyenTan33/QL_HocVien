using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public class OfficerService : IOfficerService
    {
        private readonly IOfficerRepository _officerRepo;
        private readonly IUserRepository _userRepo;

        public OfficerService(IOfficerRepository officerRepo, IUserRepository userRepo)
        {
            _officerRepo = officerRepo;
            _userRepo = userRepo;
        }

        public async Task<IEnumerable<Officer>> GetAllOfficersAsync()
        {
            return await _officerRepo.SearchOfficersAsync(null, null, null, null);
        }

        public async Task<IEnumerable<Officer>> SearchOfficersAsync(string? keyword, string? rank, string? unit, string? position)
        {
            return await _officerRepo.SearchOfficersAsync(keyword, rank, unit, position);
        }

        public async Task<IEnumerable<Officer>> SearchOfficersAsync(QL_HocVien.Models.Filters.OfficerFilterCriteria criteria)
        {
            return await _officerRepo.SearchWithCriteriaAsync(criteria);
        }

        public async Task<Officer?> GetOfficerByIdAsync(int id)
        {
            return await _officerRepo.GetByIdAsync(id);
        }

        public async Task<Officer?> GetOfficerWithDetailsAsync(int id)
        {
            return await _officerRepo.GetOfficerWithDetailsAsync(id);
        }

        public async Task<(bool Success, string Message, Officer? Officer)> AddOfficerAsync(
            Officer officer, bool createLoginAccount = false, string? rawPassword = null)
        {
            if (string.IsNullOrWhiteSpace(officer.OfficerCode))
                return (false, "Mã cán bộ không được để trống.", null);

            if (string.IsNullOrWhiteSpace(officer.FullName))
                return (false, "Họ và tên cán bộ không được để trống.", null);

            officer.OfficerCode = officer.OfficerCode.Trim().ToUpper();
            officer.FullName = officer.FullName.Trim();

            if (await _officerRepo.ExistsByCodeAsync(officer.OfficerCode))
                return (false, $"Mã cán bộ '{officer.OfficerCode}' đã tồn tại trong hệ thống.", null);

            // Tùy chọn tạo tài khoản đăng nhập cho cán bộ
            if (createLoginAccount)
            {
                var username = officer.OfficerCode.ToLower();
                if (await _userRepo.ExistsByUsernameAsync(username))
                {
                    username = $"{username}_{new Random().Next(100, 999)}";
                }

                var phone = !string.IsNullOrWhiteSpace(officer.PhoneNumber) ? officer.PhoneNumber : $"09{new Random().Next(10000000, 99999999)}";
                var email = !string.IsNullOrWhiteSpace(officer.Email) ? officer.Email : $"{username}@mod.gov.vn";
                var pwd = !string.IsNullOrWhiteSpace(rawPassword) ? rawPassword : "Canbo@123";

                var user = new User
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd),
                    FullName = officer.FullName,
                    PhoneNumber = phone,
                    Email = email,
                    Role = "CanBo",
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _userRepo.AddAsync(user);
                await _userRepo.SaveChangesAsync();
                officer.UserId = user.Id;
            }

            officer.CreatedAt = DateTime.Now;
            await _officerRepo.AddAsync(officer);
            await _officerRepo.SaveChangesAsync();

            return (true, "Thêm cán bộ thành công!", officer);
        }

        public async Task<(bool Success, string Message)> UpdateOfficerAsync(Officer officer)
        {
            if (string.IsNullOrWhiteSpace(officer.OfficerCode))
                return (false, "Mã cán bộ không được để trống.");

            if (string.IsNullOrWhiteSpace(officer.FullName))
                return (false, "Họ và tên cán bộ không được để trống.");

            var existing = await _officerRepo.GetByIdAsync(officer.Id);
            if (existing == null)
                return (false, "Không tìm thấy cán bộ cần cập nhật.");

            officer.OfficerCode = officer.OfficerCode.Trim().ToUpper();
            officer.FullName = officer.FullName.Trim();

            if (!existing.OfficerCode.Equals(officer.OfficerCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _officerRepo.ExistsByCodeAsync(officer.OfficerCode))
                    return (false, $"Mã cán bộ '{officer.OfficerCode}' đã được sử dụng.");
            }

            existing.OfficerCode = officer.OfficerCode;
            existing.FullName = officer.FullName;
            existing.Rank = officer.Rank;
            existing.Position = officer.Position;
            existing.Unit = officer.Unit;
            existing.PhoneNumber = officer.PhoneNumber;
            existing.Email = officer.Email;
            existing.Specialty = officer.Specialty;
            existing.DateOfBirth = officer.DateOfBirth;
            existing.EnlistmentDate = officer.EnlistmentDate;
            existing.Notes = officer.Notes;

            _officerRepo.Update(existing);
            await _officerRepo.SaveChangesAsync();

            return (true, "Cập nhật thông tin cán bộ thành công!");
        }

        public async Task<(bool Success, string Message)> DeleteOfficerAsync(int id)
        {
            var existing = await _officerRepo.GetOfficerWithDetailsAsync(id);
            if (existing == null)
                return (false, "Không tìm thấy cán bộ cần xóa.");

            int classCount = existing.ManagedClasses.Count;

            _officerRepo.Delete(existing);
            await _officerRepo.SaveChangesAsync();

            if (classCount > 0)
            {
                return (true, $"Đã xóa cán bộ thành công! ({classCount} lớp do cán bộ phụ trách đã được chuyển trạng thái chờ phân công mới).");
            }

            return (true, "Đã xóa cán bộ thành công!");
        }

        public async Task<(bool Success, string Message)> ResetOfficerPasswordAsync(int officerId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            var officer = await _officerRepo.GetOfficerWithDetailsAsync(officerId);
            if (officer == null)
                return (false, "Không tìm thấy thông tin cán bộ.");

            if (officer.UserId.HasValue && officer.User != null)
            {
                officer.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _userRepo.Update(officer.User);
                await _userRepo.SaveChangesAsync();
                return (true, $"Đã đặt lại mật khẩu cho tài khoản '{officer.User.Username}' thành công!");
            }

            // Nếu cán bộ chưa có tài khoản, tạo mới luôn
            var username = officer.OfficerCode.ToLower();
            var phone = !string.IsNullOrWhiteSpace(officer.PhoneNumber) ? officer.PhoneNumber : $"09{new Random().Next(10000000, 99999999)}";
            var email = !string.IsNullOrWhiteSpace(officer.Email) ? officer.Email : $"{username}@mod.gov.vn";

            var newUser = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword),
                FullName = officer.FullName,
                PhoneNumber = phone,
                Email = email,
                Role = "CanBo",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _userRepo.AddAsync(newUser);
            await _userRepo.SaveChangesAsync();

            officer.UserId = newUser.Id;
            _officerRepo.Update(officer);
            await _officerRepo.SaveChangesAsync();

            return (true, $"Đã tạo tài khoản đăng nhập '{username}' với mật khẩu mới cho cán bộ thành công!");
        }

        public async Task<string> GenerateSuggestedOfficerCodeAsync()
        {
            int seq = await _officerRepo.GetNextOfficerSequenceNumberAsync();
            return $"CB-{seq:D3}";
        }

        public Task<string> GenerateNextOfficerCodeAsync() => GenerateSuggestedOfficerCodeAsync();

        public Task<(bool Success, string Message, Officer? Officer)> CreateOfficerAsync(Officer officer, bool createLoginAccount = false, string? rawPassword = null) =>
            AddOfficerAsync(officer, createLoginAccount, rawPassword);
    }
}
