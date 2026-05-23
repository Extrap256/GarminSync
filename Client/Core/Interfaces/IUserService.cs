using System;
using System.Collections.Generic;
using System.Text;
using Client.Core.Interfaces.DTO.API;

namespace Client.Core.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Получить данные текущего профиля
        /// </summary>
        Task<UserProfileDto> GetCurrentUserAsync(CancellationToken ct = default);

        /// <summary>
        /// Обновить данные пользователя
        /// </summary>
        Task UpdateCurrentUserAsync(UpdateUserRequest request, CancellationToken ct = default);
    }
}
