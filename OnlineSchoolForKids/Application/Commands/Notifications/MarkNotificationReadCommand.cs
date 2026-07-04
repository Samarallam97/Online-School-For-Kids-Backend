using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Notifications
{
    public record MarkNotificationReadCommand(string NotificationId, string UserId)
     : IRequest<Result<string>>;

    public class MarkNotificationReadCommandHandler
        : IRequestHandler<MarkNotificationReadCommand, Result<string>>
    {
        private readonly INotificationRepository _repo;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public MarkNotificationReadCommandHandler(INotificationRepository repo, IStringLocalizer<SharedResource> localizer)
        {
            _repo = repo;
            _localizer = localizer;
        }

        public async Task<Result<string>> Handle(MarkNotificationReadCommand request, CancellationToken ct)
        {
            var notification = await _repo.GetByIdAsync(request.NotificationId, ct);
            if (notification is null) return Result<string>.Failure(_localizer["NotificationNotFound"]);
            if (notification.UserId != request.UserId) return Result<string>.Failure(_localizer["AccessDenied"]);

            await _repo.MarkAsReadAsync(request.NotificationId, ct);
            return Result<string>.Success(_localizer["NotificationMarkedAsRead"]);
        }
    }

    public record MarkAllNotificationsReadCommand(string UserId)
        : IRequest<Result<string>>;

    public class MarkAllNotificationsReadCommandHandler
        : IRequestHandler<MarkAllNotificationsReadCommand, Result<string>>
    {
        private readonly INotificationRepository _repo;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public MarkAllNotificationsReadCommandHandler(INotificationRepository repo, IStringLocalizer<SharedResource> localizer)
        {
            _repo = repo;
            _localizer = localizer;
        }

        public async Task<Result<string>> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
        {
            await _repo.MarkAllAsReadAsync(request.UserId, ct);
            return Result<string>.Success(_localizer["AllNotificationsMarkedAsRead"]);
        }
    }
}