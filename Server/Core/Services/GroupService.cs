using EntityClassLibraryServer.Models;
using Microsoft.EntityFrameworkCore;
using Server.API.Models.Group;
using Server.Common.Exceptions;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Core.Services
{
    public class GroupService (ApplicationDbContext context) : IGroupService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Group> CreateGroupAsync(int creatorUserId, CreateGroupDto dto, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == creatorUserId, ct) ?? throw new NotFoundException("Создатель группы не найден");

            var group = new Group()
            {
                Name = dto.Name,
                Description = dto.Description,
                Creator = user,
                CreatedAt = DateTime.UtcNow,
                Trainers = [user]
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync(ct);

            return group;
        }

        public async Task AddMemberToGroupAsync(int groupId, string memberLogin, CancellationToken ct = default)
        {
            User memberUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == memberLogin, ct) ?? throw new NotFoundException($"ПОльзователь с логином {memberLogin} не найден");

            Group group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId, ct) ?? throw new NotFoundException("Группа не найдена");

            group.Members.Add(memberUser);

            await _context.SaveChangesAsync(ct);
        }
        public async Task AddTrainerToGroupAsync(int groupId, string trainerLogin, CancellationToken ct = default)
        {
            User trainerUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == trainerLogin, ct) ?? throw new NotFoundException($"ПОльзователь с логином {trainerLogin} не найден");

            Group group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId, ct) ?? throw new NotFoundException("Группа не найдена");

            group.Trainers.Add(trainerUser);

            await _context.SaveChangesAsync(ct);
        }
        public async Task<List<Group>> GetUserGroupsAsync(int userId, CancellationToken ct = default)
        {
            User t = await _context.Users
                .Include(u => u.Groups)
                .FirstOrDefaultAsync(x => x.UserId == userId, ct) ?? throw new NotFoundException("Пользоатель не найден");
            return t.Groups;
        }

    }
}
