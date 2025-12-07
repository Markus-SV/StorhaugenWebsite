using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Supabase;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;
using Client = Supabase.Client;

namespace StorhaugenWebsite.Services;

public class SupabaseAuthService : IAuthService, IAsyncDisposable
{
    private readonly Client _supabaseClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private Session? _session;

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => _session?.User != null;
    public bool IsAuthorized => IsAuthenticated; // Any authenticated user is authorized

    private string? _cachedDisplayName; 
    public string? CurrentUserEmail => _session?.User?.Email;
    public string? CurrentUserName => !string.IsNullOrEmpty(_cachedDisplayName)
        ? _cachedDisplayName
        : GetUserNameFromEmail(CurrentUserEmail);

    // Inject NavigationManager here
    public SupabaseAuthService(Client supabaseClient, IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        _supabaseClient = supabaseClient;
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;

        _supabaseClient.Auth.AddStateChangedListener(OnAuthStateChange);
    }

    private void OnAuthStateChange(object? sender, Constants.AuthState state)
    {
        _session = _supabaseClient.Auth.CurrentSession;
        OnAuthStateChanged?.Invoke();
    }

    public void UpdateCachedDisplayName(string? displayName)
    {
        _cachedDisplayName = displayName;
        OnAuthStateChanged?.Invoke();
    }

    public async Task InitializeAsync()
    {
        try
        {
            // 1. Sjekk URL for redirect etter login (samme som før)
            var uri = _navigationManager.Uri;
            if (uri.Contains("access_token") && uri.Contains("type=recovery") == false)
            {
                var session = await _supabaseClient.Auth.GetSessionFromUrl(new Uri(uri));

                if (session != null)
                {
                    _session = session;
                    OnAuthStateChanged?.Invoke();
                    return;
                }
            }

            // 2. Sjekk LocalStorage for eksisterende sesjon
            var storedSession = await _supabaseClient.Auth.RetrieveSessionAsync();

            if (storedSession != null)
            {
                // --- HER ER FIKSEN ---
                // Vi må sjekke om tokenet faktisk er gyldig i tid.
                // Supabase-klienten har en metode ExpiresAt() som regner ut dette.
                if (storedSession.ExpiresAt() > DateTime.UtcNow)
                {
                    // Tokenet er fortsatt gyldig -> Logg inn
                    _session = storedSession;
                    OnAuthStateChanged?.Invoke();
                }
                else
                {
                    // Tokenet er utløpt! Vi må slette det for å stoppe loopen.
                    Console.WriteLine("Fant lagret sesjon, men den var utløpt. Logger ut.");
                    await _supabaseClient.Auth.SignOut();
                    _session = null;
                    // Vi invoker ikke OnAuthStateChanged her, for vi vil at brukeren skal bli stående på Login
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth initialization error: {ex.Message}");
            // For sikkerhets skyld, nullstill session hvis noe kræsjer
            _session = null;
        }
    }

    public async Task<(bool success, string? errorMessage)> LoginAsync()
    {
        try
        {
            var redirectUrl = await GetRedirectUrlAsync();

            var options = new SignInOptions
            {
                RedirectTo = redirectUrl
            };

            var result = await _supabaseClient.Auth.SignIn(Provider.Google, options);

            if (result != null && result.Uri != null)
            {
                // 1. We are leaving the app. Using NavigationManager with forceLoad: true 
                // is cleaner than JS Interop for external links.
                _navigationManager.NavigateTo(result.Uri.ToString(), forceLoad: true);

                // 2. We return basic success here, but the code below 
                // technically won't matter because the browser is navigating away.
                return (true, null);
            }

            return (false, "Login was cancelled or failed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return (false, $"Login error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _supabaseClient.Auth.SignOut();
            _session = null;
            OnAuthStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout error: {ex.Message}");
        }
    }

    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult(_session?.AccessToken);
    }

    private Task<string> GetRedirectUrlAsync()
    {
        // Ensure we redirect specifically to the login page so the 
        // OnInitializedAsync method in Login.razor actually runs to parse the token.
        var baseUri = _navigationManager.BaseUri;

        // Handle trailing slash just in case
        var redirectUrl = baseUri.EndsWith("/")
            ? $"{baseUri}login"
            : $"{baseUri}/login";

        return Task.FromResult(redirectUrl);
    }

    public async ValueTask DisposeAsync()
    {
        _supabaseClient.Auth.RemoveStateChangedListener(OnAuthStateChange);
        await Task.CompletedTask;
    }

    private static string? GetUserNameFromEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        // Extract name from email (part before @)
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return email;

        var namePart = email.Substring(0, atIndex);

        // Capitalize first letter
        if (namePart.Length > 0)
        {
            return char.ToUpper(namePart[0]) + namePart.Substring(1).ToLower();
        }

        return namePart;
    }
}
