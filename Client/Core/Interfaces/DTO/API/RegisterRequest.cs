namespace Client.Core.Interfaces.DTO.API
{
    public record RegisterRequest(
        string Login,
        string Password,
        string FirstName,
        string LastName,
        int? YearOfBirth);
}
