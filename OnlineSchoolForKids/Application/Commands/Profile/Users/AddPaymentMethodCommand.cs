using Application.Queries.Profile.Users;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using FluentValidation;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Users;

public class AddPaymentMethodCommand : IRequest<PaymentMethodDto>
{
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "card", "vodafone_cash", "instapay", "fawry", "bank_account"

    // Card fields (legacy/international)
    public string? CardNumber { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? Cvv { get; set; }
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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AddPaymentMethodCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<PaymentMethodDto> Handle(AddPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException(_localizer["userNotFound"]);


        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid().ToString(),
            IsDefault = user.PaymentMethods == null || !user.PaymentMethods.Any(),
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

        if (user.PaymentMethods == null)
            user.PaymentMethods = new List<PaymentMethod>();

        user.PaymentMethods.Add(paymentMethod);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);

        return MapToDto(paymentMethod);
    }

    private void ProcessCardPayment(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException(_localizer["CardNumberRequired"]);

        if (!request.ExpiryMonth.HasValue || !request.ExpiryYear.HasValue)
            throw new ArgumentException(_localizer["CardExpiryDateRequired"]);

        if (request.ExpiryMonth.Value < 1 || request.ExpiryMonth.Value > 12)
            throw new ArgumentException(_localizer["InvalidExpiryMonth"]);

        if (request.ExpiryYear.Value < DateTime.UtcNow.Year)
            throw new ArgumentException(_localizer["CardExpired"]);

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
            throw new ArgumentException(_localizer["VodafoneCashPhoneRequired"]);

        // Validate Egyptian phone number format (01XXXXXXXXX)
        var cleanNumber = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());
        if (!cleanNumber.StartsWith("01") || cleanNumber.Length != 11)
            throw new ArgumentException(_localizer["InvalidEgyptianPhoneNumber"]);

        paymentMethod.VodafoneNumber = cleanNumber;
    }

    private void ProcessInstapay(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.InstapayId))
            throw new ArgumentException(_localizer["InstapayIdRequired"]);

        paymentMethod.InstapayId = request.InstapayId.Trim();
    }

    private void ProcessFawry(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenceNumber))
            throw new ArgumentException(_localizer["FawryReferenceNumberRequired"]);

        paymentMethod.FawryReferenceNumber = request.ReferenceNumber.Trim();
    }

    private void ProcessBankAccount(PaymentMethod paymentMethod, AddPaymentMethodCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountHolderName))
            throw new ArgumentException(_localizer["AccountHolderNameRequired"]);

        if (string.IsNullOrWhiteSpace(request.BankName))
            throw new ArgumentException(_localizer["BankNameRequired"]);

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new ArgumentException(_localizer["AccountNumberRequired"]);

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
    public AddPaymentMethodCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(localizer["UserIdIsRequired"]);

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage(localizer["PaymentTypeRequired"])
            .Must(type => new[] { "card", "vodafone_cash", "instapay", "fawry", "bank_account" }.Contains(type.ToLower()))
            .WithMessage(localizer["InvalidPaymentType"]);

        // Card validation
        When(x => x.Type.ToLower() == "card", () =>
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .WithMessage(localizer["CardNumberRequired"])
                .Matches(@"^\d+$")
                .WithMessage(localizer["CardNumberDigitsOnly"])
                .Length(13, 19)
                .WithMessage(localizer["CardNumberLength"]);

            RuleFor(x => x.ExpiryMonth)
                .NotNull()
                .WithMessage(localizer["ExpiryMonthRequired"])
                .InclusiveBetween(1, 12)
                .WithMessage(localizer["ExpiryMonthRange"]);

            RuleFor(x => x.ExpiryYear)
                .NotNull()
                .WithMessage(localizer["ExpiryYearRequired"])
                .GreaterThanOrEqualTo(DateTime.UtcNow.Year)
                .WithMessage(localizer["CardExpired"]);

            RuleFor(x => x.Cvv)
                .NotEmpty()
                .WithMessage(localizer["CvcRequired"])
                .Matches(@"^\d{3,4}$")
                .WithMessage(localizer["CvcLength"]);

            RuleFor(x => x.CardholderName)
                .NotEmpty()
                .WithMessage(localizer["CardholderNameRequired"])
                .MaximumLength(100)
                .WithMessage(localizer["CardholderNameMaxLength"]);
        });

        // Vodafone Cash validation
        When(x => x.Type.ToLower() == "vodafone_cash", () =>
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage(localizer["PhoneNumberRequired"])
                .Matches(@"^01[0-9]{9}$")
                .WithMessage(localizer["InvalidEgyptianPhoneNumber"]);
        });

        // Instapay validation
        When(x => x.Type.ToLower() == "instapay", () =>
        {
            RuleFor(x => x.InstapayId)
                .NotEmpty()
                .WithMessage(localizer["InstapayIdRequired"])
                .MaximumLength(100)
                .WithMessage(localizer["InstapayIdMaxLength"]);
        });

        // Fawry validation
        When(x => x.Type.ToLower() == "fawry", () =>
        {
            RuleFor(x => x.ReferenceNumber)
                .NotEmpty()
                .WithMessage(localizer["FawryReferenceNumberRequired"])
                .MaximumLength(50)
                .WithMessage(localizer["ReferenceNumberMaxLength"]);
        });

        // Bank Account validation
        When(x => x.Type.ToLower() == "bank_account", () =>
        {
            RuleFor(x => x.AccountHolderName)
                .NotEmpty()
                .WithMessage(localizer["AccountHolderNameRequired"])
                .MaximumLength(100)
                .WithMessage(localizer["AccountHolderNameMaxLength"]);

            RuleFor(x => x.BankName)
                .NotEmpty()
                .WithMessage(localizer["BankNameRequired"])
                .MaximumLength(100)
                .WithMessage(localizer["BankNameMaxLength"]);

            RuleFor(x => x.AccountNumber)
                .NotEmpty()
                .WithMessage(localizer["AccountNumberRequired"])
                .MaximumLength(50)
                .WithMessage(localizer["AccountNumberMaxLength"]);

            RuleFor(x => x.IBAN)
                .MaximumLength(34)
                .WithMessage(localizer["IbanMaxLength"])
                .When(x => !string.IsNullOrWhiteSpace(x.IBAN));
        });
    }
}
