using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries.Auth;

public record Get2FAStatusQuery(string UserId) : IRequest<Result<TwoFactorStatusResponse>>;

public class TwoFactorStatusResponse
{
    public bool IsEnabled { get; set; }
    public bool IsConfigured { get; set; }
}

public class Get2FAStatusQueryHandler : IRequestHandler<Get2FAStatusQuery, Result<TwoFactorStatusResponse>>
{
    private readonly IUserRepository _userRepository;

    public Get2FAStatusQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<TwoFactorStatusResponse>> Handle(Get2FAStatusQuery request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return Result<TwoFactorStatusResponse>.Failure("User not found.");

        return Result<TwoFactorStatusResponse>.Success(new TwoFactorStatusResponse
        {
            IsEnabled    = user.TwoFactorEnabled == true,
            IsConfigured = user.TwoFactorSecret  != null,
        });
    }
}