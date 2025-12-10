using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

/// <summary>
/// LEGACY: Manages household entities.
/// While households still exist for grouping users, the application is transitioning
/// to a user-centric model. New features should use user-level services.
/// </summary>
public class HouseholdService : IHouseholdService
{
    private readonly AppDbContext _context;

    public HouseholdService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Household?> GetByIdAsync(Guid id)
    {
        return await _context.Households
            .Include(h => h.Leader)
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<Household> CreateAsync(string name, Guid leaderId)
    {
        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = name,
            LeaderId = leaderId,
            Settings = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Households.Add(household);

        // Assign leader to household
        var leader = await _context.Users.FindAsync(leaderId);
        if (leader != null)
        {
            leader.CurrentHouseholdId = household.Id;
            leader.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return household;
    }

    public async Task<Household> UpdateAsync(Household household)
    {
        household.UpdatedAt = DateTime.UtcNow;
        _context.Households.Update(household);
        await _context.SaveChangesAsync();
        return household;
    }

    public async Task<bool> AddMemberAsync(Guid householdId, Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.CurrentHouseholdId = householdId;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid householdId, Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.CurrentHouseholdId != householdId) return false;

        user.CurrentHouseholdId = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<User>> GetMembersAsync(Guid householdId)
    {
        return await _context.Users
            .Where(u => u.CurrentHouseholdId == householdId)
            .ToListAsync();
    }

    public async Task<bool> MergeHouseholdsAsync(Guid sourceHouseholdId, Guid targetHouseholdId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Move all household recipes
            var recipesToMove = await _context.HouseholdRecipes
                .Where(hr => hr.HouseholdId == sourceHouseholdId)
                .ToListAsync();

            foreach (var recipe in recipesToMove)
            {
                recipe.HouseholdId = targetHouseholdId;
                recipe.UpdatedAt = DateTime.UtcNow;
            }

            // Move all users
            var usersToMove = await _context.Users
                .Where(u => u.CurrentHouseholdId == sourceHouseholdId)
                .ToListAsync();

            foreach (var user in usersToMove)
            {
                user.CurrentHouseholdId = targetHouseholdId;
                user.UpdatedAt = DateTime.UtcNow;
            }

            // Delete source household
            var sourceHousehold = await _context.Households.FindAsync(sourceHouseholdId);
            if (sourceHousehold != null)
            {
                _context.Households.Remove(sourceHousehold);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
}
