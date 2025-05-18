using ShareVault.API.Models;

namespace ShareVault.API.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> IsEmailUniqueAsync(string email);
        Task<bool> IsUsernameUniqueAsync(string username);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
    }
} 