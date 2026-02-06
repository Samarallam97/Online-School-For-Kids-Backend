using Application.Interfaces;
using Application.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Auth;

public record LogOutCommand(string userId) : IRequest<Result<string>>;


public class LogOutCommandHandler
    : IRequestHandler<LogOutCommand, Result<string>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogOutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<string>> Handle(
        LogOutCommand request,
        CancellationToken cancellationToken)
    {
        await _refreshTokenRepository.RevokeAllUserTokensAsync(
            request.userId,
            cancellationToken);

        return Result<string>.Success("Logged out successfully.");
    }
}

