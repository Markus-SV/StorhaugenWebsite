using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Supabase;

namespace StorhaugenWebsite.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly Client _supabaseClient;
        private readonly string _apiBaseUrl;

        public ApiClient(HttpClient httpClient, Client supabaseClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _supabaseClient = supabaseClient;
            _apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl not configured");
        }

        public string? GetAuthToken()
        {
            return _supabaseClient.Auth.CurrentSession?.AccessToken;
        }

        private void SetAuthHeader()
        {
            var token = GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                SetAuthHeader();
                var url = $"{_apiBaseUrl}/{endpoint.TrimStart('/')}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<T>();
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GET request failed: {ex.Message}");
                return default;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                SetAuthHeader();
                var url = $"{_apiBaseUrl}/{endpoint.TrimStart('/')}";
                var response = await _httpClient.PostAsJsonAsync(url, data);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>();
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST request failed: {ex.Message}");
                return default;
            }
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                SetAuthHeader();
                var url = $"{_apiBaseUrl}/{endpoint.TrimStart('/')}";
                var response = await _httpClient.PutAsJsonAsync(url, data);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>();
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PUT request failed: {ex.Message}");
                return default;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                SetAuthHeader();
                var url = $"{_apiBaseUrl}/{endpoint.TrimStart('/')}";
                var response = await _httpClient.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DELETE request failed: {ex.Message}");
                return false;
            }
        }
    }
}
