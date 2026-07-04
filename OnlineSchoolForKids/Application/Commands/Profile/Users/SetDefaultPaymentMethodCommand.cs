using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Users;

public class SetDefaultPaymentMethodCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class SetDefaultPaymentMethodCommandHandler : IRequestHandler<SetDefaultPaymentMethodCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SetDefaultPaymentMethodCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<Unit> Handle(SetDefaultPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);

        if (user.PaymentMethods == null || !user.PaymentMethods.Any())
            throw new KeyNotFoundException(_localizer["PaymentMethodNotFound"]);

        var paymentMethod = user.PaymentMethods.FirstOrDefault(pm => pm.Id == request.PaymentMethodId);
        if (paymentMethod == null)
            throw new KeyNotFoundException(_localizer["PaymentMethodNotFound"]);

        // Set all to non-default
        foreach (var pm in user.PaymentMethods)
        {
            pm.IsDefault = false;
        }

        // Set the selected one as default
        paymentMethod.IsDefault = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);

        return Unit.Value;
    }
}