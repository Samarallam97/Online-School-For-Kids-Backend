using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Auth;

public record LogOutCommand(string userId) : IRequest<Result<string>>;


public class LogOutCommandHandler
    : IRequestHandler<LogOutCommand, Result<string>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LogOutCommandHandler(IRefreshTokenRepository refreshTokenRepository, IStringLocalizer<SharedResource> localizer)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _localizer = localizer;
    }

    public async Task<Result<string>> Handle(
        LogOutCommand request,
        CancellationToken cancellationToken)
    {
        await _refreshTokenRepository.RevokeAllUserTokensAsync(
            request.userId,
            cancellationToken);

        return Result<string>.Success(_localizer["LoggedOutSuccessfully"]);
    }
}

