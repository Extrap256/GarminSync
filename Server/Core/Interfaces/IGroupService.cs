using EntityClassLibraryServer.Models;
using Server.API.Models.Group;

namespace Server.Core.Interfaces
{
    public interface IGroupService
    {
        Task<Group> CreateGroupAsync(int creatorUserId, CreateGroupDto dto, CancellationToken ct = default);
        Task AddMemberToGroupAsync(int groupId, string memberLogin, CancellationToken ct = default);
        Task AddTrainerToGroupAsync(int groupId, string trainerLogin, CancellationToken ct = default);
        Task<List<Group>> GetUserGroupsAsync(int userId, CancellationToken ct = default);
        Task<string?> GetUserRoleInGroupAsync(int groupId, int userId, CancellationToken ct = default);
        Task<bool> IsUserInGroupAsync(int groupId, int userId, CancellationToken ct = default);
    }
}