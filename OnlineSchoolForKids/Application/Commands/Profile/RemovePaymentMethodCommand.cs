using Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

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
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != Domain.Enums.UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        if (parent.PaymentMethods == null || !parent.PaymentMethods.Any())
            throw new KeyNotFoundException("No payment methods found");

        var paymentMethod = parent.PaymentMethods.FirstOrDefault(pm => pm.Id == request.PaymentMethodId);
        if (paymentMethod == null)
            throw new KeyNotFoundException("Payment method not found");

        var wasDefault = paymentMethod.IsDefault;
        parent.PaymentMethods.Remove(paymentMethod);

        // If the removed method was default, set another one as default
        if (wasDefault && parent.PaymentMethods.Any())
        {
            parent.PaymentMethods.First().IsDefault = true;
        }

        parent.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(parent.Id, parent);

        return Unit.Value;
    }
}