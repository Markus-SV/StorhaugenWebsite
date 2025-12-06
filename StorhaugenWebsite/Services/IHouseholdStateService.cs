using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services;

public interface IHouseholdStateService
{
    event Action? OnHouseholdChanged;

    HouseholdDto? CurrentHousehold { get; }
    List<HouseholdDto> UserHouseholds { get; }
    bool HasHousehold { get; }
    bool NeedsHouseholdSetup { get; }

    Task InitializeAsync();
    Task RefreshHouseholdsAsync();
    Task SetCurrentHouseholdAsync(int householdId);
    Task<HouseholdDto> CreateHouseholdAsync(string name);
    Task<List<HouseholdInviteDto>> GetPendingInvitesAsync();
    Task AcceptInviteAsync(int inviteId);
}
