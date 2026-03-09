using Domain.Entities;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;

namespace Application.Queries.Profile.Users;

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
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var paymentMethods = user.PaymentMethods?.Select(pm => MapToDto(pm)).ToList()
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

        // Add card-specific details to DTO
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
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 6)
            return "****";

        return $"{phoneNumber[..4]}*****{phoneNumber[^2..]}";
    }

    private string MaskString(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 4)
            return "****";

        return $"{value[..2]}****{value[^2..]}";
    }

    private string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length < 4)
            return "****";

        return $"****{accountNumber[^4..]}";
    }
}

public class PaymentMethodDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "card", "vodafone_cash", "instapay", "fawry", "bank_account"
    public string DisplayInfo { get; set; } = string.Empty; // User-friendly display text
    public bool IsDefault { get; set; }
    public string? Last4 { get; set; }
    public string? Brand { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
}