using Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

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

        // Set all to non-default
        foreach (var pm in parent.PaymentMethods)
        {
            pm.IsDefault = false;
        }

        // Set the selected one as default
        paymentMethod.IsDefault = true;
        parent.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(parent.Id, parent);

        return Unit.Value;
    }
}