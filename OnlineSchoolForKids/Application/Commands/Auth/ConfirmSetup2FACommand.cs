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

    public ConfirmSetup2FACommandHandler(IUserRepository userRepository, ITotpService totpService)
    {
        _userRepository = userRepository;
        _totpService    = totpService;
    }

    public async Task<Result<string>> Handle(ConfirmSetup2FACommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return Result<string>.Failure("User not found.");

        if (!_totpService.ValidateCode(request.Secret, request.Code))
            return Result<string>.Failure("Invalid code. Please try again.");

        user.TwoFactorSecret  = request.Secret;
        user.TwoFactorEnabled = true;
        await _userRepository.UpdateAsync(user.Id, user, ct);

        return Result<string>.Success("2FA has been enabled successfully.");
    }
}