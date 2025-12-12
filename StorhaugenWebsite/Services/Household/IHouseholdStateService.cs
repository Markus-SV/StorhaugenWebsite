using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.Services;

/// <summary>
/// Manages group (household) state for the current user.
/// Supports multi-group filtering - user can be in multiple groups and filter which ones to view.
/// </summary>
public interface IHouseholdStateService
{
    event Action? OnHouseholdChanged;
    event Action? OnActiveGroupsChanged;

    /// <summary>
    /// All groups (households) the user is a member of.
    /// </summary>
    List<HouseholdDto> UserHouseholds { get; }

    /// <summary>
    /// Currently active group filters - which groups to show recipes from.
    /// By default, all groups are active.
    /// </summary>
    List<Guid> ActiveGroupFilters { get; }

    /// <summary>
    /// The active groups as DTOs (for easy UI binding).
    /// </summary>
    List<HouseholdDto> ActiveGroups { get; }

    /// <summary>
    /// Whether the user is a member of at least one group.
    /// </summary>
    bool HasHousehold { get; }

    /// <summary>
    /// Whether the user needs to create or join a group.
    /// </summary>
    bool NeedsHouseholdSetup { get; }

    /// <summary>
    /// [DEPRECATED] For backwards compatibility - returns first active group or first household.
    /// New code should use ActiveGroupFilters instead.
    /// </summary>
    HouseholdDto? CurrentHousehold { get; }

    Task InitializeAsync(bool force = false);
    Task RefreshHouseholdsAsync();

    /// <summary>
    /// Toggle a group in the active filters.
    /// </summary>
    void ToggleGroupFilter(Guid groupId);

    /// <summary>
    /// Set all groups as active.
    /// </summary>
    void SelectAllGroups();

    /// <summary>
    /// Clear all group filters (select none).
    /// </summary>
    void ClearAllGroups();

    /// <summary>
    /// Check if a specific group is active in the filter.
    /// </summary>
    bool IsGroupActive(Guid groupId);

    /// <summary>
    /// [DEPRECATED] For backwards compatibility - switches to a single household context.
    /// New code should use ToggleGroupFilter instead.
    /// </summary>
    Task SetCurrentHouseholdAsync(Guid householdId);

    Task<HouseholdDto> CreateHouseholdAsync(string name);
    Task<List<HouseholdInviteDto>> GetPendingInvitesAsync();
    Task AcceptInviteAsync(Guid inviteId);
}
