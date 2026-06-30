using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using MediatR;

namespace Application.Commands.Notifications
{
    public record NotificationDto(
     string Id,
     string Title,
     string Message,
     string Type,
     string? ActionUrl,
     bool IsRead,
     DateTime CreatedAt);

    public record GetNotificationsRequest(int Page = 1, int PageSize = 20);

    public record GetNotificationsQuery(string UserId, int Page, int PageSize)
        : IRequest<Result<(IEnumerable<NotificationDto> Items, long TotalCount)>>;

    public record GetUnreadCountQuery(string UserId)
        : IRequest<Result<int>>;

    public class GetNotificationsQueryHandler
        : IRequestHandler<GetNotificationsQuery, Result<(IEnumerable<NotificationDto> Items, long TotalCount)>>
    {
        private readonly INotificationRepository _repo;
        public GetNotificationsQueryHandler(INotificationRepository repo) => _repo = repo;

        public async Task<Result<(IEnumerable<NotificationDto> Items, long TotalCount)>> Handle(
            GetNotificationsQuery request, CancellationToken ct)
        {
            var pageSize = Math.Clamp(request.PageSize, 1, 50);
            var skip = (Math.Max(request.Page, 1) - 1) * pageSize;

            var (items, total) = await _repo.GetByUserIdPagedAsync(request.UserId, skip, pageSize, ct);

            var dtos = items.Select(n => new NotificationDto(
                n.Id, n.Title, n.Message, n.Type.ToString(), n.ActionUrl, n.IsRead, n.CreatedAt));

            return Result<(IEnumerable<NotificationDto>, long)>.Success((dtos, total));
        }
    }

    public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
    {
        private readonly INotificationRepository _repo;
        public GetUnreadCountQueryHandler(INotificationRepository repo) => _repo = repo;

        public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken ct)
            => Result<int>.Success(await _repo.GetUnreadCountAsync(request.UserId, ct));
    }
}
