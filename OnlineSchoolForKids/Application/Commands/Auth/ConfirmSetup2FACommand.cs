using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Auth;

public record ConfirmSetupRequest(string Secret, string Code);
public record ConfirmSetup2FACommand(
    string UserId,
    string Secret,
    string Code
) : IRequest<Result<string>>;

public class ConfirmSetup2FACommandHandler : IRequestHandler<ConfirmSetup2FACommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ConfirmSetup2FACommandHandler(IUserRepository userRepository, ITotpService totpService, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _localizer = localizer;
    }

    public async Task<Result<string>> Handle(ConfirmSetup2FACommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return Result<string>.Failure(_localizer["UserNotFound"]);

        if (!_totpService.ValidateCode(request.Secret, request.Code))
            return Result<string>.Failure(_localizer["InvalidVerificationCode"]);


        user.TwoFactorSecret = request.Secret;
        user.TwoFactorEnabled = true;
        await _userRepository.UpdateAsync(user.Id, user, ct);

        return Result<string>.Success(_localizer["TwoFactorEnabledSuccessfully"]);
    }
}