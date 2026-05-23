namespace Client.Core.Interfaces.DTO.API
{
    public record AuthResponse(
        string Token,
        DateTime ExpiresAt,
        UserProfileDto User);
}
