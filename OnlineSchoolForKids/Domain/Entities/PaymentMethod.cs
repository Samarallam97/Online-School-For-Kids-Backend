using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

//public class PaymentMethod
//{
//    public string Id { get; set; } = string.Empty;
//    public PaymentMethodType Type { get; set; }
//    public bool IsDefault { get; set; }

//    // Card details (for legacy/international cards)
//    public string? Last4 { get; set; }
//    public string? Brand { get; set; }
//    public int? ExpiryMonth { get; set; }
//    public int? ExpiryYear { get; set; }

//    // Vodafone Cash
//    public string? VodafoneNumber { get; set; }

//    // Instapay
//    public string? InstapayId { get; set; }

//    // Fawry
//    public string? FawryReferenceNumber { get; set; }

//    // Bank Account
//    public string? AccountHolderName { get; set; }
//    public string? BankName { get; set; }
//    public string? AccountNumber { get; set; }
//    public string? IBAN { get; set; }

//    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
//    public DateTime? UpdatedAt { get; set; }
//}

public enum PaymentMethodType
{
    Card = 0,           // Legacy/International cards
    VodafoneCash = 1,
    Instapay = 2,
    Fawry = 3,
    BankAccount = 4
}

