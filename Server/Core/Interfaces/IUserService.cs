using Microsoft.Extensions.Primitives;
using Server.API.Models.User;

namespace Server.Core.Interfaces
{
    public interface IUserService
    {
        Task<UserInfoDto> GetUserInfoAsync(int userId, CancellationToken ct = default);
        Task UpdateUserInfoAsync(int userId, UserUpdateDto dto, CancellationToken ct = default);
        // Можно добавить удаление
    }
}
