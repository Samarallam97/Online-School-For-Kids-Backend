using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Users;

public class RemovePaymentMethodCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class RemovePaymentMethodCommandHandler : IRequestHandler<RemovePaymentMethodCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public RemovePaymentMethodCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(RemovePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("user not found");


        if (user.PaymentMethods == null || !user.PaymentMethods.Any())
            throw new KeyNotFoundException("No payment methods found");

        var paymentMethod = user.PaymentMethods.FirstOrDefault(pm => pm.Id == request.PaymentMethodId);
        if (paymentMethod == null)
            throw new KeyNotFoundException("Payment method not found");

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