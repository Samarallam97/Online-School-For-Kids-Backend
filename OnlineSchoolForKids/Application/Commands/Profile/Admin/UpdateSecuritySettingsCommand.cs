namespace Application.Commands.Profile.Admin;

using Application.Queries.Profile.Admin;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

public record UpdateSecuritySettingsRequest(
    bool LoginNotifications,
    bool SuspiciousActivityAlerts
);

public record UpdateSecuritySettingsCommand(
    string UserId,
    bool LoginNotifications,
    bool SuspiciousActivityAlerts
) : IRequest<AdminSecuritySettingsDto>;


public class UpdateSecuritySettingsCommandHandler : IRequestHandler<UpdateSecuritySettingsCommand, AdminSecuritySettingsDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateSecuritySettingsCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<AdminSecuritySettingsDto> Handle(UpdateSecuritySettingsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["UserNotFound"]);

        user.LoginNotifications = request.LoginNotifications;
        user.SuspiciousActivityAlerts = request.SuspiciousActivityAlerts;

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        return new AdminSecuritySettingsDto
        {
            TwoFactorEnabled = user.TwoFactorEnabled ?? false,
            LoginNotifications = user.LoginNotifications ?? false,
            SuspiciousActivityAlerts = user.SuspiciousActivityAlerts ?? false,
        };
    }
}