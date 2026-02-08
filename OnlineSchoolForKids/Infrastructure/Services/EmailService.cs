using Application.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger )
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            throw;
        }
    }

    public async Task SendVerificationEmailAsync(string to, string userName, DateTime expiry, string verificationLink, CancellationToken cancellationToken = default)
    {
        var subject = "Verify Your Email Address";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Hello {userName},</h2>
                <p>Thank you for registering! Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}' style='background-color: #4CAF50; color: white; padding: 14px 20px; text-decoration: none; border-radius: 4px;'>Verify Email</a></p>
                <p>If you didn't create this account, please ignore this email.</p>
                <p>This link will expire at {expiry}</p>
                <br/>
                <p>Best regards,<br/>The Team</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string to, string userName, DateTime expiry, string resetLink, CancellationToken cancellationToken = default)
    {
        var subject = "Reset Your Password";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Hello {userName},</h2>
                <p>We received a request to reset your password. Click the link below to create a new password:</p>
                <p><a href='{resetLink}' style='background-color: #2196F3; color: white; padding: 14px 20px; text-decoration: none; border-radius: 4px;'>Reset Password</a></p>
                <p>If you didn't request this, please ignore this email and your password will remain unchanged.</p>
                <p>This link will expire at {expiry}.</p>
                <br/>
                <p>Best regards,<br/>The Team</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, cancellationToken);
    }

    public async Task SendPasswordChangedEmailAsync(string email)
    {
        var subject = "Password Changed Successfully";
        var body = $@"
            <h2>Password Changed</h2>
            <p>Your password has been changed successfully.</p>
            <p>If you didn't make this change, please contact our support team immediately.</p>
            <p>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        ";

        await SendEmailAsync(email, subject, body);
    }
    public async Task SendWelcomeEmailAsync(string to, string userName, CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to Our Platform!";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome {userName}!</h2>
                <p>Your email has been verified successfully. Thank you for joining us!</p>
                <p>You can now access all features of our platform.</p>
                <br/>
                <p>Best regards,<br/>The Team</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, cancellationToken);
    }

    public async Task SendParentLinkInvitationAsync(string childEmail, string childName, string parentName, string inviteLink , CancellationToken cancellationToken = default)
    {
        var subject = "Parent Invitation";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Hello {childName},</h2>
                <p>{parentName} asked to link your account</p>
                <p><a href='{inviteLink}' style='background-color: #4CAF50; color: white; padding: 14px 20px; text-decoration: none; border-radius: 4px;'>Accept Invite</a></p>
                <p>If you didn't know them, please ignore this email.</p>
                <br/>
                <p>Best regards,<br/>The Team</p>
            </body>
            </html>";

        await SendEmailAsync(childEmail, subject, body, true, cancellationToken);
    }

    public async Task SendParentLinkedNotificationAsync(string childEmail, string childName, string parentName , CancellationToken cancellationToken = default)
    {
        var subject = "Student Invitation Acceptance";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Hello {childName},</h2>
                <p>you accepted the invite sent from {parentName}</p>
                <br/>
                <p>Best regards,<br/>The Team</p>
            </body>
            </html>";

        await SendEmailAsync(childEmail, subject, body, true, cancellationToken);
    }
}

