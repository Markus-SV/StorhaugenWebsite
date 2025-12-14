using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByShareIdAsync(string shareId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UniqueShareId == shareId);
    }

    public async Task<User> CreateAsync(Guid authUserId, string email, string displayName, string? avatarUrl = null)
    {
        var shareId = await GenerateUniqueShareIdAsync();

        var user = new User
        {
            Id = authUserId, // Use Supabase Auth user ID
            Email = email,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            UniqueShareId = shareId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<string> GenerateUniqueShareIdAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude confusing chars
        var random = new Random();
        string shareId;

        do
        {
            shareId = new string(Enumerable.Range(0, 12)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        }
        while (await _context.Users.AnyAsync(u => u.UniqueShareId == shareId));

        return shareId;
    }
}
