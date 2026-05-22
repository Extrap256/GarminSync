using EntityClassLibrary.Models;
using Server.API.Models.Group;

namespace Server.Core.Interfaces
{
    public interface IGroupService
    {
        Task<Group> CreateGroupAsync(int creatorUserId, CreateGroupDto dto, CancellationToken ct = default);
        Task AddMemberToGroupAsync(int groupId, string memberLogin, CancellationToken ct = default);
        Task AddTrainerToGroupAsync(int groupId, string trainerLogin, CancellationToken ct = default);
        Task<List<Group>> GetUserGroupsAsync(int userId, CancellationToken ct = default);
    }
}
