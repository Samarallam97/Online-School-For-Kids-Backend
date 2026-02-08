using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

public class AddPaymentMethodCommand : IRequest<PaymentMethodDto>
{
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "card", "vodafone_cash", "instapay", "fawry", "bank_account"

    // Card fields (legacy/international)
    public string? CardNumber { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? Cvc { get; set; }
    public string? CardholderName { get; set; }

    // Vodafone Cash
    public string? PhoneNumber { get; set; }

    // Instapay
    public string? InstapayId { get; set; }

    // Fawry
    public string? ReferenceNumber { get; set; }

    // Bank Account
    public string? AccountHolderName { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }
}

public class AddPaymentMethodCommandHandler : IRequestHandler<AddPaymentMethodCommand, PaymentMethodDto>
{
    private readonly IUserRepository _userRepository;

    public AddPaymentMethodCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PaymentMethodDto> Handle(AddPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != Domain.Enums.UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid().ToString(),
            IsDefault = parent.PaymentMethods == null || !parent.PaymentMethods.Any(),
            CreatedAt = DateTime.UtcNow
        };

        // Process based on payment type
        switch (request.Type.ToLower())
        {
            case "card":
                paymentMethod.Type = PaymentMethodType.Card;
                ProcessCardPayment(paymentMethod, request);
                break;

            case "vodafone_cash":
                paymentMethod.Type = PaymentMethodType.VodafoneCash;
                ProcessVodafoneCash(paymentMethod, request);
                break;

            case "instapay":
                paymentMethod.Type = PaymentMethodType.Instapay;
                ProcessInstapay(paymentMethod, request);
                break;

            case "fawry":
                paymentMethod.Type = PaymentMethodType.Fawry;
                ProcessFawry(paymentMethod, request);
                break;

            case "bank_account":
                paymentMethod.Type = PaymentMethodType.BankAccount;
                ProcessBankAccount(paymentMethod, request);
                break;

            default:
                throw new ArgumentException($"Unsupported payment type: {request.Type}");
        }

        if (parent.PaymentMethods == null)
            parent.PaymentMethods = new List<PaymentMethod>();

        parent.PaymentMethods.Add(paymentMethod);
        parent.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(parent.Id, parent);

        return MapToDto(paymentMethod);
    }

    private void ProcessCardPayment(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException("Card number is required for card payment type");

        if (!request.ExpiryMonth.HasValue || !request.ExpiryYear.HasValue)
            throw new ArgumentException("Expiry date is required for card payment type");

        if (request.ExpiryMonth.Value < 1 || request.ExpiryMonth.Value > 12)
            throw new ArgumentException("Invalid expiry month");

        if (request.ExpiryYear.Value < DateTime.UtcNow.Year)
            throw new ArgumentException("Card has expired");

        var last4 = request.CardNumber.Length >= 4
            ? request.CardNumber.Substring(request.CardNumber.Length - 4)
            : request.CardNumber;

        var brand = DetermineCardBrand(request.CardNumber);

        paymentMethod.Last4 = last4;
        paymentMethod.Brand = brand;
        paymentMethod.ExpiryMonth = request.ExpiryMonth.Value;
        paymentMethod.ExpiryYear = request.ExpiryYear.Value;
    }

    private void ProcessVodafoneCash(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("Phone number is required for Vodafone Cash");

        // Validate Egyptian phone number format (01XXXXXXXXX)
        var cleanNumber = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());
        if (!cleanNumber.StartsWith("01") || cleanNumber.Length != 11)
            throw new ArgumentException("Invalid Egyptian phone number format");

        paymentMethod.VodafoneNumber = cleanNumber;
    }

    private void ProcessInstapay(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.InstapayId))
            throw new ArgumentException("Instapay ID is required");

        paymentMethod.InstapayId = request.InstapayId.Trim();
    }

    private void ProcessFawry(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenceNumber))
            throw new ArgumentException("Fawry reference number is required");

        paymentMethod.FawryReferenceNumber = request.ReferenceNumber.Trim();
    }

    private void ProcessBankAccount(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountHolderName))
            throw new ArgumentException("Account holder name is required for bank account");

        if (string.IsNullOrWhiteSpace(request.BankName))
            throw new ArgumentException("Bank name is required for bank account");

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new ArgumentException("Account number is required for bank account");

        paymentMethod.AccountHolderName = request.AccountHolderName.Trim();
        paymentMethod.BankName = request.BankName.Trim();
        paymentMethod.AccountNumber = request.AccountNumber.Trim();
        paymentMethod.IBAN = request.IBAN?.Trim();
    }

    private string DetermineCardBrand(string cardNumber)
    {
        var cleanNumber = new string(cardNumber.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(cleanNumber))
            return "Unknown";

        if (cleanNumber.StartsWith("4"))
            return "Visa";
        if (cleanNumber.StartsWith("5"))
            return "Mastercard";
        if (cleanNumber.StartsWith("3"))
            return "American Express";
        if (cleanNumber.StartsWith("6"))
            return "Discover";

        return "Unknown";
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

public class AddPaymentMethodCommandValidator : AbstractValidator<AddPaymentMethodCommand>
{
    public AddPaymentMethodCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Payment type is required")
            .Must(type => new[] { "card", "vodafone_cash", "instapay", "fawry", "bank_account" }.Contains(type.ToLower()))
            .WithMessage("Invalid payment type");

        // Card validation
        When(x => x.Type.ToLower() == "card", () =>
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .WithMessage("Card number is required")
                .Matches(@"^\d+$")
                .WithMessage("Card number must contain only digits")
                .Length(13, 19)
                .WithMessage("Card number must be between 13 and 19 digits");

            RuleFor(x => x.ExpiryMonth)
                .NotNull()
                .WithMessage("Expiry month is required")
                .InclusiveBetween(1, 12)
                .WithMessage("Expiry month must be between 1 and 12");

            RuleFor(x => x.ExpiryYear)
                .NotNull()
                .WithMessage("Expiry year is required")
                .GreaterThanOrEqualTo(DateTime.UtcNow.Year)
                .WithMessage("Card has expired");

            RuleFor(x => x.Cvc)
                .NotEmpty()
                .WithMessage("CVC is required")
                .Matches(@"^\d{3,4}$")
                .WithMessage("CVC must be 3 or 4 digits");

            RuleFor(x => x.CardholderName)
                .NotEmpty()
                .WithMessage("Cardholder name is required")
                .MaximumLength(100)
                .WithMessage("Cardholder name must not exceed 100 characters");
        });

        // Vodafone Cash validation
        When(x => x.Type.ToLower() == "vodafone_cash", () =>
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Phone number is required")
                .Matches(@"^01[0-9]{9}$")
                .WithMessage("Invalid Egyptian phone number format (must be 01XXXXXXXXX)");
        });

        // Instapay validation
        When(x => x.Type.ToLower() == "instapay", () =>
        {
            RuleFor(x => x.InstapayId)
                .NotEmpty()
                .WithMessage("Instapay ID is required")
                .MaximumLength(100)
                .WithMessage("Instapay ID must not exceed 100 characters");
        });

        // Fawry validation
        When(x => x.Type.ToLower() == "fawry", () =>
        {
            RuleFor(x => x.ReferenceNumber)
                .NotEmpty()
                .WithMessage("Fawry reference number is required")
                .MaximumLength(50)
                .WithMessage("Reference number must not exceed 50 characters");
        });

        // Bank Account validation
        When(x => x.Type.ToLower() == "bank_account", () =>
        {
            RuleFor(x => x.AccountHolderName)
                .NotEmpty()
                .WithMessage("Account holder name is required")
                .MaximumLength(100)
                .WithMessage("Account holder name must not exceed 100 characters");

            RuleFor(x => x.BankName)
                .NotEmpty()
                .WithMessage("Bank name is required")
                .MaximumLength(100)
                .WithMessage("Bank name must not exceed 100 characters");

            RuleFor(x => x.AccountNumber)
                .NotEmpty()
                .WithMessage("Account number is required")
                .MaximumLength(50)
                .WithMessage("Account number must not exceed 50 characters");

            RuleFor(x => x.IBAN)
                .MaximumLength(34)
                .WithMessage("IBAN must not exceed 34 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.IBAN));
        });
    }
}
