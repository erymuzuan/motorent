using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Tourist;

/// <summary>
/// DTO containing shop contact information for the tourist offline app.
/// Pre-generates contact URLs for easy one-tap communication.
/// </summary>
public class ShopContactInfo
{
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string Address { get; set; } = string.Empty;

    #region Direct Contact

    /// <summary>
    /// Phone number for tel: links.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Pre-formatted tel: URL (e.g., tel:+66-81-234-5678).
    /// </summary>
    public string PhoneUrl { get; set; } = string.Empty;

    /// <summary>
    /// Email address for mailto: links.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Pre-formatted mailto: URL with subject.
    /// </summary>
    public string? EmailUrl { get; set; }

    #endregion

    #region Messaging Apps

    /// <summary>
    /// Pre-formatted WhatsApp URL with message template.
    /// e.g., https://wa.me/66812345678?text=Hi%20I%20rented...
    /// </summary>
    public string? WhatsAppUrl { get; set; }

    /// <summary>
    /// Pre-formatted LINE URL.
    /// e.g., https://line.me/ti/p/@adammoto
    /// </summary>
    public string? LineUrl { get; set; }

    /// <summary>
    /// Pre-formatted Facebook Messenger URL.
    /// e.g., https://m.me/adammoto
    /// </summary>
    public string? MessengerUrl { get; set; }

    #endregion

    #region Location

    /// <summary>
    /// GPS latitude.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// GPS longitude.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Pre-formatted Google Maps directions URL.
    /// Uses current location as origin.
    /// </summary>
    public string GoogleMapsUrl { get; set; } = string.Empty;

    #endregion

    #region Operating Hours

    /// <summary>
    /// Operating hours by day of week.
    /// Key: day name (Monday, Tuesday, etc.)
    /// Value: hours string (e.g., "09:00 - 20:00" or "Closed")
    /// </summary>
    public Dictionary<string, string> Hours { get; set; } = [];

    /// <summary>
    /// Whether the shop is currently open (calculated at download time).
    /// </summary>
    public bool IsCurrentlyOpen { get; set; }

    /// <summary>
    /// Whether the shop operates 24 hours.
    /// </summary>
    public bool IsOpen24Hours { get; set; }

    /// <summary>
    /// Display text for next open/close time.
    /// e.g., "Opens at 9:00 AM" or "Closes at 8:00 PM"
    /// </summary>
    public string? NextStatusChange { get; set; }

    #endregion

    /// <summary>
    /// Creates a ShopContactInfo from a Shop entity with pre-generated URLs.
    /// </summary>
    /// <param name="shop">The shop entity</param>
    /// <param name="renterName">Renter's name for message templates</param>
    /// <param name="rentalReference">Rental reference number for message templates</param>
    /// <param name="vehicleInfo">Vehicle info for message templates (e.g., "Honda Click 125 (ABC-1234)")</param>
    /// <param name="logoBaseUrl">Base URL for logo images</param>
    public static ShopContactInfo FromShop(
        Shop shop,
        string? renterName = null,
        string? rentalReference = null,
        string? vehicleInfo = null,
        string? logoBaseUrl = null)
    {
        var info = new ShopContactInfo
        {
            ShopId = shop.ShopId,
            Name = shop.Name,
            Address = shop.Address,
            Phone = shop.Phone,
            Email = shop.Email,
            IsOpen24Hours = shop.IsOpen24Hours,
            Latitude = shop.GpsLocation?.Lat ?? 0,
            Longitude = shop.GpsLocation?.Lng ?? 0
        };

        // Logo URL
        if (!string.IsNullOrEmpty(shop.LogoPath) && !string.IsNullOrEmpty(logoBaseUrl))
        {
            info.LogoUrl = $"{logoBaseUrl.TrimEnd('/')}/{shop.LogoPath}";
        }

        // Phone URL
        if (!string.IsNullOrEmpty(shop.Phone))
        {
            var cleanPhone = new string(shop.Phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
            info.PhoneUrl = $"tel:{cleanPhone}";
        }

        // Email URL with subject
        if (!string.IsNullOrEmpty(shop.Email))
        {
            var subject = !string.IsNullOrEmpty(rentalReference)
                ? Uri.EscapeDataString($"Rental {rentalReference}")
                : Uri.EscapeDataString("Rental Inquiry");
            info.EmailUrl = $"mailto:{shop.Email}?subject={subject}";
        }

        // WhatsApp URL with pre-filled message
        if (!string.IsNullOrEmpty(shop.WhatsAppNumber))
        {
            var message = BuildMessageTemplate(renterName, rentalReference, vehicleInfo);
            var encodedMessage = Uri.EscapeDataString(message);
            info.WhatsAppUrl = $"https://wa.me/{shop.WhatsAppNumber}?text={encodedMessage}";
        }

        // LINE URL
        if (!string.IsNullOrEmpty(shop.LineUrl))
        {
            info.LineUrl = shop.LineUrl.StartsWith("http")
                ? shop.LineUrl
                : $"https://{shop.LineUrl}";
        }
        else if (!string.IsNullOrEmpty(shop.LineId))
        {
            var lineId = shop.LineId.TrimStart('@');
            info.LineUrl = $"https://line.me/ti/p/@{lineId}";
        }

        // Facebook Messenger URL
        if (!string.IsNullOrEmpty(shop.FacebookMessenger))
        {
            info.MessengerUrl = $"https://m.me/{shop.FacebookMessenger}";
        }

        // Google Maps directions URL
        if (shop.GpsLocation != null)
        {
            info.GoogleMapsUrl = $"https://www.google.com/maps/dir/?api=1&destination={shop.GpsLocation.Lat},{shop.GpsLocation.Lng}";
        }

        // Operating hours
        info.Hours = BuildHoursDictionary(shop);
        info.IsCurrentlyOpen = CalculateIsOpen(shop);
        info.NextStatusChange = CalculateNextStatusChange(shop);

        return info;
    }

    private static string BuildMessageTemplate(string? renterName, string? rentalReference, string? vehicleInfo)
    {
        var parts = new List<string> { "Hi" };

        if (!string.IsNullOrEmpty(renterName) || !string.IsNullOrEmpty(rentalReference))
        {
            parts.Add(",");
            if (!string.IsNullOrEmpty(renterName))
                parts.Add($" I'm {renterName}");
            if (!string.IsNullOrEmpty(rentalReference))
                parts.Add($" with rental #{rentalReference}");
            parts.Add(".");
        }

        if (!string.IsNullOrEmpty(vehicleInfo))
        {
            parts.Add($" I rented a {vehicleInfo}.");
        }

        return string.Concat(parts);
    }

    private static Dictionary<string, string> BuildHoursDictionary(Shop shop)
    {
        var hours = new Dictionary<string, string>();
        var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        if (shop.IsOpen24Hours)
        {
            foreach (var day in dayNames)
                hours[day] = "Open 24 Hours";
            return hours;
        }

        foreach (var day in dayNames)
        {
            var dayOfWeek = (DayOfWeek)Array.IndexOf(dayNames, day);
            var template = shop.DefaultHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

            if (template == null || !template.IsOpen)
            {
                hours[day] = "Closed";
            }
            else
            {
                var open = template.OpenTime.ToString(@"hh\:mm");
                var close = template.CloseTime.ToString(@"hh\:mm");
                hours[day] = $"{open} - {close}";
            }
        }

        return hours;
    }

    private static bool CalculateIsOpen(Shop shop)
    {
        if (shop.IsOpen24Hours) return true;

        var now = DateTime.Now; // Should use timezone-aware time in production
        var todayTemplate = shop.DefaultHours.FirstOrDefault(h => h.DayOfWeek == now.DayOfWeek);

        if (todayTemplate == null || !todayTemplate.IsOpen) return false;

        var currentTime = now.TimeOfDay;
        return currentTime >= todayTemplate.OpenTime && currentTime < todayTemplate.CloseTime;
    }

    private static string? CalculateNextStatusChange(Shop shop)
    {
        if (shop.IsOpen24Hours) return null;

        var now = DateTime.Now;
        var todayTemplate = shop.DefaultHours.FirstOrDefault(h => h.DayOfWeek == now.DayOfWeek);
        var currentTime = now.TimeOfDay;

        if (todayTemplate == null || !todayTemplate.IsOpen)
        {
            // Find next open day
            for (int i = 1; i <= 7; i++)
            {
                var nextDay = (DayOfWeek)(((int)now.DayOfWeek + i) % 7);
                var nextTemplate = shop.DefaultHours.FirstOrDefault(h => h.DayOfWeek == nextDay);
                if (nextTemplate?.IsOpen == true)
                {
                    var dayName = i == 1 ? "tomorrow" : nextDay.ToString();
                    return $"Opens {dayName} at {nextTemplate.OpenTime:hh\\:mm}";
                }
            }
            return "Temporarily closed";
        }

        if (currentTime < todayTemplate.OpenTime)
        {
            return $"Opens at {todayTemplate.OpenTime:hh\\:mm}";
        }

        if (currentTime < todayTemplate.CloseTime)
        {
            return $"Closes at {todayTemplate.CloseTime:hh\\:mm}";
        }

        // After closing, find next open time
        for (int i = 1; i <= 7; i++)
        {
            var nextDay = (DayOfWeek)(((int)now.DayOfWeek + i) % 7);
            var nextTemplate = shop.DefaultHours.FirstOrDefault(h => h.DayOfWeek == nextDay);
            if (nextTemplate?.IsOpen == true)
            {
                var dayName = i == 1 ? "tomorrow" : nextDay.ToString();
                return $"Opens {dayName} at {nextTemplate.OpenTime:hh\\:mm}";
            }
        }

        return null;
    }
}
