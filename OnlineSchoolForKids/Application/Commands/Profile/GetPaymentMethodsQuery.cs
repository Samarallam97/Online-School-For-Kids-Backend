using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

public class GetPaymentMethodsQuery : IRequest<List<PaymentMethodDto>>
{
    public string UserId { get; set; } = string.Empty;
}

public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, List<PaymentMethodDto>>
{
    private readonly IUserRepository _userRepository;

    public GetPaymentMethodsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<PaymentMethodDto>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != Domain.Enums.UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        var paymentMethods = parent.PaymentMethods?.Select(pm => MapToDto(pm)).ToList()
            ?? new List<PaymentMethodDto>();

        return paymentMethods;
    }

    private PaymentMethodDto MapToDto(PaymentMethod paymentMethod)
    {
        var dto = new PaymentMethodDto
        {
            Id = paymentMethod.Id,
            Type = GetPaymentTypeString(paymentMethod.Type),
            IsDefault = paymentMethod.IsDefault,
            DisplayInfo = GetDisplayInfo(paymentMethod)
        };

        // Include legacy card fields for backward compatibility
        if (paymentMethod.Type == PaymentMethodType.Card)
        {
            dto.Last4 = paymentMethod.Last4;
            dto.Brand = paymentMethod.Brand;
            dto.ExpiryMonth = paymentMethod.ExpiryMonth;
            dto.ExpiryYear = paymentMethod.ExpiryYear;
        }

        return dto;
    }

    private string GetPaymentTypeString(PaymentMethodType type)
    {
        return type switch
        {
            PaymentMethodType.Card => "card",
            PaymentMethodType.VodafoneCash => "vodafone_cash",
            PaymentMethodType.Instapay => "instapay",
            PaymentMethodType.Fawry => "fawry",
            PaymentMethodType.BankAccount => "bank_account",
            _ => "unknown"
        };
    }

    private string GetDisplayInfo(PaymentMethod paymentMethod)
    {
        return paymentMethod.Type switch
        {
            PaymentMethodType.Card => $"{paymentMethod.Brand} •••• {paymentMethod.Last4}",
            PaymentMethodType.VodafoneCash => $"Vodafone Cash - {MaskPhoneNumber(paymentMethod.VodafoneNumber)}",
            PaymentMethodType.Instapay => $"Instapay - {MaskString(paymentMethod.InstapayId)}",
            PaymentMethodType.Fawry => $"Fawry - {MaskString(paymentMethod.FawryReferenceNumber)}",
            PaymentMethodType.BankAccount => $"{paymentMethod.BankName} - {MaskAccountNumber(paymentMethod.AccountNumber)}",
            _ => "Unknown Payment Method"
        };
    }

    private string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        return $"{phoneNumber.Substring(0, 4)}*****{phoneNumber.Substring(phoneNumber.Length - 2)}";
    }

    private string MaskString(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 4)
            return "****";

        return $"{value.Substring(0, 2)}****{value.Substring(value.Length - 2)}";
    }

    private string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length < 4)
            return "****";

        return $"****{accountNumber.Substring(accountNumber.Length - 4)}";
    }
}