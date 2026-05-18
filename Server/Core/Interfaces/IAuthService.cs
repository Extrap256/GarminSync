using Server.API.Models.Auth;

namespace Server.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
        Task<bool> ValidateTokenAsync(string Token, CancellationToken token = default);
    }
}
