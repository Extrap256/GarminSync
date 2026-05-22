using System.Security.Claims;
using Server.API.Models.Group;
using Server.Common.Exceptions;
using Server.Core.Interfaces;

namespace Server.API.Endpoints
{
    public static class GroupEndpoints
    {
        public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/groups").RequireAuthorization();

            //Создание группы
            group.MapPost("/", async (
                CreateGroupDto dto,
                HttpContext context,
                IGroupService groupService,
                CancellationToken ct) =>
            {
                var userId = GetUserId(context);
                var newGroup = await groupService.CreateGroupAsync(userId, dto, ct);
                return Results.Created($"api/groups/{newGroup}", new { newGroup.GroupId, newGroup.Name });
            });

            //Добавление участника
            group.MapPost("/{groupId:int}/members", async (
                int groupId,
                AddMemberDto dto,
                IGroupService groupService,
                CancellationToken ct
                ) =>
            {
                await groupService.AddMemberToGroupAsync(groupId, dto.MemberLogin, ct);
                return Results.Ok(new { message = "Участник добавлен" });
            });

            //Добавление тренера
            group.MapPost("/{groupId:int}/trainers", async (
                int groupId,
                HttpContext context,
                AssignTrainerDto dto,
                IGroupService groupService,
                CancellationToken ct) =>
            {
                await groupService.AddTrainerToGroupAsync(groupId, dto.TrainerLogin, ct);
                return Results.Ok(new { message = "Тренер добавлен" });
            });

            // получение групп пользователя
            group.MapGet("/my", async (
                HttpContext context,
                IGroupService groupService,
                CancellationToken ct) =>
            {
                var userId = GetUserId(context);
                var groups = await groupService.GetUserGroupsAsync(userId, ct);
                return Results.Ok(groups.Select(x => new { x.GroupId, x.Name, x.Description }));
            });
        }

        private static int GetUserId(HttpContext context)
        {
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedException("Отсутсвует UserId");

            return int.Parse(claim.Value);
        }
    }
}
