using Microsoft.EntityFrameworkCore;
using Server.API.Models.User;
using Server.Common.Exceptions;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Core.Services
{
    public class UserService (ApplicationDbContext context) : IUserService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<UserInfoDto> GetUserInfoAsync(int userId, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId, ct) ?? throw new NotFoundException("Пользователь не найден");

            return new UserInfoDto
            {
                UserId = user.UserId,
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                YearOfBirth = user.YearOfBirth,
                //CreatedAt = user.CreatedAt
            };
        }

        public async Task UpdateUserInfoAsync(int userId, UserUpdateDto dto, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId) ?? throw new NotFoundException("Пользователь не найден");

            if (!string.IsNullOrEmpty(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = dto.LastName;

            if (dto.YearOfBirth.HasValue)
                user.YearOfBirth = (ushort?)dto.YearOfBirth;

            await _context.SaveChangesAsync(ct);
        }
    }
}
