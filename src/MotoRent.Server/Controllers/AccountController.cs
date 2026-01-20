using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using CoreUser = MotoRent.Domain.Core.User;

namespace MotoRent.Server.Controllers;

/// <summary>
/// Controller for authentication, authorization, and user account management.
/// </summary>
[Route("account")]
[Authorize]
public class AccountController(
    IDirectoryService directoryService,
    IRequestContext requestContext,
    CoreDataContext coreDataContext,
    RentalDataContext rentalDataContext) : Controller
{
    private IDirectoryService DirectoryService { get; } = directoryService;
    private IRequestContext RequestContext { get; } = requestContext;
    private CoreDataContext CoreDataContext { get; } = coreDataContext;
    private RentalDataContext RentalDataContext { get; } = rentalDataContext;

    // Login Pages

    /// <summary>
    /// Login page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("login")]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl = null)
    {
        this.ViewData["ReturnUrl"] = returnUrl;

        // Pass OAuth configuration status to the view
        var authSchemes = this.HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = (await authSchemes.GetAllSchemesAsync()).ToArray();
        this.ViewData["GoogleEnabled"] = schemes.Any(s => s.Name == "Google");
        this.ViewData["MicrosoftEnabled"] = schemes.Any(s => s.Name == "Microsoft");
        this.ViewData["LineEnabled"] = schemes.Any(s => s.Name == "Line");

        return this.View();
    }

    /// <summary>
    /// Access denied page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied() => this.View();

    /// <summary>
    /// Page shown when user is not registered in the system.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("not-registered")]
    public IActionResult NotRegistered() => this.View();

    // OAuth Login

    /// <summary>
    /// Initiates Google OAuth login.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("google")]
    public IActionResult LoginWithGoogle([FromQuery] string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = this.Url.Action(nameof(this.OAuthCallback), new { returnUrl }),
            Items = { { "LoginProvider", "Google" } }
        };
        return this.Challenge(properties, "Google");
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
            RedirectUri = this.Url.Action(nameof(this.OAuthCallback), new { returnUrl }),
            Items = { { "LoginProvider", "Microsoft" } }
        };
        return this.Challenge(properties, "Microsoft");
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
            RedirectUri = this.Url.Action(nameof(this.OAuthCallback), new { returnUrl }),
            Items = { { "LoginProvider", "Line" } }
        };
        return this.Challenge(properties, "Line");
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
        var result = await this.HttpContext.AuthenticateAsync("ExternalAuth");
        if (!result.Succeeded || result.Principal == null)
        {
            return this.RedirectToAction(nameof(this.Login));
        }

        // Get the external claims
        var externalUser = result.Principal;
        var provider = result.Properties?.Items["LoginProvider"] ?? CoreUser.GOOGLE;
        var nameIdentifier = externalUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = externalUser.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = externalUser.FindFirst(ClaimTypes.Name)?.Value
            ?? externalUser.FindFirst("name")?.Value;

        // For LINE users, email may be null - use nameIdentifier as username
        CoreUser? user = null;

        // Determine username based on provider
        // LINE users: username = LINE userId (nameIdentifier)
        // Google/Microsoft: username = email
        var userName = provider == CoreUser.LINE
            ? nameIdentifier
            : email?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(userName))
        {
            return this.RedirectToAction(nameof(this.Login));
        }

        // Primary lookup: by username
        user = await this.DirectoryService.GetUserAsync(userName);

        // Fallback: by NameIdentifier (for existing LINE users with line_ prefix)
        if (user == null && !string.IsNullOrWhiteSpace(userName))
        {
            user = await this.DirectoryService.GetUserByProviderIdAsync(provider, userName);
        }

        // User must be pre-registered, EXCEPT SuperAdmins (identified by env var only)
        if (user == null)
        {
            // Check if this user is a configured SuperAdmin
            if (MotoConfig.SuperAdmins.Contains(userName, StringComparer.OrdinalIgnoreCase))
            {
                // SuperAdmin can self-register (they exist only in env var, not database)
                user = new CoreUser
                {
                    UserName = userName,
                    Email = email ?? "",
                    FullName = displayName ?? userName,
                    CredentialProvider = provider,
                    NameIdentifier = nameIdentifier
                };
                await this.DirectoryService.SaveUserProfileAsync(user);
            }
            else
            {
                // Not a SuperAdmin and not pre-registered - redirect to not-registered page
                await this.HttpContext.SignOutAsync("ExternalAuth");
                return this.RedirectToAction(nameof(this.NotRegistered));
            }
        }
        else
        {
            // Update NameIdentifier if not set (for existing users linking a new provider)
            if (string.IsNullOrWhiteSpace(user.NameIdentifier) && !string.IsNullOrWhiteSpace(nameIdentifier))
            {
                user.NameIdentifier = nameIdentifier;
                user.CredentialProvider = provider;
                await this.DirectoryService.SaveUserProfileAsync(user);
            }
        }

        // Check if user is locked
        if (user.IsLockedOut)
        {
            return this.RedirectToAction(nameof(this.AccessDenied));
        }

        // Get the user's default account (or null for super admins)
        var accountNo = user.AccountNo;

        // Build claims for the user
        var claims = await this.DirectoryService.GetClaimsAsync(user.UserName, accountNo);
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
        await this.HttpContext.SignOutAsync("ExternalAuth");
        await this.HttpContext.SignInAsync(
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
                return this.LocalRedirect("/super-admin/start-page");
            }
        }

        return this.LocalRedirect(returnUrl ?? "/");
    }

    // Account Switching

    /// <summary>
    /// Signs in to a specific account (for users with multiple accounts).
    /// </summary>
    [Authorize]
    [HttpGet("sign-in/{accountNo}")]
    public async Task<IActionResult> SignInToAccount(string accountNo, [FromQuery] string? returnUrl = null)
    {
        var userName = await this.RequestContext.GetUserNameAsync();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return this.RedirectToAction(nameof(this.Login));
        }

        // Verify user has access to this account
        var user = await this.DirectoryService.GetUserAsync(userName);
        if (user is null || user.AccountCollection.All(a => a.AccountNo != accountNo))
        {
            return this.RedirectToAction(nameof(this.AccessDenied));
        }

        // Build claims for the new account
        var claims = await this.DirectoryService.GetClaimsAsync(userName, accountNo);
        var claimsList = claims.ToList();

        if (claimsList.Count == 0)
        {
            return this.RedirectToAction(nameof(this.AccessDenied));
        }

        // Sign in with the new account
        var identity = new ClaimsIdentity(claimsList, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await this.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return this.LocalRedirect(returnUrl ?? "/");
    }

    /// <summary>
    /// Switches to a specific shop context.
    /// ShopId = 0 means "all shops" (no filter).
    /// </summary>
    [Authorize]
    [HttpGet("switch-shop/{shopId:int}")]
    public async Task<IActionResult> SwitchShop(int shopId, [FromQuery] string? returnUrl = null)
    {
        var userName = await this.RequestContext.GetUserNameAsync();
        var accountNo = await this.RequestContext.GetAccountNoAsync();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(accountNo))
        {
            return this.RedirectToAction(nameof(this.Login));
        }

        // Validate shop belongs to current org (if shopId > 0)
        if (shopId > 0)
        {
            var shop = await this.RentalDataContext.LoadOneAsync<Shop>(s => s.ShopId == shopId);
            if (shop is not {IsActive:true})
            {
                return this.RedirectToAction(nameof(this.AccessDenied));
            }
        }

        // Rebuild claims with new ShopId
        var claims = (await this.DirectoryService.GetClaimsAsync(userName, accountNo)).ToList();

        // Replace ShopId claim
        claims.RemoveAll(c => c.Type == "ShopId");
        claims.Add(new Claim("ShopId", shopId.ToString()));

        // Re-sign cookie with updated claims
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await this.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return this.LocalRedirect(returnUrl ?? "/");
    }

    // Logout

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("logoff")]
    public async Task<IActionResult> Logoff()
    {
        await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await this.HttpContext.SignOutAsync("ExternalAuth");
        return this.RedirectToAction(nameof(this.Login));
    }

    // Impersonation

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
        var superAdmin = this.RequestContext.GetUserName();
        if (string.IsNullOrWhiteSpace(superAdmin))
        {
            return this.BadRequest("Not authenticated");
        }

        // Localhost development can skip hash validation for convenience
        var isLocal = string.Equals(this.HttpContext.Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || this.HttpContext.Connection.RemoteIpAddress is { } remoteIp && IPAddress.IsLoopback(remoteIp);

        // Validate hash: MD5(userName:accountNo)
        var expectedHash = ComputeMd5Hash($"{userName}:{accountNo}");
        if (!isLocal && !string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            return this.BadRequest("Invalid hash");
        }

        // Load the target user
        var user = await this.CoreDataContext.LoadOneAsync<User>(u => u.UserName == userName);
        if (user == null)
        {
            return this.NotFound($"User '{userName}' not found");
        }

        // Load the target organization
        var org = await this.CoreDataContext.LoadOneAsync<Organization>(o => o.AccountNo == accountNo);
        if (org == null)
        {
            return this.NotFound($"Organization '{accountNo}' not found");
        }

        // Build claims for the impersonated user
        var claims = (await this.DirectoryService.GetClaimsAsync(userName, accountNo)).ToList();

        // Add super admin claims for audit and to allow returning
        claims.Add(new Claim(ClaimTypes.Role, UserAccount.SUPER_ADMIN));
        claims.Add(new Claim("SuperAdmin", superAdmin)); // Original admin for audit trail
        claims.Add(new Claim("Provider", "SuperAdmin")); // Indicates impersonation is active

        // Create the impersonated identity
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign in as the impersonated user
        await this.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return this.LocalRedirect(url ?? "/");
    }

    /// <summary>
    /// Returns to the super admin account after impersonation.
    /// </summary>
    [Authorize(Policy = UserAccount.POLICY_SUPER_ADMIN_IMPERSONATE)]
    [HttpGet("back-super-admin")]
    public async Task<IActionResult> RemoveImpersonateAccount()
    {
        // Get the original super admin username from claims
        var superAdminUserName = await this.RequestContext.GetClaimAsync<string>("SuperAdmin");
        if (string.IsNullOrWhiteSpace(superAdminUserName))
        {
            return this.BadRequest("Not in impersonation mode");
        }

        // Load the super admin user
        var user = await this.CoreDataContext.LoadOneAsync<User>(u => u.UserName == superAdminUserName);
        if (user == null)
        {
            return this.BadRequest("Failed to restore super admin session");
        }

        // Build claims for the super admin (no account)
        var claims = (await this.DirectoryService.GetClaimsAsync(superAdminUserName, null)).ToList();

        // Create the super admin identity
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign in as the super admin
        await this.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return this.Redirect("/super-admin/start-page");
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

        return this.Ok(new { url = impersonateUrl });
    }

    // Helpers

    private static string ComputeMd5Hash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
