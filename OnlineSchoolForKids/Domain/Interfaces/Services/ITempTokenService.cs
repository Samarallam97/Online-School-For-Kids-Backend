using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces.Services;

public interface ITempTokenService
{
    Task<string> StorePendingGoogleUserAsync(PendingGoogleUser user);
    Task<PendingGoogleUser?> GetPendingGoogleUserAsync(string token);
    Task DeletePendingGoogleUserAsync(string token);
}

public class PendingGoogleUser
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool EmailVerified { get; set; }
}