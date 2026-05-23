namespace Client.Infrastructure.Security.Interfaces
{
    public interface ITokenStorage
    {
        Task SaveTokenAsync(string token, DateTime expiresAt, CancellationToken ct = default);
        Task ClearTokenAsync(CancellationToken ct = default);
        string? GetToken();
        DateTime? GetTokenExpiresAt();
    }
}
