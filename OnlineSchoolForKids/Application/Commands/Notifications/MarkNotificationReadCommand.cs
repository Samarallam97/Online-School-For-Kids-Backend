using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;


namespace Application.Commands.Notifications
{
    public record MarkNotificationReadCommand(string NotificationId, string UserId)
     : IRequest<Result<string>>;

    public class MarkNotificationReadCommandHandler
        : IRequestHandler<MarkNotificationReadCommand, Result<string>>
    {
        private readonly INotificationRepository _repo;


        public MarkNotificationReadCommandHandler(INotificationRepository repo)
        {
            _repo = repo;

        }

        public async Task<Result<string>> Handle(MarkNotificationReadCommand request, CancellationToken ct)
        {
            var notification = await _repo.GetByIdAsync(request.NotificationId, ct);
            if (notification is null) return Result<string>.Failure("Notification Not Found");
            if (notification.UserId != request.UserId) return Result<string>.Failure("Access Denied");

            await _repo.MarkAsReadAsync(request.NotificationId, ct);
            return Result<string>.Success("Notification Marked As Read");
        }
    }

    public record MarkAllNotificationsReadCommand(string UserId)
        : IRequest<Result<string>>;

    public class MarkAllNotificationsReadCommandHandler
        : IRequestHandler<MarkAllNotificationsReadCommand, Result<string>>
    {
        private readonly INotificationRepository _repo;


        public MarkAllNotificationsReadCommandHandler(INotificationRepository repo)
        {
            _repo = repo;

        }

        public async Task<Result<string>> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
        {
            await _repo.MarkAllAsReadAsync(request.UserId, ct);
            return Result<string>.Success("All Notifications Marked As Read");
        }
    }
}