using System.Net.Http.Json;
using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO.API;
using Client.Infrastructure.API;
using Client.Infrastructure.Security.Interfaces;
using Microsoft.Extensions.Options;

namespace Client.Services
{
    public class AuthService (HttpClient httpClient, ITokenStorage tokenStorage, IOptions<ApiSettings> options) : IAuthService
    {
        private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        private readonly ITokenStorage _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
        private readonly string _baseUrl = !string.IsNullOrEmpty(options.Value.BaseUrl) ? options.Value.BaseUrl : throw new InvalidOperationException("Отсутствует Api:DaseUrl");

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/auth/register", request, ct);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(ct);
            if (result?.Token != null)
                await _tokenStorage.SaveTokenAsync(result.Token, result.ExpiresAt, ct);

            return result ?? throw new InvalidOperationException("Пустой ответ от сервера");
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/auth/login", request, ct);

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
            // Добавить очищение локальнойБД
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
