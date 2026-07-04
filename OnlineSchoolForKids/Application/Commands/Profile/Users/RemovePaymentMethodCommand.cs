using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Users;

public class RemovePaymentMethodCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class RemovePaymentMethodCommandHandler : IRequestHandler<RemovePaymentMethodCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RemovePaymentMethodCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<Unit> Handle(RemovePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);


        if (user.PaymentMethods == null || !user.PaymentMethods.Any())
            throw new KeyNotFoundException(_localizer["PaymentMethodNotFound"]);

        var paymentMethod = user.PaymentMethods.FirstOrDefault(pm => pm.Id == request.PaymentMethodId);
        if (paymentMethod == null)
            throw new KeyNotFoundException(_localizer["PaymentMethodNotFound"]);

        var wasDefault = paymentMethod.IsDefault;
        user.PaymentMethods.Remove(paymentMethod);

        // If the removed method was default, set another one as default
        if (wasDefault && user.PaymentMethods.Any())
        {
            user.PaymentMethods.First().IsDefault = true;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);

        return Unit.Value;
    }
}