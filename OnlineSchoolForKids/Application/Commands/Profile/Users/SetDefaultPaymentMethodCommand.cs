using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Users;

public class SetDefaultPaymentMethodCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class SetDefaultPaymentMethodCommandHandler : IRequestHandler<SetDefaultPaymentMethodCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public SetDefaultPaymentMethodCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(SetDefaultPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("user not found");

        if (user.PaymentMethods == null || !user.PaymentMethods.Any())
            throw new KeyNotFoundException("No payment methods found");

        var paymentMethod = user.PaymentMethods.FirstOrDefault(pm => pm.Id == request.PaymentMethodId);
        if (paymentMethod == null)
            throw new KeyNotFoundException("Payment method not found");

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