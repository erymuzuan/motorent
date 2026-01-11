using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services.Core;

/// <summary>
/// SQL-based directory service for user authentication, claims generation, and organization management.
/// </summary>
public class SqlDirectoryService : IDirectoryService
{
    private readonly CoreDataContext m_context;

    public SqlDirectoryService(CoreDataContext context)
    {
        m_context = context;
    }

    #region User Management

    public async Task<User?> GetUserAsync(string userName)
    {
        return await m_context.LoadOneAsync<User>(u => u.UserName == userName);
    }

    public async Task<IEnumerable<User>> GetUsersAsync(string accountNo)
    {
        // Load all users and filter by account in memory (since AccountCollection is in JSON)
        var query = m_context.Users.OrderBy(u => u.UserName);
        var result = await m_context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        return result.ItemCollection
            .Where(u => u.AccountCollection.Any(a => a.AccountNo == accountNo));
    }

    public async Task<IEnumerable<User>> GetUsersInRoleAsync(string accountNo, string role)
    {
        var users = await GetUsersAsync(accountNo);
        return users.Where(u => u.HasRole(accountNo, role));
    }

    public async Task SaveUserProfileAsync(User user)
    {
        using var session = m_context.OpenSession("system");
        session.Attach(user);
        await session.SubmitChanges("SaveUserProfile");
    }

    public async Task<UserAuthenticationStatus> AuthenticateAsync(string userName, string password)
    {
        var user = await GetUserAsync(userName);

        if (user == null)
            return UserAuthenticationStatus.NotFound;

        if (user.IsLockedOut)
            return UserAuthenticationStatus.Locked;

        if (user.CredentialProvider != User.CUSTOM)
            return UserAuthenticationStatus.Unauthenticated;

        if (string.IsNullOrWhiteSpace(user.Salt) || string.IsNullOrWhiteSpace(user.HashedPassword))
            return UserAuthenticationStatus.Unauthenticated;

        if (!VerifyPassword(password, user.Salt, user.HashedPassword))
            return UserAuthenticationStatus.Unauthenticated;

        return UserAuthenticationStatus.Authenticated;
    }

    #endregion

    #region Organization Management

    public async Task<Organization?> GetOrganizationAsync(string accountNo)
    {
        return await m_context.LoadOneAsync<Organization>(o => o.AccountNo == accountNo);
    }

    public async Task<IEnumerable<Organization>> GetOrganizationsAsync()
    {
        var query = m_context.Organizations
            .Where(o => o.IsActive)
            .OrderBy(o => o.Name);

        var result = await m_context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task SaveOrganizationAsync(Organization organization)
    {
        using var session = m_context.OpenSession("system");
        session.Attach(organization);
        await session.SubmitChanges("SaveOrganization");
    }

    #endregion

    #region Claims and Tokens

    public async Task<string?> GetAccountNoAsync(string userName)
    {
        var user = await GetUserAsync(userName);
        return user?.AccountNo;
    }

    public async Task<string[]> GetSubscriptionsAsync(string userName)
    {
        var user = await GetUserAsync(userName);
        if (user?.AccountNo == null) return [];

        var org = await GetOrganizationAsync(user.AccountNo);
        return org?.Subscriptions ?? [];
    }

    public async Task<IEnumerable<Claim>> GetClaimsAsync(string userName, string? account)
    {
        var claims = new List<Claim>();

        // Check if super admin (no account required)
        if (MotoConfig.SuperAdmins.Contains(userName, StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Name, userName));
            claims.Add(new Claim(ClaimTypes.Role, UserAccount.SUPER_ADMIN));
            claims.Add(new Claim(ClaimTypes.Role, UserAccount.REGISTERED_USER));
            claims.Add(new Claim("SuperAdmin", userName));

            // If account is specified, also load that account's claims
            if (!string.IsNullOrWhiteSpace(account))
            {
                var org = await GetOrganizationAsync(account);
                if (org != null)
                {
                    claims.Add(new Claim("AccountNo", account));
                    claims.Add(new Claim("Timezone", (org.Timezone ?? 7d).ToString()));
                    claims.Add(new Claim("FirstDay", (org.FirstDay ?? DayOfWeek.Monday).ToString()));
                    claims.Add(new Claim("Currency", org.Currency ?? "THB"));

                    // Add subscriptions
                    foreach (var sub in org.Subscriptions ?? [])
                    {
                        claims.Add(new Claim($"subscription:{sub}", "true"));
                    }

                    // Also load the user's tenant-specific roles if they have an account
                    var superAdminUser = await GetUserAsync(userName);
                    if (superAdminUser != null)
                    {
                        var superAdminAccount = superAdminUser.AccountCollection.FirstOrDefault(a => a.AccountNo == account);
                        if (superAdminAccount != null)
                        {
                            foreach (var role in superAdminAccount.Roles)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role));
                            }
                        }
                    }
                }
            }

            return claims;
        }

        // Regular user - must have an account
        if (string.IsNullOrWhiteSpace(account))
            return [];

        var user = await GetUserAsync(userName);
        if (user == null) return [];

        var organization = await GetOrganizationAsync(account);
        if (organization == null) return [];

        // Basic claims
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, userName));
        claims.Add(new Claim(ClaimTypes.Email, user.Email ?? userName));
        claims.Add(new Claim("FullName", user.FullName ?? userName));
        claims.Add(new Claim("AccountNo", account));
        claims.Add(new Claim(ClaimTypes.Role, UserAccount.REGISTERED_USER));

        // Organization claims
        claims.Add(new Claim("Timezone", (organization.Timezone ?? 7d).ToString()));
        claims.Add(new Claim("FirstDay", (organization.FirstDay ?? DayOfWeek.Monday).ToString()));
        claims.Add(new Claim("Currency", organization.Currency ?? "THB"));
        claims.Add(new Claim("Language", organization.Language ?? "th-TH"));

        // Role claims from UserAccount
        var userAccount = user.AccountCollection.FirstOrDefault(a => a.AccountNo == account);
        if (userAccount != null)
        {
            foreach (var role in userAccount.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (!string.IsNullOrWhiteSpace(userAccount.StartPage))
            {
                claims.Add(new Claim("StartPage", userAccount.StartPage));
            }
        }

        // Subscription claims
        foreach (var sub in organization.Subscriptions ?? [])
        {
            claims.Add(new Claim($"subscription:{sub}", "true"));
        }

        return claims;
    }

    public async Task<string> CreateJwtTokenAsync(string userName, string account)
    {
        var claims = await GetClaimsAsync(userName, account);
        var claimsList = claims.ToList();

        var secret = MotoConfig.JwtSecret;
        var key = Encoding.ASCII.GetBytes(secret);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimsList),
            Expires = DateTime.UtcNow.AddMonths(6),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Audience = "motorent-api",
            Issuer = "motorent"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public Task<IEnumerable<Claim>> ReadJwtTokenAsync(string jwt)
    {
        try
        {
            var secret = MotoConfig.JwtSecret;
            var key = Encoding.ASCII.GetBytes(secret);

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "motorent",
                ValidateAudience = true,
                ValidAudience = "motorent-api",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(jwt, validationParameters, out var validatedToken);
            return Task.FromResult<IEnumerable<Claim>>(principal.Claims);
        }
        catch
        {
            return Task.FromResult<IEnumerable<Claim>>([]);
        }
    }

    #endregion

    #region Password Management

    public string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + salt;
        var bytes = Encoding.UTF8.GetBytes(saltedPassword);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public string GenerateSalt()
    {
        var saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    public bool VerifyPassword(string password, string salt, string hashedPassword)
    {
        var hash = HashPassword(password, salt);
        return hash == hashedPassword;
    }

    #endregion
}
