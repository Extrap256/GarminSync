namespace Server.Core.Interfaces
{
    public interface IBotService
    {
        Task CreateUserAsync();
        Task UpdateUserAsync(int userId, Dictionary<string, object> updates);
        Task DeleteUserAsync(int userId);
        Task<string> GetUserWorkoutReportAsync(int userId, DateTime from, DateTime to);
        // Расширить
    }
}
