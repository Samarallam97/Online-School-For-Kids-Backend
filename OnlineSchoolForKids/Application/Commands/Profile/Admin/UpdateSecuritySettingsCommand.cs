using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Admin;

using Application.Queries.Profile.Admin;
using Domain.Interfaces.Repositories.Users;
using MediatR;

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

    public UpdateSecuritySettingsCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AdminSecuritySettingsDto> Handle(UpdateSecuritySettingsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

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