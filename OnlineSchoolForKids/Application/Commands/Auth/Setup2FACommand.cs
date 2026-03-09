using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Auth;

public record Setup2FACommand(string UserId) : IRequest<Result<Setup2FAResponse>>;

public class Setup2FAResponse
{
    public string Secret { get; set; } = string.Empty;
    public string QrUri { get; set; } = string.Empty;
}

public class Setup2FACommandHandler : IRequestHandler<Setup2FACommand, Result<Setup2FAResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;

    public Setup2FACommandHandler(IUserRepository userRepository, ITotpService totpService)
    {
        _userRepository = userRepository;
        _totpService    = totpService;
    }

    public async Task<Result<Setup2FAResponse>> Handle(Setup2FACommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return Result<Setup2FAResponse>.Failure("User not found.");

        if (user.TwoFactorEnabled == true && user.TwoFactorSecret != null)
            return Result<Setup2FAResponse>.Failure("2FA is already configured.");

        var secret = _totpService.GenerateSecret();
        var qrUri = _totpService.GetQrCodeUri(user.Email, secret);

        // Return secret temporarily — only persisted after confirmation
        return Result<Setup2FAResponse>.Success(new Setup2FAResponse
        {
            Secret = secret,
            QrUri  = qrUri,
        });
    }
}