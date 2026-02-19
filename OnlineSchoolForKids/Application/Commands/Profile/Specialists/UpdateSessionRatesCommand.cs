using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Specialists;

using Domain.Interfaces.Repositories.Users;
using MediatR;

public record UpdateSessionRatesCommand(string UserId, decimal HourlyRate) : IRequest;

public class UpdateSessionRatesCommandHandler : IRequestHandler<UpdateSessionRatesCommand>
{
    private readonly IUserRepository _userRepository;

    public UpdateSessionRatesCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(UpdateSessionRatesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        user.HourlyRate = request.HourlyRate;

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
    }
}

public class UpdateSessionRatesDto
{
    public decimal HourlyRate { get; set; }
}