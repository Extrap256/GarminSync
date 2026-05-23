using System.Text.Json;
using Client.Infrastructure.Security.Interfaces;

namespace Client.Infrastructure.Security
{
    public class TokenStorage : ITokenStorage
    {
        public TokenStorage(string appDataPath)
        {
            var appFolder = Path.Combine(appDataPath, "GarminSync");

            Directory.CreateDirectory(appFolder);
            _filePath = Path.Combine(appFolder, TokenFileName);
        }
        private const string TokenFileName = "auth_token.json";
        private readonly string _filePath;

        public async Task SaveTokenAsync(string token, DateTime expiresAt, CancellationToken ct = default)
        {
            var data = new { Token = token, ExpiresAt = expiresAt };
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(_filePath, json, ct);
        }

        public Task ClearTokenAsync(CancellationToken ct = default)
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
            return Task.CompletedTask;
        }

        public string? GetToken()
        {
            if (!File.Exists(_filePath))
                return null;
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<AuthTokenData>(json);
            return data?.Token;
        }

        public DateTime? GetTokenExpiresAt()
        {
            if (!File.Exists(_filePath))
                return null;
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<AuthTokenData>(json);
            return data?.ExpiresAt;
        }

        private record AuthTokenData(string Token, DateTime ExpiresAt);
    }
}
