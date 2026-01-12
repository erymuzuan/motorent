using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace MotoRent.Server.Controllers;

/// <summary>
/// Controller for managing the user's culture/language settings.
/// </summary>
[Route("Culture")]
public class CultureController : Controller
{
    /// <summary>
    /// Sets the user's culture by storing it in a cookie.
    /// </summary>
    /// <param name="culture">The culture name (e.g., 'en', 'th').</param>
    /// <param name="redirectUri">The URI to redirect back to after setting the culture.</param>
    [HttpGet("Set")]
    public IActionResult Set([FromQuery] string culture, [FromQuery] string redirectUri)
    {
        if (!string.IsNullOrWhiteSpace(culture))
        {
            HttpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                }
            );
        }

        // Validate redirect URI to prevent open redirect attacks
        if (string.IsNullOrEmpty(redirectUri) || !Url.IsLocalUrl(redirectUri))
        {
            redirectUri = "/";
        }

        return LocalRedirect(redirectUri);
    }
}
