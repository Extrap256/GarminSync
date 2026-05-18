using Server.Common.Settings;

namespace Server.API.Models.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;

        public static AuthResponse Success(string token, EntityClassLibrary.Models.User user)
        {
            return new()
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(JwtConstants.ExpiryHours),
                User = UserDto.FromEntity(user)
            };
        }
    }
}
