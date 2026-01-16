using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for sending notifications via Email and LINE.
/// </summary>
public class NotificationService
{
    private readonly HttpClient m_httpClient;
    private readonly ILogger m_logger;
    private const string LINE_NOTIFY_API = "https://notify-api.line.me/api/notify";

    public NotificationService(HttpClient httpClient, ILogger logger)
    {
        m_httpClient = httpClient;
        m_logger = logger;
    }

    #region Email Notifications

    /// <summary>
    /// Sends a booking confirmation email to the customer.
    /// </summary>
    public async Task<bool> SendBookingConfirmationAsync(Booking booking, string? shopPhone = null, string? shopLineId = null)
    {
        if (string.IsNullOrWhiteSpace(booking.CustomerEmail))
        {
            m_logger.WriteWarning($"Cannot send booking confirmation: No email address for booking {booking.BookingRef}");
            return false;
        }

        var subject = $"Booking Confirmed - {booking.BookingRef}";
        var body = BuildBookingConfirmationEmail(booking, shopPhone, shopLineId);

        return await SendEmailAsync(booking.CustomerEmail, subject, body);
    }

    /// <summary>
    /// Sends a payment receipt email to the customer.
    /// </summary>
    public async Task<bool> SendPaymentReceiptAsync(Booking booking, decimal amount, string paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(booking.CustomerEmail))
        {
            m_logger.WriteWarning($"Cannot send payment receipt: No email address for booking {booking.BookingRef}");
            return false;
        }

        var subject = $"Payment Receipt - {booking.BookingRef}";
        var body = BuildPaymentReceiptEmail(booking, amount, paymentMethod);

        return await SendEmailAsync(booking.CustomerEmail, subject, body);
    }

    /// <summary>
    /// Sends a booking reminder email to the customer.
    /// </summary>
    public async Task<bool> SendBookingReminderAsync(Booking booking, string? shopAddress = null, string? shopPhone = null)
    {
        if (string.IsNullOrWhiteSpace(booking.CustomerEmail))
        {
            m_logger.WriteWarning($"Cannot send reminder: No email address for booking {booking.BookingRef}");
            return false;
        }

        var subject = $"Reminder: Your Rental Tomorrow - {booking.BookingRef}";
        var body = BuildBookingReminderEmail(booking, shopAddress, shopPhone);

        return await SendEmailAsync(booking.CustomerEmail, subject, body);
    }

    /// <summary>
    /// Sends a cancellation notice email to the customer.
    /// </summary>
    public async Task<bool> SendCancellationNoticeAsync(Booking booking, decimal? refundAmount = null)
    {
        if (string.IsNullOrWhiteSpace(booking.CustomerEmail))
        {
            m_logger.WriteWarning($"Cannot send cancellation notice: No email address for booking {booking.BookingRef}");
            return false;
        }

        var subject = $"Booking Cancelled - {booking.BookingRef}";
        var body = BuildCancellationEmail(booking, refundAmount);

        return await SendEmailAsync(booking.CustomerEmail, subject, body);
    }

    private async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpHost = MotoConfig.SmtpHost;
            var smtpPort = MotoConfig.SmtpPort;
            var smtpUser = MotoConfig.SmtpUser;
            var smtpPassword = MotoConfig.SmtpPassword;
            var fromEmail = MotoConfig.SmtpFromEmail;
            var fromName = MotoConfig.SmtpFromName;

            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                m_logger.WriteWarning($"SMTP not configured. Email not sent to {to}: {subject}");
                return false;
            }

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
            m_logger.WriteInfo($"Email sent successfully to {to}: {subject}");
            return true;
        }
        catch (Exception ex)
        {
            m_logger.WriteError(ex, $"Failed to send email to {to}: {subject}");
            return false;
        }
    }

    #endregion

    #region LINE Notifications

    /// <summary>
    /// Sends a LINE notification for booking confirmation.
    /// </summary>
    public async Task<bool> SendLineBookingConfirmationAsync(Booking booking, string lineAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lineAccessToken))
        {
            m_logger.WriteWarning($"Cannot send LINE notification: No access token for booking {booking.BookingRef}");
            return false;
        }

        var message = BuildLineBookingConfirmation(booking);
        return await SendLineNotifyAsync(lineAccessToken, message);
    }

    /// <summary>
    /// Sends a LINE notification for payment receipt.
    /// </summary>
    public async Task<bool> SendLinePaymentReceiptAsync(Booking booking, decimal amount, string lineAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lineAccessToken))
        {
            return false;
        }

        var message = $"\n[Payment Received]\nBooking: {booking.BookingRef}\nAmount: {amount:N0} THB\nThank you!";
        return await SendLineNotifyAsync(lineAccessToken, message);
    }

    /// <summary>
    /// Sends a LINE notification for booking reminder.
    /// </summary>
    public async Task<bool> SendLineBookingReminderAsync(Booking booking, string lineAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lineAccessToken))
        {
            return false;
        }

        var message = $"\n[Reminder: Tomorrow's Pickup]\nBooking: {booking.BookingRef}\nDate: {booking.StartDate:MMM dd, yyyy}\nVehicles: {booking.Items.Count}\nSee you tomorrow!";
        return await SendLineNotifyAsync(lineAccessToken, message);
    }

    /// <summary>
    /// Sends a LINE notification for cancellation.
    /// </summary>
    public async Task<bool> SendLineCancellationNoticeAsync(Booking booking, decimal? refundAmount, string lineAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lineAccessToken))
        {
            return false;
        }

        var refundText = refundAmount.HasValue && refundAmount.Value > 0
            ? $"\nRefund: {refundAmount:N0} THB"
            : "";
        var message = $"\n[Booking Cancelled]\nBooking: {booking.BookingRef}{refundText}\nWe hope to serve you again!";
        return await SendLineNotifyAsync(lineAccessToken, message);
    }

    /// <summary>
    /// Sends a notification to shop staff via LINE Notify.
    /// </summary>
    public async Task<bool> SendLineStaffNotificationAsync(string message, string shopLineNotifyToken)
    {
        if (string.IsNullOrWhiteSpace(shopLineNotifyToken))
        {
            m_logger.WriteWarning("Cannot send LINE staff notification: No token configured");
            return false;
        }

        return await SendLineNotifyAsync(shopLineNotifyToken, message);
    }

    private async Task<bool> SendLineNotifyAsync(string accessToken, string message)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, LINE_NOTIFY_API);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("message", message)
            });
            request.Content = content;

            var response = await m_httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                m_logger.WriteInfo("LINE notification sent successfully");
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                m_logger.WriteWarning($"LINE notification failed: {response.StatusCode} - {responseBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            m_logger.WriteError(ex, "Failed to send LINE notification");
            return false;
        }
    }

    #endregion

    #region Email Templates

    private string BuildBookingConfirmationEmail(Booking booking, string? shopPhone, string? shopLineId)
    {
        var vehiclesList = string.Join("<br>", booking.Items.Select(i =>
            $"&bull; {i.VehicleDisplayName ?? "Vehicle"} - {i.DailyRate:N0} THB/day"));

        var pickupTime = booking.PickupTime.HasValue
            ? booking.PickupTime.Value.ToString(@"hh\:mm")
            : "To be confirmed";

        var contactInfo = "";
        if (!string.IsNullOrWhiteSpace(shopPhone))
            contactInfo += $"Phone: {shopPhone}<br>";
        if (!string.IsNullOrWhiteSpace(shopLineId))
            contactInfo += $"LINE: {shopLineId}<br>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #00897B; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .booking-ref {{ font-size: 24px; font-weight: bold; color: #00897B; }}
        .details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .btn {{ display: inline-block; padding: 10px 20px; background: #00897B; color: white; text-decoration: none; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Booking Confirmed!</h1>
        </div>
        <div class='content'>
            <p>Dear {booking.CustomerName},</p>
            <p>Thank you for your booking! Your reservation has been confirmed.</p>

            <div class='details'>
                <p><strong>Booking Reference:</strong></p>
                <p class='booking-ref'>{booking.BookingRef}</p>
                <p><em>Please save this code - you'll need it at pickup.</em></p>
            </div>

            <div class='details'>
                <p><strong>Rental Details:</strong></p>
                <p>Pickup Date: {booking.StartDate:dddd, MMMM dd, yyyy}</p>
                <p>Pickup Time: {pickupTime}</p>
                <p>Return Date: {booking.EndDate:dddd, MMMM dd, yyyy}</p>
            </div>

            <div class='details'>
                <p><strong>Vehicles:</strong></p>
                <p>{vehiclesList}</p>
            </div>

            <div class='details'>
                <p><strong>Payment Summary:</strong></p>
                <p>Total Amount: {booking.TotalAmount:N0} THB</p>
                <p>Deposit Required: {booking.DepositRequired:N0} THB</p>
                <p>Amount Paid: {booking.AmountPaid:N0} THB</p>
                <p>Balance Due: {(booking.TotalAmount - booking.AmountPaid):N0} THB</p>
            </div>

            <div class='details'>
                <p><strong>What to Bring:</strong></p>
                <p>&bull; Valid passport or ID</p>
                <p>&bull; This booking confirmation</p>
                <p>&bull; Payment for remaining balance</p>
            </div>

            {(string.IsNullOrEmpty(contactInfo) ? "" : $@"
            <div class='details'>
                <p><strong>Contact Us:</strong></p>
                <p>{contactInfo}</p>
            </div>
            ")}

            <p>We look forward to seeing you!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string BuildPaymentReceiptEmail(Booking booking, decimal amount, string paymentMethod)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #00897B; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .amount {{ font-size: 28px; font-weight: bold; color: #00897B; }}
        .details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Payment Receipt</h1>
        </div>
        <div class='content'>
            <p>Dear {booking.CustomerName},</p>
            <p>Thank you for your payment!</p>

            <div class='details'>
                <p><strong>Payment Details:</strong></p>
                <p class='amount'>{amount:N0} THB</p>
                <p>Payment Method: {paymentMethod}</p>
                <p>Date: {DateTimeOffset.Now:MMMM dd, yyyy HH:mm}</p>
            </div>

            <div class='details'>
                <p><strong>Booking Reference:</strong> {booking.BookingRef}</p>
                <p><strong>Total Amount:</strong> {booking.TotalAmount:N0} THB</p>
                <p><strong>Amount Paid:</strong> {booking.AmountPaid:N0} THB</p>
                <p><strong>Balance Due:</strong> {(booking.TotalAmount - booking.AmountPaid):N0} THB</p>
            </div>

            <p>Thank you for choosing us!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string BuildBookingReminderEmail(Booking booking, string? shopAddress, string? shopPhone)
    {
        var pickupTime = booking.PickupTime.HasValue
            ? booking.PickupTime.Value.ToString(@"hh\:mm")
            : "As confirmed";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .highlight {{ font-size: 24px; font-weight: bold; color: #FF9800; }}
        .details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Reminder: Your Rental is Tomorrow!</h1>
        </div>
        <div class='content'>
            <p>Dear {booking.CustomerName},</p>
            <p>This is a friendly reminder that your rental starts tomorrow!</p>

            <div class='details'>
                <p><strong>Booking Reference:</strong></p>
                <p class='highlight'>{booking.BookingRef}</p>
            </div>

            <div class='details'>
                <p><strong>Pickup Details:</strong></p>
                <p>Date: {booking.StartDate:dddd, MMMM dd, yyyy}</p>
                <p>Time: {pickupTime}</p>
                {(string.IsNullOrWhiteSpace(shopAddress) ? "" : $"<p>Location: {shopAddress}</p>")}
            </div>

            <div class='details'>
                <p><strong>Don't Forget to Bring:</strong></p>
                <p>&bull; Valid passport or ID</p>
                <p>&bull; Booking reference: {booking.BookingRef}</p>
                {(booking.TotalAmount - booking.AmountPaid > 0 ? $"<p>&bull; Remaining payment: {(booking.TotalAmount - booking.AmountPaid):N0} THB</p>" : "")}
            </div>

            {(string.IsNullOrWhiteSpace(shopPhone) ? "" : $@"
            <div class='details'>
                <p><strong>Questions?</strong></p>
                <p>Call us at: {shopPhone}</p>
            </div>
            ")}

            <p>See you tomorrow!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string BuildCancellationEmail(Booking booking, decimal? refundAmount)
    {
        var refundSection = refundAmount.HasValue && refundAmount.Value > 0
            ? $@"
            <div class='details'>
                <p><strong>Refund Information:</strong></p>
                <p>Refund Amount: {refundAmount:N0} THB</p>
                <p>Refunds are typically processed within 3-5 business days.</p>
            </div>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #757575; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Booking Cancelled</h1>
        </div>
        <div class='content'>
            <p>Dear {booking.CustomerName},</p>
            <p>Your booking has been cancelled as requested.</p>

            <div class='details'>
                <p><strong>Cancelled Booking:</strong></p>
                <p>Reference: {booking.BookingRef}</p>
                <p>Original Dates: {booking.StartDate:MMM dd} - {booking.EndDate:MMM dd, yyyy}</p>
            </div>

            {refundSection}

            <p>We're sorry to see you go. We hope to serve you in the future!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string BuildLineBookingConfirmation(Booking booking)
    {
        var vehicles = string.Join(", ", booking.Items.Select(i => i.VehicleDisplayName ?? "Vehicle"));
        return $@"
[Booking Confirmed]
Ref: {booking.BookingRef}
Customer: {booking.CustomerName}
Pickup: {booking.StartDate:MMM dd, yyyy}
Return: {booking.EndDate:MMM dd, yyyy}
Vehicles: {vehicles}
Total: {booking.TotalAmount:N0} THB
Paid: {booking.AmountPaid:N0} THB";
    }

    #endregion
}
