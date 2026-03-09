using Application.DTOs;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Auth;

public record RefreshTokenRequest(string RefreshToken);

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null
) : IRequest<Result<AuthResponse>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken, request.IpAddress);

        if (result == null)
        {
            return Result<AuthResponse>.Failure("Invalid or expired refresh token.");
        }

        // Note: Full user info would be fetched in the JWT service implementation
        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = result.Value.AccessToken,
            RefreshToken = result.Value.RefreshToken,
            User = new UserDto(), // Would be populated from token claims
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }
}
