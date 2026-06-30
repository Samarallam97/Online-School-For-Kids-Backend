using Domain.Entities;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;

// No MimeKit / MailKit imports — everything goes through System.Net.Mail,
// which works directly with Gmail on port 587 + EnableSsl = true.

namespace Infrastructure.Services.Shared;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly EmailSettings _emailSettings;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    // ── Core delivery ─────────────────────────────────────────────────────────

    public async Task SendEmailAsync(
        string to, string subject, string body,
        bool isHtml = true, CancellationToken cancellationToken = default)
    {
        await DeliverAsync(to, subject, body, isHtml, attachment: null, cancellationToken);
    }

    // ── Account emails ────────────────────────────────────────────────────────

    public async Task SendVerificationEmailAsync(
        string to, string userName, DateTime expiry, string verificationLink,
        CancellationToken cancellationToken = default)
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

    public async Task SendPasswordResetEmailAsync(
        string to, string userName, DateTime expiry, string resetLink,
        CancellationToken cancellationToken = default)
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
            <p>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(
        string to, string userName,
        CancellationToken cancellationToken = default)
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
        // 1. Send the email (keep as-is per the task requirement)
        await SendEmailAsync(to, subject, body, true, cancellationToken);
        // 2. In-app notification → saved to MongoDB → pushed via SignalR
        var user = await _userRepository.GetByEmailAsync(to, cancellationToken);
        if (user is not null)
        {
            await _notificationService.SendAsync(
                userId: user.Id,
                title: "Welcome to the Platform! 🎉",
                message: $"Hi {userName}, your account is ready. Start exploring our courses!",
                type: NotificationType.Welcome,
                actionUrl: "/dashboard",
                ct: cancellationToken);
        }
    }

    public async Task SendParentLinkInvitationAsync(
        string childEmail, string childName, string parentName, string inviteLink,
        CancellationToken cancellationToken = default)
    {
        var child = await _userRepository.GetByEmailAsync(childEmail, cancellationToken);
        if (child is not null)
            await _notificationService.SendAsync(
                child.Id,
                "Parent Link Request",
                $"{parentName} wants to link their account with yours. Tap to review.",
                NotificationType.ParentLinkInvitation,
                inviteLink, cancellationToken);
    }

    public async Task SendParentLinkedNotificationAsync(
         string childEmail, string childName, string parentName,
         CancellationToken cancellationToken = default)
    {
        var child = await _userRepository.GetByEmailAsync(childEmail, cancellationToken);
        if (child is not null)
            await _notificationService.SendAsync(
                child.Id,
                "Account linked",
                $"You have successfully linked your account with {parentName}.",
                NotificationType.ParentLinked,
                "/profile", cancellationToken);
    }

    public async Task SendParentLinkAcceptedNotificationAsync(
        string parentEmail, string parentName,
        string childName, string childProgressUrl)
    {
        var subject = $"{childName} accepted your parent link invitation!";
        var body = $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
                .success-badge {{ background: #10b981; color: white; padding: 10px 20px; border-radius: 20px; display: inline-block; margin: 20px 0; font-weight: bold; }}
                .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                .info-box {{ background: #e0e7ff; border-left: 4px solid #667eea; padding: 15px; margin: 20px 0; border-radius: 5px; }}
                .footer {{ text-align: center; color: #6b7280; font-size: 12px; margin-top: 30px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'><h1>🎉 Great News!</h1></div>
                <div class='content'>
                    <div class='success-badge'>✓ Account Linked Successfully</div>
                    <p>Dear {parentName},</p>
                    <p>We're excited to let you know that <strong>{childName}</strong> has accepted your parent link invitation!</p>
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
                        <a href='{childProgressUrl}' class='button'>View {childName}'s Progress</a>
                    </p>
                    <p>Thank you for being an active part of {childName}'s learning journey!</p>
                    <p>Best regards,<br/><strong>The EduPlatform Team</strong></p>
                </div>
                <div class='footer'>
                    <p>This is an automated notification from EduPlatform.</p>
                    <p>If you have any questions, please contact our support team.</p>
                </div>
            </div>
        </body>
        </html>";
        // 1. Email (permanent record for the parent)
        await SendEmailAsync(parentEmail, subject, body);
        // 2. Notification → saved to MongoDB → pushed via SignalR
        var parent = await _userRepository.GetByEmailAsync(parentEmail);
        if (parent is not null)
            await _notificationService.SendAsync(
                parent.Id,
                $"{childName} accepted your invitation! 🎉",
                $"You can now monitor {childName}'s progress, achievements, and study time.",
                NotificationType.ParentLinkAccepted,
                childProgressUrl);
    }


    // ── Booking emails ────────────────────────────────────────────────────────

    public async Task SendBookingConfirmedAsync(
        Appointment appt,
        string specialistEmail, string specialistName,
        string studentEmail, string studentName,
        string meetLink,
        CancellationToken ct = default)
    {
        var dateStr = DateTime.Parse(appt.AppointmentDate)
            .ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
        var timeStr = $"{appt.StartTime} – {appt.EndTime} UTC";

        //var icsBytes = BuildIcs(appt, specialistName, meetLink);

        var studentBody = $@"
            <h2>Your session is confirmed! 🎉</h2>
            <p><strong>Session:</strong> {appt.Title}</p>
            <p><strong>Specialist:</strong> {specialistName}</p>
            <p><strong>Date:</strong> {dateStr}</p>
            <p><strong>Time:</strong> {timeStr}</p>
            <p><strong>Join here:</strong> <a href='{meetLink}'>{meetLink}</a></p>
            <p>The calendar invite (.ics) is attached — click it to add the session to Google Calendar, Outlook, or Apple Calendar.</p>";

        var specialistBody = $@"
            <h2>New session confirmed 📅</h2>
            <p><strong>Student:</strong> {studentName}</p>
            <p><strong>Session:</strong> {appt.Title}</p>
            <p><strong>Date:</strong> {dateStr}</p>
            <p><strong>Time:</strong> {timeStr}</p>
            <p><strong>Join here:</strong> <a href='{meetLink}'>{meetLink}</a></p>
            <p>The calendar invite (.ics) is attached.</p>";

        var icsBytes = BuildIcs(appt, specialistName, meetLink);

        await DeliverAsync(
            studentEmail,
            $"Session confirmed: {appt.Title}",
            studentBody,
            true,
            BuildIcsAttachment(icsBytes),
            ct);

        await DeliverAsync(
            specialistEmail,
            $"New session: {appt.Title}",
            specialistBody,
            true,
            BuildIcsAttachment(icsBytes),
            ct);
        // 2. Notifications → saved to MongoDB → pushed via SignalR to each user
        var student = await _userRepository.GetByEmailAsync(studentEmail, ct);
        var specialist = await _userRepository.GetByEmailAsync(specialistEmail, ct);

        if (student is not null)
            await _notificationService.SendAsync(
                student.Id,
                "Session confirmed ✅",
                $"Your session \"{appt.Title}\" with {specialistName} is set for {dateStr} at {appt.StartTime} UTC.",
                NotificationType.BookingConfirmed,
                "/sessions", ct);

        if (specialist is not null)
            await _notificationService.SendAsync(
                specialist.Id,
                "New booking",
                $"{studentName} booked \"{appt.Title}\" with you on {dateStr} at {appt.StartTime} UTC.",
                NotificationType.BookingConfirmed,
                "/sessions", ct);
    }

    public async Task SendBookingCancelledAsync(
        Appointment appt,
        string specialistEmail,
        string studentEmail,
        string? reason,
        bool refundIssued,
        CancellationToken ct = default)
    {
        var dateStr = DateTime.Parse(appt.AppointmentDate)
            .ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);

        var refundLine = refundIssued
            ? "<p>✅ A full refund has been issued to your original payment method.</p>"
            : "";

        var reasonLine = !string.IsNullOrWhiteSpace(reason)
            ? $"<p><strong>Reason:</strong> {reason}</p>"
            : "";

        var body = $@"
            <h2>Session cancelled</h2>
            <p><strong>Session:</strong> {appt.Title}</p>
            <p><strong>Date:</strong> {dateStr} at {appt.StartTime} UTC</p>
            {reasonLine}
            {refundLine}";

        await SendEmailAsync(studentEmail, $"Session cancelled: {appt.Title}", body, true, ct);
        await SendEmailAsync(specialistEmail, $"Session cancelled: {appt.Title}", body, true, ct);
        // 2. Notifications → saved to MongoDB → pushed via SignalR
        var baseMsg = string.IsNullOrWhiteSpace(reason)
            ? $"The session \"{appt.Title}\" on {dateStr} has been cancelled."
            : $"The session \"{appt.Title}\" on {dateStr} has been cancelled. Reason: {reason}";

        var student = await _userRepository.GetByEmailAsync(studentEmail, ct);
        var specialist = await _userRepository.GetByEmailAsync(specialistEmail, ct);

        if (student is not null)
            await _notificationService.SendAsync(
                student.Id, "Session cancelled",
                refundIssued ? baseMsg + " A refund has been issued." : baseMsg,
                NotificationType.BookingCancelled, "/sessions", ct);

        if (specialist is not null)
            await _notificationService.SendAsync(
                specialist.Id, "Session cancelled",
                baseMsg, NotificationType.BookingCancelled, "/sessions", ct);

    }
    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Single SMTP delivery point. Accepts an optional System.Net.Mail.Attachment
    /// (used for .ics files). All other emails pass null.
    /// </summary>
    private async Task DeliverAsync(
        string to, string subject, string body, bool isHtml,
        Attachment? attachment, CancellationToken ct)
    {
        try
        {
            using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
            };

            message.To.Add(to);

            if (attachment is not null)
                message.Attachments.Add(attachment);

            await smtpClient.SendMailAsync(message, ct);

            _logger.LogInformation(
                "Email sent to {To} — subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email to {To} — subject: {Subject}", to, subject);
            throw;
        }
    }

    /// <summary>
    /// Wraps raw .ics bytes in a System.Net.Mail.Attachment
    /// with the correct text/calendar MIME type.
    /// </summary>
    private static Attachment BuildIcsAttachment(byte[] icsBytes)
    {
        var stream = new MemoryStream(icsBytes);
        var contentType = new System.Net.Mime.ContentType("text/calendar")
        {
            Name = "session.ics",
            Parameters = { { "method", "REQUEST" } }
        };

        return new Attachment(stream, contentType);
    }

    /// <summary>Builds a minimal RFC 5545 .ics calendar invite for a session.</summary>
    private static byte[] BuildIcs(Appointment appt, string specialistName, string meetLink)
    {
        var dtStart = DateTime.Parse($"{appt.AppointmentDate}T{appt.StartTime}:00")
            .ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        var dtEnd = DateTime.Parse($"{appt.AppointmentDate}T{appt.EndTime}:00")
            .ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        var now = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

        var ics = $@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//LearningPlatform//EN
METHOD:REQUEST
BEGIN:VEVENT
UID:{appt.Id}@learningplatform
DTSTAMP:{now}
DTSTART:{dtStart}
DTEND:{dtEnd}
SUMMARY:{appt.Title} with {specialistName}
DESCRIPTION:Join the Google Meet here: {meetLink}
URL:{meetLink}
END:VEVENT
END:VCALENDAR";

        return Encoding.UTF8.GetBytes(ics);
    }
}