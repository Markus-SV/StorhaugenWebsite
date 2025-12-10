using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

/// <summary>
/// LEGACY: Manages household entities.
/// While households still exist for grouping users, the application is transitioning
/// to a user-centric model. New features should use user-level services.
/// </summary>
public interface IHouseholdService
{
    Task<Household?> GetByIdAsync(Guid id);
    Task<Household> CreateAsync(string name, Guid leaderId);
    Task<Household> UpdateAsync(Household household);
    Task<bool> AddMemberAsync(Guid householdId, Guid userId);
    Task<bool> RemoveMemberAsync(Guid householdId, Guid userId);
    Task<IEnumerable<User>> GetMembersAsync(Guid householdId);
    Task<bool> MergeHouseholdsAsync(Guid sourceHouseholdId, Guid targetHouseholdId);
}
