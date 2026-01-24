using System.Text.RegularExpressions;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Core;

/// <summary>
/// Implementation of IOnboardingService for handling SaaS signup.
/// </summary>
public class OnboardingService(
    CoreDataContext coreDataContext,
    RentalDataContext rentalDataContext,
    IDirectoryService directoryService) : IOnboardingService
{
    private CoreDataContext CoreDataContext { get; } = coreDataContext;
    private RentalDataContext RentalDataContext { get; } = rentalDataContext;
    private IDirectoryService DirectoryService { get; } = directoryService;

    public async Task<Organization> OnboardAsync(OnboardingRequest request)
    {
        // 1. Generate unique AccountNo
        var accountNo = await this.GenerateAccountNoAsync(request.ShopName);

        // 2. Create Organization (Core Schema)
        var org = new Organization
        {
            AccountNo = accountNo,
            Name = request.ShopName,
            Email = request.Email,
            Phone = request.Phone,
            PreferredLanguage = request.PreferredLanguage,
            SubscriptionPlan = request.Plan,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(30), // 30-day Pro trial
            IsActive = true
        };

        // 3. Create or Update User (Core Schema)
        var user = await this.DirectoryService.GetUserAsync(request.Email.ToLowerInvariant());
        if (user == null)
        {
            user = new User
            {
                UserName = request.Email.ToLowerInvariant(),
                Email = request.Email.ToLowerInvariant(),
                FullName = request.FullName,
                CredentialProvider = request.Provider,
                NameIdentifier = request.ProviderId,
                GoogleId = request.Provider == User.GOOGLE ? request.ProviderId : null,
                LineId = request.Provider == User.LINE ? request.ProviderId : null
            };
        }
        else
        {
            // Link provider if not already linked
            if (request.Provider == User.GOOGLE) user.GoogleId = request.ProviderId;
            if (request.Provider == User.LINE) user.LineId = request.ProviderId;
        }

        // Add OrgAdmin role to user for this account
        var account = user.AccountCollection.FirstOrDefault(a => a.AccountNo == accountNo);
        if (account == null)
        {
            user.AccountCollection.Add(new UserAccount
            {
                AccountNo = accountNo,
                Roles = [UserAccount.ORG_ADMIN]
            });
        }

        // Save Org and User in Core context
        using (var coreSession = this.CoreDataContext.OpenSession(user.UserName))
        {
            coreSession.Attach(org);
            coreSession.Attach(user);
            await coreSession.SubmitChanges("Onboarding");
        }

        // 4. Create Shop (Tenant Schema)
        // We need to set the context to the new account so the RentalDataContext knows which schema to use
        // However, since we are using the service inside the server, we might need a way to override schema.
        // For now, we'll assume the implementation of RentalDataContext can handle dynamic schema if AccountNo is provided.
        
        var shop = new Shop
        {
            Name = request.ShopName,
            Location = request.Location,
            IsActive = true
        };

        using (var rentalSession = this.RentalDataContext.OpenSession(user.UserName))
        {
            // Set AccountNo for this session to target the new tenant schema
            rentalSession.SetAccountNo(accountNo);
            
            rentalSession.Attach(shop);
            await rentalSession.SubmitChanges("InitialSetup");
            
            // 5. Create Initial Vehicles
            foreach (var fleet in request.Fleet)
            {
                for (int i = 0; i < fleet.Quantity; i++)
                {
                    var vehicle = new Vehicle
                    {
                        HomeShopId = shop.ShopId,
                        CurrentShopId = shop.ShopId,
                        VehicleType = fleet.VehicleType,
                        Brand = fleet.Brand ?? "",
                        Model = fleet.Model ?? "",
                        Status = "Available",
                        LicensePlate = $"NEW-{accountNo}-{i+1}"
                    };
                    rentalSession.Attach(vehicle);
                }
            }
            
            if (request.Fleet.Any())
            {
                await rentalSession.SubmitChanges("InitialFleet");
            }
        }

        return org;
    }

    private async Task<string> GenerateAccountNoAsync(string shopName)
    {
        // Create slug from shop name
        string slug = Regex.Replace(shopName.ToLowerInvariant(), @"[^a-z0-9]", "");
        if (slug.Length > 10) slug = slug.Substring(0, 10);
        if (string.IsNullOrWhiteSpace(slug)) slug = "shop";

        string accountNo = slug;
        int suffix = 1;

        // Ensure uniqueness
        while (await this.CoreDataContext.ExistAsync<Organization>(o => o.AccountNo == accountNo))
        {
            string suffixStr = suffix.ToString();
            accountNo = slug.Length + suffixStr.Length > 15 
                ? slug.Substring(0, 15 - suffixStr.Length) + suffixStr 
                : slug + suffixStr;
            suffix++;
        }

        return accountNo;
    }
}
