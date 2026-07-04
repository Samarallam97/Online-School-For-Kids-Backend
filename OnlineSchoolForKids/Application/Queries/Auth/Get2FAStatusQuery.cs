using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public Get2FAStatusQueryHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<Result<TwoFactorStatusResponse>> Handle(Get2FAStatusQuery request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return Result<TwoFactorStatusResponse>.Failure(_localizer["UserNotFound"]);

        return Result<TwoFactorStatusResponse>.Success(new TwoFactorStatusResponse
        {
            IsEnabled = user.TwoFactorEnabled == true,
            IsConfigured = user.TwoFactorSecret != null,
        });
    }
}