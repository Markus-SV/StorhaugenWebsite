using System.Security.Claims;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;
using Microsoft.EntityFrameworkCore;

namespace StorhaugenEats.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public string? GetUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User
            ?.FindFirst(ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User
            ?.FindFirst("email")?.Value;
    }

    public string? GetUserSubject()
    {
        return _httpContextAccessor.HttpContext?.User
            ?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User
            ?.FindFirst("sub")?.Value;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    public async Task<int> GetOrCreateUserIdAsync()
    {
        if (!IsAuthenticated())
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var email = GetUserEmail();
        var subject = GetUserSubject();

        if (string.IsNullOrEmpty(email))
        {
            throw new UnauthorizedAccessException("User email not found in token");
        }

        // Try to find existing user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            // Create new user
            // Get display name from JWT claims or use email
            var displayName = _httpContextAccessor.HttpContext?.User
                ?.FindFirst("name")?.Value
                ?? _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.Name)?.Value
                ?? email.Split('@')[0]; // Fallback to email prefix

            var avatarUrl = _httpContextAccessor.HttpContext?.User
                ?.FindFirst("picture")?.Value;

            user = new User
            {
                Email = email,
                DisplayName = displayName,
                AvatarUrl = avatarUrl,
                SupabaseUserId = subject,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return user.Id;
    }
}
