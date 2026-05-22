namespace Server.Core.Interfaces
{
    public interface IReportService
    {
        Task<string> GetUserReportAsync(int userId, DateTime from, DateTime to, CancellationToken ct = default);
        Task<string> GetGroupReportAsync(int groupId, DateTime from, DateTime to, CancellationToken ct = default);
    }
}
