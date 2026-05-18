namespace Server.API.Models.Auth
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public ushort? YearOfBirth {  get; set; }
        public static UserDto FromEntity(EntityClassLibrary.Models.User user)
        {
            return new()
            {
                Id = user.UserId,
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                YearOfBirth = user.YearOfBirth
            };
        }
    }
}
