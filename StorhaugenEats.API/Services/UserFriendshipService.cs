using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

public class UserFriendshipService : IUserFriendshipService
{
    private readonly AppDbContext _context;

    public UserFriendshipService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FriendshipListDto> GetFriendshipsAsync(Guid userId)
    {
        var friendships = await _context.UserFriendships
            .Include(f => f.RequesterUser)
            .Include(f => f.TargetUser)
            .Where(f => f.RequesterUserId == userId || f.TargetUserId == userId)
            .ToListAsync();

        var result = new FriendshipListDto();

        foreach (var friendship in friendships)
        {
            var isRequester = friendship.RequesterUserId == userId;
            var otherUser = isRequester ? friendship.TargetUser : friendship.RequesterUser;

            var recipeCount = await _context.UserRecipes
                .CountAsync(r => r.UserId == otherUser.Id && !r.IsArchived);

            var dto = new UserFriendshipDto
            {
                Id = friendship.Id,
                FriendUserId = otherUser.Id,
                FriendDisplayName = otherUser.DisplayName,
                FriendAvatarUrl = otherUser.AvatarUrl,
                FriendShareId = otherUser.UniqueShareId,
                Message = friendship.Message,
                CreatedAt = friendship.CreatedAt,
                RespondedAt = friendship.RespondedAt,
                RecipeCount = recipeCount
            };

            if (friendship.Status == "accepted")
            {
                dto.Status = "accepted";
                result.Friends.Add(dto);
            }
            else if (friendship.Status == "pending")
            {
                if (isRequester)
                {
                    dto.Status = "pending_sent";
                    result.PendingSent.Add(dto);
                }
                else
                {
                    dto.Status = "pending_received";
                    result.PendingReceived.Add(dto);
                }
            }
        }

        return result;
    }

    public async Task<List<FriendProfileDto>> GetFriendsAsync(Guid userId)
    {
        var friendIds = await GetFriendIdsAsync(userId);

        var friends = await _context.Users
            .Where(u => friendIds.Contains(u.Id))
            .ToListAsync();

        var result = new List<FriendProfileDto>();
        foreach (var friend in friends)
        {
            var recipeCount = await _context.UserRecipes
                .CountAsync(r => r.UserId == friend.Id && !r.IsArchived);

            result.Add(new FriendProfileDto
            {
                Id = friend.Id,
                DisplayName = friend.DisplayName,
                AvatarUrl = friend.AvatarUrl,
                ShareId = friend.UniqueShareId,
                Bio = friend.Bio,
                IsProfilePublic = friend.IsProfilePublic,
                FavoriteCuisines = ParseJsonList(friend.FavoriteCuisines),
                RecipeCount = recipeCount,
                JoinedAt = friend.CreatedAt
            });
        }

        return result;
    }

    public async Task<UserFriendshipDto?> GetFriendshipAsync(Guid friendshipId, Guid userId)
    {
        var friendship = await _context.UserFriendships
            .Include(f => f.RequesterUser)
            .Include(f => f.TargetUser)
            .FirstOrDefaultAsync(f => f.Id == friendshipId &&
                (f.RequesterUserId == userId || f.TargetUserId == userId));

        if (friendship == null) return null;

        var isRequester = friendship.RequesterUserId == userId;
        var otherUser = isRequester ? friendship.TargetUser : friendship.RequesterUser;

        var recipeCount = await _context.UserRecipes
            .CountAsync(r => r.UserId == otherUser.Id && !r.IsArchived);

        string status = friendship.Status;
        if (friendship.Status == "pending")
        {
            status = isRequester ? "pending_sent" : "pending_received";
        }

        return new UserFriendshipDto
        {
            Id = friendship.Id,
            FriendUserId = otherUser.Id,
            FriendDisplayName = otherUser.DisplayName,
            FriendAvatarUrl = otherUser.AvatarUrl,
            FriendShareId = otherUser.UniqueShareId,
            Status = status,
            Message = friendship.Message,
            CreatedAt = friendship.CreatedAt,
            RespondedAt = friendship.RespondedAt,
            RecipeCount = recipeCount
        };
    }

    public async Task<UserFriendshipDto> SendFriendRequestAsync(Guid userId, SendFriendRequestDto dto)
    {
        Guid targetUserId;

        if (dto.TargetUserId.HasValue)
        {
            targetUserId = dto.TargetUserId.Value;
        }
        else if (!string.IsNullOrEmpty(dto.TargetShareId))
        {
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UniqueShareId == dto.TargetShareId)
                ?? throw new InvalidOperationException("User not found with that share ID");
            targetUserId = targetUser.Id;
        }
        else
        {
            throw new InvalidOperationException("Either TargetUserId or TargetShareId must be provided");
        }

        // Validate
        if (targetUserId == userId)
            throw new InvalidOperationException("You cannot send a friend request to yourself");

        // Check for existing friendship
        var existing = await _context.UserFriendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterUserId == userId && f.TargetUserId == targetUserId) ||
                (f.RequesterUserId == targetUserId && f.TargetUserId == userId));

        if (existing != null)
        {
            if (existing.Status == "accepted")
                throw new InvalidOperationException("You are already friends");
            if (existing.Status == "pending")
                throw new InvalidOperationException("A friend request already exists");
            if (existing.Status == "blocked")
                throw new InvalidOperationException("Unable to send friend request");
        }

        var friendship = new UserFriendship
        {
            Id = Guid.NewGuid(),
            RequesterUserId = userId,
            TargetUserId = targetUserId,
            Status = "pending",
            Message = dto.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserFriendships.Add(friendship);
        await _context.SaveChangesAsync();

        return (await GetFriendshipAsync(friendship.Id, userId))!;
    }

    public async Task<UserFriendshipDto> RespondToRequestAsync(Guid friendshipId, Guid userId, string action)
    {
        var friendship = await _context.UserFriendships
            .FirstOrDefaultAsync(f => f.Id == friendshipId && f.TargetUserId == userId && f.Status == "pending")
            ?? throw new InvalidOperationException("Friend request not found or you cannot respond to it");

        action = action.ToLower();
        if (action != "accept" && action != "reject")
            throw new InvalidOperationException("Action must be 'accept' or 'reject'");

        friendship.Status = action == "accept" ? "accepted" : "rejected";
        friendship.RespondedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (await GetFriendshipAsync(friendshipId, userId))!;
    }

    public async Task RemoveFriendshipAsync(Guid friendshipId, Guid userId)
    {
        var friendship = await _context.UserFriendships
            .FirstOrDefaultAsync(f => f.Id == friendshipId &&
                (f.RequesterUserId == userId || f.TargetUserId == userId))
            ?? throw new InvalidOperationException("Friendship not found");

        _context.UserFriendships.Remove(friendship);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(Guid userId, string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<UserSearchResultDto>();

        query = query.ToLower();

        var users = await _context.Users
            .Where(u => u.Id != userId)
            .Where(u => u.IsProfilePublic)
            .Where(u =>
                u.DisplayName.ToLower().Contains(query) ||
                u.UniqueShareId.ToLower() == query ||
                u.Email.ToLower().Contains(query))
            .Take(limit)
            .ToListAsync();

        // Get friendship statuses
        var userIds = users.Select(u => u.Id).ToList();
        var friendships = await _context.UserFriendships
            .Where(f =>
                (f.RequesterUserId == userId && userIds.Contains(f.TargetUserId)) ||
                (f.TargetUserId == userId && userIds.Contains(f.RequesterUserId)))
            .ToListAsync();

        return users.Select(u =>
        {
            var friendship = friendships.FirstOrDefault(f =>
                (f.RequesterUserId == userId && f.TargetUserId == u.Id) ||
                (f.TargetUserId == userId && f.RequesterUserId == u.Id));

            string status = "none";
            if (friendship != null)
            {
                if (friendship.Status == "accepted")
                    status = "friends";
                else if (friendship.Status == "pending")
                    status = friendship.RequesterUserId == userId ? "pending_sent" : "pending_received";
            }

            return new UserSearchResultDto
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                ShareId = u.UniqueShareId,
                FriendshipStatus = status
            };
        }).ToList();
    }

    public async Task<FriendProfileDto?> GetUserProfileAsync(Guid profileUserId, Guid requestingUserId)
    {
        var user = await _context.Users.FindAsync(profileUserId);
        if (user == null) return null;

        // Check if profile is public or if they are friends
        var areFriends = await AreFriendsAsync(profileUserId, requestingUserId);
        if (!user.IsProfilePublic && !areFriends && profileUserId != requestingUserId)
            return null;

        var recipeCount = await _context.UserRecipes
            .CountAsync(r => r.UserId == profileUserId && !r.IsArchived &&
                (r.Visibility == "public" ||
                 (r.Visibility == "friends" && areFriends) ||
                 r.UserId == requestingUserId));

        return new FriendProfileDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            ShareId = user.UniqueShareId,
            Bio = user.Bio,
            IsProfilePublic = user.IsProfilePublic,
            FavoriteCuisines = ParseJsonList(user.FavoriteCuisines),
            RecipeCount = recipeCount,
            JoinedAt = user.CreatedAt
        };
    }

    public async Task<bool> AreFriendsAsync(Guid userId1, Guid userId2)
    {
        if (userId1 == userId2) return true; // User is always "friends" with themselves

        return await _context.UserFriendships
            .AnyAsync(f =>
                f.Status == "accepted" &&
                ((f.RequesterUserId == userId1 && f.TargetUserId == userId2) ||
                 (f.RequesterUserId == userId2 && f.TargetUserId == userId1)));
    }

    public async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
    {
        var friendships = await _context.UserFriendships
            .Where(f => f.Status == "accepted")
            .Where(f => f.RequesterUserId == userId || f.TargetUserId == userId)
            .ToListAsync();

        return friendships
            .Select(f => f.RequesterUserId == userId ? f.TargetUserId : f.RequesterUserId)
            .Distinct()
            .ToList();
    }

    private static List<string> ParseJsonList(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch
        {
            return new List<string>();
        }
    }
}
