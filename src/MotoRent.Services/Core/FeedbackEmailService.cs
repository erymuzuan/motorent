using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;

namespace MotoRent.Services.Core;

/// <summary>
/// Sends email notifications when feedback is submitted or exceptions occur.
/// Uses Azure Communication Services if configured, falls back to SMTP, silently skips if neither is configured.
/// </summary>
public class FeedbackEmailService
{
    private readonly ILogger<FeedbackEmailService> m_logger;

    public FeedbackEmailService(ILogger<FeedbackEmailService> logger)
    {
        m_logger = logger;
    }

    /// <summary>
    /// Sends a feedback notification email to configured recipients.
    /// Fire-and-forget: failures are logged but never thrown.
    /// </summary>
    public async Task SendFeedbackNotificationAsync(Feedback feedback)
    {
        var recipients = MotoConfig.FeedbackNotificationEmails;
        if (string.IsNullOrWhiteSpace(recipients))
        {
            m_logger.LogDebug("FeedbackNotificationEmails not configured, skipping email");
            return;
        }

        var emails = recipients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (emails.Length == 0) return;

        var subject = feedback.Type == FeedbackType.ErrorReport
            ? $"[MotoRent] Error Report from {feedback.UserName ?? "Unknown"}"
            : $"[MotoRent] Feedback from {feedback.UserName ?? "Unknown"}";

        var body = BuildEmailBody(feedback);

        foreach (var email in emails)
        {
            await SendEmailAsync(email, subject, body);
        }
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            // Try Azure Communication Services first
            var azureConnectionString = MotoConfig.AzureCommunicationConnectionString;
            if (!string.IsNullOrWhiteSpace(azureConnectionString))
            {
                await SendViaAzureAsync(azureConnectionString, to, subject, body);
                return;
            }

            // Fall back to SMTP
            var smtpHost = MotoConfig.SmtpHost;
            if (!string.IsNullOrWhiteSpace(smtpHost))
            {
                await SendViaSmtpAsync(to, subject, body);
                return;
            }

            m_logger.LogDebug("No email provider configured (Azure or SMTP), skipping email to {To}", to);
        }
        catch (Exception ex)
        {
            m_logger.LogWarning(ex, "Failed to send feedback notification email to {To}", to);
        }
    }

    private async Task SendViaAzureAsync(string connectionString, string to, string subject, string body)
    {
        // Azure Communication Email SDK
        var emailClient = new Azure.Communication.Email.EmailClient(connectionString);
        var fromEmail = MotoConfig.AzureCommunicationFromEmail;

        var emailMessage = new Azure.Communication.Email.EmailMessage(
            senderAddress: fromEmail,
            recipientAddress: to,
            content: new Azure.Communication.Email.EmailContent(subject)
            {
                Html = body
            });

        await emailClient.SendAsync(Azure.WaitUntil.Started, emailMessage);
        m_logger.LogInformation("Feedback notification email sent via Azure to {To}", to);
    }

    private async Task SendViaSmtpAsync(string to, string subject, string body)
    {
        var smtpHost = MotoConfig.SmtpHost!;
        var smtpPort = MotoConfig.SmtpPort;
        var smtpUser = MotoConfig.SmtpUser;
        var smtpPassword = MotoConfig.SmtpPassword;
        var fromEmail = MotoConfig.SmtpFromEmail;
        var fromName = MotoConfig.SmtpFromName;

        using var client = new SmtpClient(smtpHost, smtpPort);
        client.EnableSsl = true;

        if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPassword))
        {
            client.Credentials = new NetworkCredential(smtpUser, smtpPassword);
        }

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(to);

        await client.SendMailAsync(message);
        m_logger.LogInformation("Feedback notification email sent via SMTP to {To}", to);
    }

    private static string BuildEmailBody(Feedback feedback)
    {
        var typeLabel = feedback.Type == FeedbackType.ErrorReport ? "Error Report" : "General Feedback";
        var baseUrl = MotoConfig.BaseUrl;
        var adminLink = $"{baseUrl}/super-admin/feedback";

        var errorSection = "";
        if (feedback.Type == FeedbackType.ErrorReport)
        {
            errorSection = $"""
                <tr>
                    <td style="padding: 8px 12px; font-weight: bold; color: #666;">Exception Type</td>
                    <td style="padding: 8px 12px;">{System.Web.HttpUtility.HtmlEncode(feedback.ExceptionType ?? "N/A")}</td>
                </tr>
                <tr>
                    <td style="padding: 8px 12px; font-weight: bold; color: #666;">Exception Message</td>
                    <td style="padding: 8px 12px;">{System.Web.HttpUtility.HtmlEncode(feedback.ExceptionMessage ?? "N/A")}</td>
                </tr>
                <tr>
                    <td style="padding: 8px 12px; font-weight: bold; color: #666;">Linked Log Entry</td>
                    <td style="padding: 8px 12px;">{(feedback.LogEntryId.HasValue ? $"#{feedback.LogEntryId}" : "None")}</td>
                </tr>
                """;
        }

        return $"""
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto;">
                <div style="background: #0054a6; color: white; padding: 20px; border-radius: 8px 8px 0 0;">
                    <h2 style="margin: 0;">New {typeLabel}</h2>
                    <p style="margin: 8px 0 0; opacity: 0.8;">MotoRent User Feedback</p>
                </div>
                <div style="background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <tr>
                            <td style="padding: 8px 12px; font-weight: bold; color: #666;">User</td>
                            <td style="padding: 8px 12px;">{System.Web.HttpUtility.HtmlEncode(feedback.UserName ?? "Anonymous")}</td>
                        </tr>
                        <tr>
                            <td style="padding: 8px 12px; font-weight: bold; color: #666;">Account</td>
                            <td style="padding: 8px 12px;">{System.Web.HttpUtility.HtmlEncode(feedback.AccountNo ?? "N/A")}</td>
                        </tr>
                        <tr>
                            <td style="padding: 8px 12px; font-weight: bold; color: #666;">Page URL</td>
                            <td style="padding: 8px 12px; word-break: break-all;">{System.Web.HttpUtility.HtmlEncode(feedback.Url ?? "N/A")}</td>
                        </tr>
                        <tr>
                            <td style="padding: 8px 12px; font-weight: bold; color: #666;">Time</td>
                            <td style="padding: 8px 12px;">{feedback.Timestamp:g}</td>
                        </tr>
                        {errorSection}
                    </table>
                    <div style="margin-top: 16px; padding: 16px; background: white; border-radius: 6px; border: 1px solid #dee2e6;">
                        <h4 style="margin: 0 0 8px; color: #333;">Description</h4>
                        <p style="margin: 0; white-space: pre-wrap;">{System.Web.HttpUtility.HtmlEncode(feedback.Description)}</p>
                    </div>
                    {(feedback.ScreenshotStoreId is not null ? "<p style='margin-top: 12px; color: #666;'><em>Screenshot attached to feedback.</em></p>" : "")}
                </div>
                <div style="padding: 16px; text-align: center; border: 1px solid #dee2e6; border-top: none; border-radius: 0 0 8px 8px;">
                    <a href="{adminLink}" style="display: inline-block; padding: 10px 24px; background: #0054a6; color: white; text-decoration: none; border-radius: 6px;">
                        View in Admin Panel
                    </a>
                </div>
            </div>
            """;
    }
}
