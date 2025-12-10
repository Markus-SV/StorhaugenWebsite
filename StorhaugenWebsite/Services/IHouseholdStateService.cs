using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services;

/// <summary>
/// LEGACY: Manages household state for the current user.
/// While households still exist, the application is transitioning to a user-centric model.
/// New features should use user-level state and services.
/// </summary>
public interface IHouseholdStateService
{
    event Action? OnHouseholdChanged;

    HouseholdDto? CurrentHousehold { get; }
    List<HouseholdDto> UserHouseholds { get; }
    bool HasHousehold { get; }
    bool NeedsHouseholdSetup { get; }

    Task InitializeAsync(bool force = false);
    Task RefreshHouseholdsAsync();
    Task SetCurrentHouseholdAsync(Guid householdId);
    Task<HouseholdDto> CreateHouseholdAsync(string name);
    Task<List<HouseholdInviteDto>> GetPendingInvitesAsync();
    Task AcceptInviteAsync(Guid inviteId);
}
