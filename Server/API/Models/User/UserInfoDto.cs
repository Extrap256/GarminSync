namespace Server.API.Models.User
{
    public class UserInfoDto
    {
        public int UserId {  get; set; }
        public string Login {  get; set; } = string.Empty;
        public string? FirstName {  get; set; }
        public string? LastName {  get; set; }
        public int? YearOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }
        
    }
}
