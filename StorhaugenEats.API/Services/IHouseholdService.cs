using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

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
