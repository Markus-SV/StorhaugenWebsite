using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Gotrue;
using System.Security.Claims;
using static Supabase.Gotrue.Constants;

namespace StorhaugenWebsite.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly Supabase.Client _client;

    public SupabaseAuthStateProvider(Supabase.Client client)
    {
        _client = client;
        // Listen to Supabase auth events automatically
        _client.Auth.AddStateChangedListener(OnAuthStateChanged);
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // 1. Try to get the session from memory or LocalStorage
        var session = _client.Auth.CurrentSession;

        if (session == null)
        {
            // If no session in memory, try retrieving from persistence (LocalStorage)
            try
            {
                session = await _client.Auth.RetrieveSessionAsync();
            }
            catch
            {
                // Ignore errors here, just means no session
            }
        }

        // 2. If we have a user, create the ClaimsPrincipal
        if (session?.User != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, session.User.Email ?? ""),
                new Claim(ClaimTypes.Email, session.User.Email ?? ""),
                new Claim(ClaimTypes.NameIdentifier, session.User.Id ?? "")
            };

            var identity = new ClaimsIdentity(claims, "Supabase");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }

        // 3. No user -> Return Empty (Not Authenticated)
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    // Triggered when Supabase detects login/logout
    private void OnAuthStateChanged(object sender, AuthState state)
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        _client.Auth.RemoveStateChangedListener(OnAuthStateChanged);
    }
}