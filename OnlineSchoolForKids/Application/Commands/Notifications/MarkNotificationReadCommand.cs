using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using MediatR;

namespace Application.Commands.Notifications
{
    public record MarkNotificationReadCommand(string NotificationId, string UserId)
     : IRequest<Result<string>>;

    public class MarkNotificationReadCommandHandler
        : IRequestHandler<MarkNotificationReadCommand, Result<string>>
    {
        private readonly INotificationRepository _repo;
        public MarkNotificationReadCommandHandler(INotificationRepository repo) => _repo = repo;

        public async Task<Result<string>> Handle(MarkNotificationReadCommand request, CancellationToken ct)
        {
            var notification = await _repo.GetByIdAsync(request.NotificationId, ct);
            if (notification is null) return Result<string>.Failure("Notification not found.");
            if (notification.UserId != request.UserId) return Result<string>.Failure("Access denied.");

            await _repo.MarkAsReadAsync(request.NotificationId, ct);
            return Result<string>.Success("Notification marked as read.");
        }
    }

    public record MarkAllNotificationsReadCommand(string UserId)
        : IRequest<Result<string>>;

    public class MarkAllNotificationsReadCommandHandler
        : IRequestHandler<MarkAllNotificationsReadCommand, Result<string>>
    {
        private readonly INotificationRepository _repo;
        public MarkAllNotificationsReadCommandHandler(INotificationRepository repo) => _repo = repo;

        public async Task<Result<string>> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
        {
            await _repo.MarkAllAsReadAsync(request.UserId, ct);
            return Result<string>.Success("All notifications marked as read.");
        }
    }
}