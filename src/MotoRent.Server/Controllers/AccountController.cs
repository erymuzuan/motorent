using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using CoreUser = MotoRent.Domain.Core.User;

namespace MotoRent.Server.Controllers;

/// <summary>
/// Controller for authentication, authorization, and user account management.
/// </summary>
[Route("account")]
[Authorize]
public class AccountController : Controller
{
    private readonly IDirectoryService m_directoryService;
    private readonly IRequestContext m_requestContext;
    private readonly CoreDataContext m_coreDataContext;

    public AccountController(
        IDirectoryService directoryService,
        IRequestContext requestContext,
        CoreDataContext coreDataContext)
    {
        m_directoryService = directoryService;
        m_requestContext = requestContext;
        m_coreDataContext = coreDataContext;
    }

    #region Login Pages

    /// <summary>
    /// Login page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        // Pass OAuth configuration status to the view
        var authSchemes = HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = authSchemes.GetAllSchemesAsync().Result;
        ViewData["GoogleEnabled"] = schemes.Any(s => s.Name == "Google");
        ViewData["MicrosoftEnabled"] = schemes.Any(s => s.Name == "Microsoft");
        ViewData["LineEnabled"] = schemes.Any(s => s.Name == "Line");

        return View();
    }

    /// <summary>
    /// Access denied page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    #endregion

    #region OAuth Login

    /// <summary>
    /// Initiates Google OAuth login.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("google")]
    public IActionResult LoginWithGoogle([FromQuery] string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCallback), new { returnUrl }),
            Items = { { "LoginProvider", "Google" } }
        };
        return Challenge(properties, "Google");
    }

    /// <summary>
    /// Initiates Microsoft OAuth login.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("microsoft")]
    public IActionResult LoginWithMicrosoft([FromQuery] string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCallback), new { returnUrl }),
            Items = { { "LoginProvider", "Microsoft" } }
        };
        return Challenge(properties, "Microsoft");
    }

    /// <summary>
    /// Initiates LINE OAuth login.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("line")]
    public IActionResult LoginWithLine([FromQuery] string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCallback), new { returnUrl }),
            Items = { { "LoginProvider", "Line" } }
        };
        return Challenge(properties, "Line");
    }

    /// <summary>
    /// OAuth callback handler - processes the response from OAuth providers.
    /// Supports LINE users who may not have email (uses LINE ID as username).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("oauth-callback")]
    public async Task<IActionResult> OAuthCallback([FromQuery] string? returnUrl = null)
    {
        // Authenticate using the external cookie
        var result = await HttpContext.AuthenticateAsync("ExternalAuth");
        if (!result.Succeeded || result.Principal == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Get the external claims
        var externalUser = result.Principal;
        var provider = result.Properties?.Items["LoginProvider"] ?? CoreUser.GOOGLE;
        var nameIdentifier = externalUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = externalUser.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = externalUser.FindFirst(ClaimTypes.Name)?.Value
            ?? externalUser.FindFirst("name")?.Value;

        // For LINE users, email may be null - use nameIdentifier for lookup
        CoreUser? user = null;

        // Strategy 1: Try lookup by NameIdentifier + Provider first (for LINE users without email)
        if (!string.IsNullOrWhiteSpace(nameIdentifier))
        {
            user = await m_directoryService.GetUserByProviderIdAsync(provider, nameIdentifier);
        }

        // Strategy 2: If not found and email exists, try email lookup
        if (user == null && !string.IsNullOrWhiteSpace(email))
        {
            user = await m_directoryService.GetUserAsync(email.ToLowerInvariant());
        }

        // Create new user if not found
        if (user == null)
        {
            // Determine username based on available information
            string userName;
            if (!string.IsNullOrWhiteSpace(email))
            {
                userName = email.ToLowerInvariant();
            }
            else if (provider == CoreUser.LINE && !string.IsNullOrWhiteSpace(nameIdentifier))
            {
                // LINE users without email use their LINE ID as username
                userName = $"line_{nameIdentifier}";
            }
            else
            {
                // No email and no LINE ID - cannot create user
                return RedirectToAction(nameof(Login));
            }

            user = new CoreUser
            {
                UserName = userName,
                Email = email ?? "",
                FullName = displayName ?? userName,
                CredentialProvider = provider,
                NameIdentifier = nameIdentifier
            };

            // Save the new user
            await m_directoryService.SaveUserProfileAsync(user);
        }
        else
        {
            // Update NameIdentifier if not set (for existing users linking a new provider)
            if (string.IsNullOrWhiteSpace(user.NameIdentifier) && !string.IsNullOrWhiteSpace(nameIdentifier))
            {
                user.NameIdentifier = nameIdentifier;
                user.CredentialProvider = provider;
                await m_directoryService.SaveUserProfileAsync(user);
            }
        }

        // Check if user is locked
        if (user.IsLockedOut)
        {
            return RedirectToAction(nameof(AccessDenied));
        }

        // Get the user's default account (or null for super admins)
        var accountNo = user.AccountNo;

        // Build claims for the user
        var claims = await m_directoryService.GetClaimsAsync(user.UserName, accountNo);
        var claimsList = claims.ToList();

        if (claimsList.Count == 0)
        {
            // User exists but has no account access
            // For now, allow them in with minimal claims
            claimsList.Add(new Claim(ClaimTypes.Name, user.UserName));
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claimsList.Add(new Claim(ClaimTypes.Email, user.Email));
            }
            claimsList.Add(new Claim(ClaimTypes.Role, UserAccount.REGISTERED_USER));
        }

        // Create the authentication principal
        var identity = new ClaimsIdentity(claimsList, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign out of external auth and sign in with the main cookie
        await HttpContext.SignOutAsync("ExternalAuth");
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                IssuedUtc = DateTimeOffset.UtcNow
            });

        // Redirect SuperAdmin users to their start page if no specific return URL
        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/")
        {
            var isSuperAdmin = claimsList.Any(c =>
                c.Type == ClaimTypes.Role && c.Value == UserAccount.SUPER_ADMIN);

            if (isSuperAdmin)
            {
                return LocalRedirect("/super-admin/start-page");
            }
        }

        return LocalRedirect(returnUrl ?? "/");
    }

    #endregion

    #region Account Switching

    /// <summary>
    /// Signs in to a specific account (for users with multiple accounts).
    /// </summary>
    [Authorize]
    [HttpGet("sign-in/{accountNo}")]
    public async Task<IActionResult> SignInToAccount(string accountNo, [FromQuery] string? returnUrl = null)
    {
        var userName = m_requestContext.GetUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToAction(nameof(Login));
        }

        // Verify user has access to this account
        var user = await m_directoryService.GetUserAsync(userName);
        if (user == null || !user.AccountCollection.Any(a => a.AccountNo == accountNo))
        {
            return RedirectToAction(nameof(AccessDenied));
        }

        // Build claims for the new account
        var claims = await m_directoryService.GetClaimsAsync(userName, accountNo);
        var claimsList = claims.ToList();

        if (claimsList.Count == 0)
        {
            return RedirectToAction(nameof(AccessDenied));
        }

        // Sign in with the new account
        var identity = new ClaimsIdentity(claimsList, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return LocalRedirect(returnUrl ?? "/");
    }

    #endregion

    #region Logout

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("logoff")]
    public async Task<IActionResult> Logoff()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync("ExternalAuth");
        return RedirectToAction(nameof(Login));
    }

    #endregion

    #region Impersonation

    /// <summary>
    /// Impersonates a user (super admin only).
    /// Hash must be MD5(userName:accountNo) for security.
    /// </summary>
    [Authorize(Roles = UserAccount.SUPER_ADMIN)]
    [HttpGet("impersonate")]
    public async Task<IActionResult> ImpersonateAccount(
        [FromQuery(Name = "user")] string userName,
        [FromQuery(Name = "account")] string accountNo,
        [FromQuery(Name = "hash")] string hash,
        [FromQuery(Name = "url")] string? url = null)
    {
        // Get the original super admin username for audit trail
        var superAdmin = m_requestContext.GetUserName();
        if (string.IsNullOrWhiteSpace(superAdmin))
        {
            return BadRequest("Not authenticated");
        }

        // Validate hash: MD5(userName:accountNo)
        var expectedHash = ComputeMd5Hash($"{userName}:{accountNo}");
        if (!string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid hash");
        }

        // Load the target user
        var user = await m_coreDataContext.LoadOneAsync<User>(u => u.UserName == userName);
        if (user == null)
        {
            return NotFound($"User '{userName}' not found");
        }

        // Load the target organization
        var org = await m_coreDataContext.LoadOneAsync<Organization>(o => o.AccountNo == accountNo);
        if (org == null)
        {
            return NotFound($"Organization '{accountNo}' not found");
        }

        // Build claims for the impersonated user
        var claims = (await m_directoryService.GetClaimsAsync(userName, accountNo)).ToList();

        // Add super admin claims for audit and to allow returning
        claims.Add(new Claim(ClaimTypes.Role, UserAccount.SUPER_ADMIN));
        claims.Add(new Claim("SuperAdmin", superAdmin)); // Original admin for audit trail
        claims.Add(new Claim("Provider", "SuperAdmin")); // Indicates impersonation is active

        // Create the impersonated identity
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign in as the impersonated user
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return LocalRedirect(url ?? "/");
    }

    /// <summary>
    /// Returns to the super admin account after impersonation.
    /// </summary>
    [Authorize(Policy = UserAccount.POLICY_SUPER_ADMIN_IMPERSONATE)]
    [HttpGet("back-super-admin")]
    public async Task<IActionResult> RemoveImpersonateAccount()
    {
        // Get the original super admin username from claims
        var superAdminUserName = await m_requestContext.GetClaimAsync<string>("SuperAdmin");
        if (string.IsNullOrWhiteSpace(superAdminUserName))
        {
            return BadRequest("Not in impersonation mode");
        }

        // Load the super admin user
        var user = await m_coreDataContext.LoadOneAsync<User>(u => u.UserName == superAdminUserName);
        if (user == null)
        {
            return BadRequest("Failed to restore super admin session");
        }

        // Build claims for the super admin (no account)
        var claims = (await m_directoryService.GetClaimsAsync(superAdminUserName, null)).ToList();

        // Create the super admin identity
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign in as the super admin
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return Redirect("/super-admin/start-page");
    }

    /// <summary>
    /// Generates an impersonation URL for a user (super admin only).
    /// </summary>
    [Authorize(Roles = UserAccount.SUPER_ADMIN)]
    [HttpGet("impersonate-url")]
    public IActionResult GetImpersonateUrl(
        [FromQuery(Name = "user")] string userName,
        [FromQuery(Name = "account")] string accountNo,
        [FromQuery(Name = "url")] string? returnUrl = null)
    {
        var hash = ComputeMd5Hash($"{userName}:{accountNo}");
        var impersonateUrl = $"/account/impersonate?user={Uri.EscapeDataString(userName)}&account={Uri.EscapeDataString(accountNo)}&hash={hash}";

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            impersonateUrl += $"&url={Uri.EscapeDataString(returnUrl)}";
        }

        return Ok(new { url = impersonateUrl });
    }

    #endregion

    #region Helpers

    private static string ComputeMd5Hash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    #endregion
}
