using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Supabase.Gotrue;
using System.Security.Claims;
using static Supabase.Gotrue.Constants;

namespace StorhaugenWebsite.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly Supabase.Client _client;
    private readonly IJSRuntime _jsRuntime;
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
            // 1. Check if Supabase already has the session in memory
            var session = _client.Auth.CurrentSession;

            if (session != null)
            {
                // Ensure persistence if session exists in memory
                var json = JsonConvert.SerializeObject(session);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AuthCacheKey, json);
            }
            else
            {
                // 2. Try to load from LocalStorage
                var cachedJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AuthCacheKey);

                if (!string.IsNullOrEmpty(cachedJson))
                {
                    session = JsonConvert.DeserializeObject<Session>(cachedJson);
                    if (session?.AccessToken != null)
                    {
                        await _client.Auth.SetSession(session.AccessToken, session.RefreshToken);
                        session = _client.Auth.CurrentSession;
                    }
                }
            }

            // 3. Create Claims
            if (session?.User != null)
            {
                // --- FIX STARTS HERE ---
                // Try to get display name from metadata ("name" or "full_name"), fallback to email
                string? displayName = session.User.Email;

                if (session.User.UserMetadata != null)
                {
                    if (session.User.UserMetadata.TryGetValue("name", out var nameObj) && nameObj != null)
                    {
                        displayName = nameObj.ToString();
                    }
                    else if (session.User.UserMetadata.TryGetValue("full_name", out var fullNameObj) && fullNameObj != null)
                    {
                        displayName = fullNameObj.ToString();
                    }
                }

                var claims = new List<Claim>
                {
                    // Use the resolved displayName here instead of just email
                    new Claim(ClaimTypes.Name, displayName ?? ""),
                    new Claim(ClaimTypes.Email, session.User.Email ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, session.User.Id ?? "")
                };
                // --- FIX ENDS HERE ---

                var identity = new ClaimsIdentity(claims, "Supabase");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
        }
        catch
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AuthCacheKey);
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    // ... rest of the file (OnAuthStateChanged, Dispose) remains the same
    private async void OnAuthStateChanged(object sender, AuthState state)
    {
        var session = _client.Auth.CurrentSession;

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