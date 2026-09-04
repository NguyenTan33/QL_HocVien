using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameOrPhoneAsync(string identifier);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsByUsernameAsync(string username);
        Task<bool> ExistsByPhoneAsync(string phoneNumber);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
