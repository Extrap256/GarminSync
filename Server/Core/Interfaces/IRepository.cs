namespace Server.Core.Interfaces
{
    public interface IRepository
    {
        Task<bool> ExistUser(int id);
        //
    }
}
