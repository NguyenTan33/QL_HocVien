using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Data.Repositories;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
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
                return (false, "MÃ£ cÃ¡n bá»™ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            if (string.IsNullOrWhiteSpace(officer.FullName))
                return (false, "Há» vÃ  tÃªn cÃ¡n bá»™ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.", null);

            officer.OfficerCode = officer.OfficerCode.Trim().ToUpper();
            officer.FullName = officer.FullName.Trim();

            if (await _officerRepo.ExistsByCodeAsync(officer.OfficerCode))
                return (false, $"MÃ£ cÃ¡n bá»™ '{officer.OfficerCode}' Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.", null);

            // TÃ¹y chá»n táº¡o tÃ i khoáº£n Ä‘Äƒng nháº­p cho cÃ¡n bá»™
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

            return (true, "ThÃªm cÃ¡n bá»™ thÃ nh cÃ´ng!", officer);
        }

        public async Task<(bool Success, string Message)> UpdateOfficerAsync(Officer officer)
        {
            if (string.IsNullOrWhiteSpace(officer.OfficerCode))
                return (false, "MÃ£ cÃ¡n bá»™ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            if (string.IsNullOrWhiteSpace(officer.FullName))
                return (false, "Há» vÃ  tÃªn cÃ¡n bá»™ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            var existing = await _officerRepo.GetByIdAsync(officer.Id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y cÃ¡n bá»™ cáº§n cáº­p nháº­t.");

            officer.OfficerCode = officer.OfficerCode.Trim().ToUpper();
            officer.FullName = officer.FullName.Trim();

            if (!existing.OfficerCode.Equals(officer.OfficerCode, StringComparison.OrdinalIgnoreCase))
            {
                if (await _officerRepo.ExistsByCodeAsync(officer.OfficerCode))
                    return (false, $"MÃ£ cÃ¡n bá»™ '{officer.OfficerCode}' Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng.");
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

            return (true, "Cáº­p nháº­t thÃ´ng tin cÃ¡n bá»™ thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message)> DeleteOfficerAsync(int id)
        {
            var existing = await _officerRepo.GetOfficerWithDetailsAsync(id);
            if (existing == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y cÃ¡n bá»™ cáº§n xÃ³a.");

            int classCount = existing.ManagedClasses.Count;

            _officerRepo.Delete(existing);
            await _officerRepo.SaveChangesAsync();

            if (classCount > 0)
            {
                return (true, $"ÄÃ£ xÃ³a cÃ¡n bá»™ thÃ nh cÃ´ng! ({classCount} lá»›p do cÃ¡n bá»™ phá»¥ trÃ¡ch Ä‘Ã£ Ä‘Æ°á»£c chuyá»ƒn tráº¡ng thÃ¡i chá» phÃ¢n cÃ´ng má»›i).");
            }

            return (true, "ÄÃ£ xÃ³a cÃ¡n bá»™ thÃ nh cÃ´ng!");
        }

        public async Task<(bool Success, string Message, int DeletedCount)> DeleteMultipleOfficersAsync(IEnumerable<int> officerIds)
        {
            var idList = officerIds?.Distinct().ToList() ?? new List<int>();
            if (!idList.Any())
                return (false, "KhÃ´ng cÃ³ cÃ¡n bá»™ nÃ o Ä‘Æ°á»£c chá»n Ä‘á»ƒ xÃ³a.", 0);

            int deleted = 0;
            try
            {
                foreach (var id in idList)
                {
                    var existing = await _officerRepo.GetOfficerWithDetailsAsync(id);
                    if (existing != null)
                    {
                        _officerRepo.Delete(existing);
                        deleted++;
                    }
                }
                if (deleted > 0)
                {
                    await _officerRepo.SaveChangesAsync();
                }
                return (true, $"ÄÃ£ xÃ³a thÃ nh cÃ´ng {deleted} cÃ¡n bá»™.", deleted);
            }
            catch (Exception ex)
            {
                return (false, $"Lá»—i khi xÃ³a cÃ¡n bá»™: {ex.Message}", deleted);
            }
        }

        public async Task<(bool Success, string Message)> ResetOfficerPasswordAsync(int officerId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Máº­t kháº©u má»›i pháº£i cÃ³ Ã­t nháº¥t 6 kÃ½ tá»±.");

            var officer = await _officerRepo.GetOfficerWithDetailsAsync(officerId);
            if (officer == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin cÃ¡n bá»™.");

            if (officer.UserId.HasValue && officer.User != null)
            {
                officer.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _userRepo.Update(officer.User);
                await _userRepo.SaveChangesAsync();
                return (true, $"ÄÃ£ Ä‘áº·t láº¡i máº­t kháº©u cho tÃ i khoáº£n '{officer.User.Username}' thÃ nh cÃ´ng!");
            }

            // Náº¿u cÃ¡n bá»™ chÆ°a cÃ³ tÃ i khoáº£n, táº¡o má»›i luÃ´n
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

            return (true, $"ÄÃ£ táº¡o tÃ i khoáº£n Ä‘Äƒng nháº­p '{username}' vá»›i máº­t kháº©u má»›i cho cÃ¡n bá»™ thÃ nh cÃ´ng!");
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

