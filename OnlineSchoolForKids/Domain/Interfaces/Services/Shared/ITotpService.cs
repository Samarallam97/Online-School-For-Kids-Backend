namespace Domain.Interfaces.Services.Shared;

public interface ITotpService
{
    string GenerateSecret();
    string GetQrCodeUri(string email, string secret);
    bool ValidateCode(string secret, string code);
}
