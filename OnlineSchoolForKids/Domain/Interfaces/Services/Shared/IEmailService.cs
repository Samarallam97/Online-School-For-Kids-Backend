namespace Domain.Interfaces.Services.Shared;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task SendVerificationEmailAsync(string to, string userName, DateTime expiry, string verificationLink, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string to, string userName, DateTime expiry, string resetLink, CancellationToken cancellationToken = default);
    Task SendPasswordChangedEmailAsync(string email);
    Task SendWelcomeEmailAsync(string to, string userName, CancellationToken cancellationToken = default);

    Task SendParentLinkInvitationAsync(string childEmail, string childName, string parentName, string inviteToken, CancellationToken cancellationToken = default);

    Task SendParentLinkAcceptedNotificationAsync(
        string parentEmail,
        string parentName,
        string childName,
        string childProgressUrl);
}
