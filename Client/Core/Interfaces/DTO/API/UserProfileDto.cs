namespace Client.Core.Interfaces.DTO.API
{
    public record UserProfileDto(
        int UserId,
        string Login,
        string FirstName,
        string LastName,
        int? YearOfBirth,
        DateTime CreatedAt);
}
