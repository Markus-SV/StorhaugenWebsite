using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Newtonsoft.Json; // Needed for Supabase JSON serialization
using Supabase.Gotrue;
using System.Security.Claims;
using static Supabase.Gotrue.Constants;

namespace StorhaugenWebsite.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly Supabase.Client _client;
    private readonly IJSRuntime _jsRuntime;

    // dedicated key just for auth
    private const string AuthCacheKey = "supa_auth_session";

    public SupabaseAuthStateProvider(Supabase.Client client, IJSRuntime jsRuntime)
    {
        _client = client;
        _jsRuntime = jsRuntime;
        _client.Auth.AddStateChangedListener(OnAuthStateChanged);
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var session = _client.Auth.CurrentSession;

            // 1. If RAM is empty, check LocalStorage
            if (session == null)
            {
                var cachedJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AuthCacheKey);

                if (!string.IsNullOrEmpty(cachedJson))
                {
                    // Deserialize and restore session to Supabase Client
                    session = JsonConvert.DeserializeObject<Session>(cachedJson);
                    if (session?.AccessToken != null)
                    {
                        // Restore the session in the client so it can make authenticated requests
                        await _client.Auth.SetSession(session.AccessToken, session.RefreshToken);
                        session = _client.Auth.CurrentSession;
                    }
                }
            }

            // 2. Create Claims
            if (session?.User != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, session.User.Email ?? ""),
                    new Claim(ClaimTypes.Email, session.User.Email ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, session.User.Id ?? "")
                };

                var identity = new ClaimsIdentity(claims, "Supabase");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
        }
        catch
        {
            // If session restoration fails (e.g. invalid JSON), clear storage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AuthCacheKey);
        }

        // 3. Not Authenticated
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private async void OnAuthStateChanged(object sender, AuthState state)
    {
        var session = _client.Auth.CurrentSession;

        // Handle Persistence
        if (state == AuthState.SignedIn || state == AuthState.TokenRefreshed)
        {
            if (session != null)
            {
                var json = JsonConvert.SerializeObject(session);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AuthCacheKey, json);
            }
        }
        else if (state == AuthState.SignedOut)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AuthCacheKey);
        }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        _client.Auth.RemoveStateChangedListener(OnAuthStateChanged);
    }
}