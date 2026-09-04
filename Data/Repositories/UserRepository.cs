using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByUsernameOrPhoneAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            var trimmed = identifier.Trim();
            return await _context.Users.FirstOrDefaultAsync(u =>
                (u.Username.ToLower() == trimmed.ToLower() || u.PhoneNumber == trimmed) && u.IsActive);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _context.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email.Trim().ToLower() && u.IsActive);
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username.ToLower() == username.Trim().ToLower());
        }

        public async Task<bool> ExistsByPhoneAsync(string phoneNumber)
        {
            return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber.Trim());
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.Trim().ToLower());
        }
    }
}
