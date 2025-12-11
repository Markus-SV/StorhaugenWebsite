using System.Security.Claims;
using System.Text.Json; // Add this
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;

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
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
    }

    public string? GetUserSubject()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    public async Task<Guid> GetOrCreateUserIdAsync()
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

        // 1. Resolve the best possible display name from the token
        var displayName = GetDisplayNameFromToken(email);
        var avatarUrl = GetAvatarFromToken();

        // 2. Check DB
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            // --- CREATE NEW USER ---
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = displayName, // Now uses the parsed name
                AvatarUrl = avatarUrl,
                SupabaseUserId = subject,
                UniqueShareId = GenerateUniqueShareId(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            // --- AUTO-UPDATE EXISTING USER ---
            // If the DB has an email-like name, but we found a real name in the token, update it!
            bool dataChanged = false;

            // Update Name if better one found
            if (user.DisplayName.Contains("@") && !displayName.Contains("@"))
            {
                user.DisplayName = displayName;
                dataChanged = true;
            }

            // Update Avatar if new one found
            if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
                dataChanged = true;
            }

            if (dataChanged)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return user.Id;
    }

    // --- HELPER METHODS ---

    private string GetDisplayNameFromToken(string email)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return email.Split('@')[0];

        // 1. Try standard claims
        var name = user.FindFirst("name")?.Value ?? user.FindFirst(ClaimTypes.Name)?.Value;

        // 2. Try parsing Supabase 'user_metadata' claim
        if (string.IsNullOrEmpty(name) || name.Contains("@"))
        {
            var metadata = user.FindFirst("user_metadata")?.Value;
            if (!string.IsNullOrEmpty(metadata))
            {
                try
                {
                    using var doc = JsonDocument.Parse(metadata);
                    if (doc.RootElement.TryGetProperty("name", out var nameProp))
                    {
                        name = nameProp.GetString();
                    }
                    else if (doc.RootElement.TryGetProperty("full_name", out var fullProp))
                    {
                        name = fullProp.GetString();
                    }
                }
                catch { /* Ignore JSON errors */ }
            }
        }

        // 3. Fallback to Email prefix
        return !string.IsNullOrWhiteSpace(name) ? name : email.Split('@')[0];
    }

    private string? GetAvatarFromToken()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return null;

        // 1. Try standard picture claim
        var avatar = user.FindFirst("picture")?.Value;

        // 2. Try Supabase 'user_metadata'
        if (string.IsNullOrEmpty(avatar))
        {
            var metadata = user.FindFirst("user_metadata")?.Value;
            if (!string.IsNullOrEmpty(metadata))
            {
                try
                {
                    using var doc = JsonDocument.Parse(metadata);
                    if (doc.RootElement.TryGetProperty("avatar_url", out var avProp))
                    {
                        avatar = avProp.GetString();
                    }
                    else if (doc.RootElement.TryGetProperty("picture", out var picProp))
                    {
                        avatar = picProp.GetString();
                    }
                }
                catch { }
            }
        }
        return avatar;
    }

    private string GenerateUniqueShareId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}