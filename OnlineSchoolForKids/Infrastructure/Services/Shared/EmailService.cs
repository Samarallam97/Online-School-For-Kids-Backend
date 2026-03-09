using Domain.Interfaces.Services.Shared;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Shared;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
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

    public async Task SendParentLinkInvitationAsync(string childEmail, string childName, string parentName, string inviteLink, CancellationToken cancellationToken = default)
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

    public async Task SendParentLinkedNotificationAsync(string childEmail, string childName, string parentName, CancellationToken cancellationToken = default)
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

    public async Task SendParentLinkAcceptedNotificationAsync(
    string parentEmail,
    string parentName,
    string childName,
    string childProgressUrl)
    {
        var subject = $"{childName} accepted your parent link invitation!";

        var body = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    line-height: 1.6;
                    color: #333;
                }}
                .container {{
                    max-width: 600px;
                    margin: 0 auto;
                    padding: 20px;
                }}
                .header {{
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 30px;
                    text-align: center;
                    border-radius: 10px 10px 0 0;
                }}
                .content {{
                    background: #f9fafb;
                    padding: 30px;
                    border-radius: 0 0 10px 10px;
                }}
                .success-badge {{
                    background: #10b981;
                    color: white;
                    padding: 10px 20px;
                    border-radius: 20px;
                    display: inline-block;
                    margin: 20px 0;
                    font-weight: bold;
                }}
                .button {{
                    display: inline-block;
                    background: #667eea;
                    color: white;
                    padding: 12px 30px;
                    text-decoration: none;
                    border-radius: 5px;
                    margin: 20px 0;
                }}
                .info-box {{
                    background: #e0e7ff;
                    border-left: 4px solid #667eea;
                    padding: 15px;
                    margin: 20px 0;
                    border-radius: 5px;
                }}
                .footer {{
                    text-align: center;
                    color: #6b7280;
                    font-size: 12px;
                    margin-top: 30px;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>🎉 Great News!</h1>
                </div>
                <div class='content'>
                    <div class='success-badge'>✓ Account Linked Successfully</div>
                    
                    <p>Dear {parentName},</p>
                    
                    <p>
                        We're excited to let you know that <strong>{childName}</strong> has accepted 
                        your parent link invitation!
                    </p>
                    
                    <div class='info-box'>
                        <strong>What you can do now:</strong>
                        <ul>
                            <li>Monitor {childName}'s course progress and achievements</li>
                            <li>View their learning statistics and study time</li>
                            <li>Track their enrolled courses and completion status</li>
                            <li>Support their educational journey</li>
                        </ul>
                    </div>
                    
                    <p style='text-align: center;'>
                        <a href='{childProgressUrl}' class='button'>
                            View {childName}'s Progress
                        </a>
                    </p>
                    
                    <p>
                        You can access {childName}'s learning dashboard anytime from your parent profile 
                        or by clicking the link above.
                    </p>
                    
                    <p>
                        Thank you for being an active part of {childName}'s learning journey!
                    </p>
                    
                    <p>
                        Best regards,<br>
                        <strong>The EduPlatform Team</strong>
                    </p>
                </div>
                
                <div class='footer'>
                    <p>This is an automated notification from EduPlatform.</p>
                    <p>If you have any questions, please contact our support team.</p>
                </div>
            </div>
        </body>
        </html>
    ";

        await SendEmailAsync(parentEmail, subject, body);
    }
}