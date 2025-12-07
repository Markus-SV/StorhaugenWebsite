using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services;

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
