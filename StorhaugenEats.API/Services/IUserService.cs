using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByShareIdAsync(string shareId);
    Task<User> CreateAsync(Guid authUserId, string email, string displayName, string? avatarUrl = null);
    Task<User> UpdateAsync(User user);
    Task<string> GenerateUniqueShareIdAsync();
}
