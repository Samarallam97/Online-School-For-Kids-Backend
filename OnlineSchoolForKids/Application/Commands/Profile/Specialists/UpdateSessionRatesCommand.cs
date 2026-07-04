namespace Application.Commands.Profile.Specialists;

using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

public record UpdateSessionRatesCommand(string UserId, decimal HourlyRate) : IRequest;

public class UpdateSessionRatesCommandHandler : IRequestHandler<UpdateSessionRatesCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateSessionRatesCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task Handle(UpdateSessionRatesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["UserNotFound"]);

        user.HourlyRate = request.HourlyRate;

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
    }
}

public class UpdateSessionRatesDto
{
    public decimal HourlyRate { get; set; }
}