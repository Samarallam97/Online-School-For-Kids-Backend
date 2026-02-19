using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries.Profile.Admin;

public record GetSecuritySettingsQuery(string UserId) : IRequest<AdminSecuritySettingsDto>;

public class GetSecuritySettingsQueryHandler : IRequestHandler<GetSecuritySettingsQuery, AdminSecuritySettingsDto>
{
    private readonly IUserRepository _userRepository;

    public GetSecuritySettingsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AdminSecuritySettingsDto> Handle(GetSecuritySettingsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return new AdminSecuritySettingsDto
        {
            TwoFactorEnabled = user.TwoFactorEnabled ?? false,
            LoginNotifications = user.LoginNotifications ?? false,
            SuspiciousActivityAlerts = user.SuspiciousActivityAlerts ?? false,
        };
    }
}

public class AdminSecuritySettingsDto
{
    public bool TwoFactorEnabled { get; set; }
    public bool LoginNotifications { get; set; }
    public bool SuspiciousActivityAlerts { get; set; }
}
