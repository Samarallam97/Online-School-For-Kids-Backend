using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Auth;

public record DisableRequest(string Code);

public record Disable2FACommand(string UserId, string Code) : IRequest<Result<string>>;

public class Disable2FACommandHandler : IRequestHandler<Disable2FACommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public Disable2FACommandHandler(IUserRepository userRepository, ITotpService totpService, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _localizer = localizer;
    }

    public async Task<Result<string>> Handle(Disable2FACommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return Result<string>.Failure(_localizer["UserNotFound"]);

        if (user.TwoFactorEnabled != true)
            return Result<string>.Failure(_localizer["TwoFactorNotEnabled"]);

        if (!_totpService.ValidateCode(user.TwoFactorSecret, request.Code))
            return Result<string>.Failure(_localizer["InvalidCodeCannotDisableTwoFactor"]);

        user.TwoFactorSecret = null;
        user.TwoFactorEnabled = false;
        await _userRepository.UpdateAsync(user.Id, user, ct);

        return Result<string>.Success(_localizer["TwoFactorDisabledSuccessfully"]);
    }
}