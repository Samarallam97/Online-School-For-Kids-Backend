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

public record DisableRequest(string Code);

public record Disable2FACommand(string UserId, string Code) : IRequest<Result<string>>;

public class Disable2FACommandHandler : IRequestHandler<Disable2FACommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;

    public Disable2FACommandHandler(IUserRepository userRepository, ITotpService totpService)
    {
        _userRepository = userRepository;
        _totpService    = totpService;
    }

    public async Task<Result<string>> Handle(Disable2FACommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return Result<string>.Failure("User not found.");

        if (user.TwoFactorEnabled != true)
            return Result<string>.Failure("2FA is not enabled on this account.");

        if (!_totpService.ValidateCode(user.TwoFactorSecret, request.Code))
            return Result<string>.Failure("Invalid code. Cannot disable 2FA.");

        user.TwoFactorSecret  = null;
        user.TwoFactorEnabled = false;
        await _userRepository.UpdateAsync(user.Id, user, ct);

        return Result<string>.Success("2FA has been disabled.");
    }
}