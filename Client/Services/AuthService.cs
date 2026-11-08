using System.Net.Http.Json;
using System.Runtime;
using System.Text;
using System.Text.Json;
using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO.API;
using Client.Infrastructure.API;
using Client.Infrastructure.Security.Interfaces;
using Microsoft.Extensions.Options;

namespace Client.Services
{
    public class AuthService (
        IHttpClientFactory httpClientFactory,
        ITokenStorage tokenStorage,
        IOptions<ApiSettings> options) : IAuthService
    {
        private readonly HttpClient _httpClient = httpClientFactory?.CreateClient("GarminApi")
            ?? throw new ArgumentNullException(nameof(httpClientFactory));
        private readonly ITokenStorage _tokenStorage = tokenStorage
            ?? throw new ArgumentNullException(nameof(tokenStorage));
        private readonly string _baseUrl = !string.IsNullOrEmpty(options.Value.BaseUrl)
            ? options.Value.BaseUrl
            : throw new InvalidOperationException("Отсутствует Api:BaseUrl");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

#if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\n═══════════════════════════════════");
            Console.WriteLine("HTTP REQUEST:");
            Console.WriteLine($"   URL: {_baseUrl}/auth/register");
            Console.WriteLine($"   Method: POST");
            Console.WriteLine($"   Content-Type: {content.Headers.ContentType}");
            Console.WriteLine($"   Body:\n{json}");
            Console.WriteLine("═══════════════════════════════════\n");
            Console.ResetColor();
#endif

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/auth/register", content, ct);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(ct);
            if (result?.Token != null)
                await _tokenStorage.SaveTokenAsync(result.Token, result.ExpiresAt, ct);

            return result ?? throw new InvalidOperationException("Пустой ответ от сервера");
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/auth/login", content, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Неверный логин или пароль");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(ct);

            if (result?.Token != null)
                await _tokenStorage.SaveTokenAsync(result.Token, result.ExpiresAt, ct);

            return result ?? throw new InvalidOperationException("Пустой ответ от сервера");
        }

        public Task LogoutAsync(CancellationToken ct = default)
        {
            return _tokenStorage.ClearTokenAsync(ct);
        }

        public bool IsAuthenticated()
        {
            var token = _tokenStorage.GetToken();
            if (string.IsNullOrEmpty(token))
                return false;

            var expires = _tokenStorage.GetTokenExpiresAt();
            return expires != null && expires.Value > DateTime.UtcNow;
        }

        public string? GetToken()
        {
            if (!IsAuthenticated())
                return null;
            return _tokenStorage.GetToken();
        }

    }
}
