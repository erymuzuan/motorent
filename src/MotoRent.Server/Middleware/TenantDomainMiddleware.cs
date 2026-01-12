using MotoRent.Domain.Tourist;
using MotoRent.Services.Tourist;

namespace MotoRent.Server.Middleware;

/// <summary>
/// Middleware that handles custom domain and subdomain mapping for tourist pages.
///
/// Supports two patterns:
/// 1. Subdomain: {tenant}.motorent.co.th → resolves tenant from subdomain
/// 2. Custom domain: adam.co.th → resolves tenant from CustomDomain field
///
/// Sets HttpContext.Items["TenantAccountNo"] and ["TenantContext"] for downstream use.
/// Rewrites path internally (user sees original URL).
/// </summary>
public class TenantDomainMiddleware
{
    private readonly RequestDelegate m_next;
    private readonly IServiceScopeFactory m_scopeFactory;
    private readonly ILogger<TenantDomainMiddleware> m_logger;

    // System domains that should NOT trigger tenant resolution
    private static readonly HashSet<string> s_systemDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "motorent.co.th",
        "www.motorent.co.th",
        "motorent.th",
        "www.motorent.th",
        "motorent.com",
        "www.motorent.com"
    };

    // Base domain for subdomain pattern
    private const string c_baseDomain = ".motorent.co.th";
    private const string c_baseThDomain = ".motorent.th";

    public TenantDomainMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<TenantDomainMiddleware> logger)
    {
        m_next = next;
        m_scopeFactory = scopeFactory;
        m_logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var path = context.Request.Path.Value ?? "";

        // Skip if path already contains /tourist/{accountNo}
        if (path.StartsWith("/tourist/", StringComparison.OrdinalIgnoreCase) &&
            path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
        {
            await m_next(context);
            return;
        }

        // Skip for non-tourist paths like /account, /api, etc.
        if (IsNonTouristPath(path))
        {
            await m_next(context);
            return;
        }

        string? accountNo = null;
        TenantContext? tenantContext = null;

        try
        {
            using var scope = m_scopeFactory.CreateScope();
            var resolver = scope.ServiceProvider.GetRequiredService<ITenantResolverService>();

            // Pattern 1: Subdomain - {tenant}.motorent.co.th or {tenant}.motorent.th
            if (TryExtractSubdomain(host, out var subdomain))
            {
                tenantContext = await resolver.ResolveByAccountNoAsync(subdomain);
                if (tenantContext != null)
                {
                    accountNo = tenantContext.AccountNo;
                    m_logger.LogDebug("Resolved tenant {AccountNo} from subdomain {Subdomain}", accountNo, subdomain);
                }
            }
            // Pattern 2: Custom domain - adam.co.th
            else if (!IsSystemDomain(host))
            {
                tenantContext = await resolver.ResolveByCustomDomainAsync(host);
                if (tenantContext != null)
                {
                    accountNo = tenantContext.AccountNo;
                    m_logger.LogDebug("Resolved tenant {AccountNo} from custom domain {Domain}", accountNo, host);
                }
            }
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Error resolving tenant from host {Host}", host);
        }

        if (accountNo != null && tenantContext != null)
        {
            // Store tenant context for this request
            context.Items["TenantAccountNo"] = accountNo;
            context.Items["TenantContext"] = tenantContext;

            // Rewrite path to include tenant - this is internal only
            // User still sees adam.co.th/browse but internally it's /tourist/AdamMotoGolok/browse
            var originalPath = path;
            var newPath = path == "/" || string.IsNullOrEmpty(path)
                ? $"/tourist/{accountNo}"
                : $"/tourist/{accountNo}{path}";

            context.Request.Path = newPath;
            m_logger.LogDebug("Rewrote path from {Original} to {New}", originalPath, newPath);
        }

        await m_next(context);
    }

    /// <summary>
    /// Attempts to extract subdomain from host (e.g., "adam" from "adam.motorent.co.th").
    /// </summary>
    private static bool TryExtractSubdomain(string host, out string subdomain)
    {
        subdomain = "";

        // Check for .motorent.co.th pattern
        if (host.EndsWith(c_baseDomain, StringComparison.OrdinalIgnoreCase))
        {
            subdomain = host[..^c_baseDomain.Length];
        }
        // Check for .motorent.th pattern
        else if (host.EndsWith(c_baseThDomain, StringComparison.OrdinalIgnoreCase))
        {
            subdomain = host[..^c_baseThDomain.Length];
        }

        // Skip "www" subdomain
        if (string.IsNullOrEmpty(subdomain) || subdomain.Equals("www", StringComparison.OrdinalIgnoreCase))
        {
            subdomain = "";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the host is a system domain that should not trigger tenant resolution.
    /// </summary>
    private static bool IsSystemDomain(string host)
    {
        // Check exact matches
        if (s_systemDomains.Contains(host))
            return true;

        // Check for localhost with port
        if (host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for IP addresses
        if (System.Net.IPAddress.TryParse(host, out _))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the path is for non-tourist endpoints that shouldn't be rewritten.
    /// </summary>
    private static bool IsNonTouristPath(string path)
    {
        var nonTouristPrefixes = new[]
        {
            "/account",
            "/api",
            "/signin-",
            "/_blazor",
            "/_framework",
            "/css",
            "/js",
            "/lib",
            "/images",
            "/favicon",
            "/super-admin",
            "/staff",
            "/manager",
            "/rentals",
            "/finance",
            "/settings",
            "/vehicles",
            "/customers"
        };

        foreach (var prefix in nonTouristPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Extension methods for registering TenantDomainMiddleware.
/// </summary>
public static class TenantDomainMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantDomainResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantDomainMiddleware>();
    }
}
