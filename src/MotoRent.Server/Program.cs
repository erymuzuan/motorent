using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MotoRent.Client.Pages;
using MotoRent.Client.Services;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Server.Components;
using MotoRent.Server.Services;
using MotoRent.Services;
using MotoRent.Services.Core;
var builder = WebApplication.CreateBuilder(args);

// Add HttpContextAccessor for request context
builder.Services.AddHttpContextAccessor();

// Add request context for user/timezone services
builder.Services.AddScoped<IRequestContext, MotoRentRequestContext>();

// Add MotoRent data context (uses environment variable MOTO_SqlConnectionString)
builder.Services.AddMotoRentDataContext(MotoConfig.SqlConnectionString);

// Add MotoRent services
builder.Services.AddScoped<MotorbikeService>();
builder.Services.AddScoped<RenterService>();
builder.Services.AddScoped<InsuranceService>();
builder.Services.AddScoped<AccessoryService>();
builder.Services.AddScoped<RentalService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<DocumentOcrService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<DepositService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<MaintenanceService>();

// Add HttpClient for external API calls (Gemini)
builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Add Core services
builder.Services.AddScoped<IDirectoryService, SqlDirectoryService>();
builder.Services.AddScoped<ISubscriptionService, SqlSubscriptionService>();

// Add UI services (Modal, Toast, Dialog)
builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DialogService>();

// Configure Authentication
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logoff";
    options.AccessDeniedPath = "/account/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Cookie.Name = "MotoRent.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddCookie("ExternalAuth", options =>
{
    options.Cookie.Name = "MotoRent.External";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});

// Add Google authentication only if configured (uses MOTO_GoogleClientId, MOTO_GoogleClientSecret)
var googleClientId = MotoConfig.GoogleClientId;
if (!string.IsNullOrEmpty(googleClientId))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = MotoConfig.GoogleClientSecret ?? "";
        options.SignInScheme = "ExternalAuth";
        options.CallbackPath = "/signin-google";
    });
}

// Add Microsoft authentication only if configured (uses MOTO_MicrosoftClientId, MOTO_MicrosoftClientSecret)
var microsoftClientId = MotoConfig.MicrosoftClientId;
if (!string.IsNullOrEmpty(microsoftClientId))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = MotoConfig.MicrosoftClientSecret ?? "";
        options.SignInScheme = "ExternalAuth";
        options.CallbackPath = "/signin-microsoft";
    });
}

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(UserAccount.POLICY_SUPER_ADMIN_IMPERSONATE, policy =>
        policy.RequireClaim("SuperAdmin"));

    options.AddPolicy("RequireOrgAdmin", policy =>
        policy.RequireRole(UserAccount.ORG_ADMIN, UserAccount.SUPER_ADMIN));

    options.AddPolicy("RequireShopManager", policy =>
        policy.RequireRole(UserAccount.SHOP_MANAGER, UserAccount.ORG_ADMIN, UserAccount.SUPER_ADMIN));

    // Tenant-specific policies (require AccountNo claim = tenant context)
    // SuperAdmin must impersonate a tenant user to access these pages
    options.AddPolicy("RequireTenantStaff", policy =>
        policy.RequireRole(UserAccount.STAFF, UserAccount.SHOP_MANAGER, UserAccount.ORG_ADMIN)
              .RequireClaim("AccountNo"));

    options.AddPolicy("RequireTenantManager", policy =>
        policy.RequireRole(UserAccount.SHOP_MANAGER, UserAccount.ORG_ADMIN)
              .RequireClaim("AccountNo"));

    options.AddPolicy("RequireTenantOrgAdmin", policy =>
        policy.RequireRole(UserAccount.ORG_ADMIN)
              .RequireClaim("AccountNo"));

    // Require authentication by default for all pages/endpoints
    // Use [AllowAnonymous] attribute to allow anonymous access to specific endpoints
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Add controllers for authentication endpoints
builder.Services.AddControllersWithViews();

// Add localization services
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "th" };
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure ObjectBuilder for service resolution
app.Services.ConfigureObjectBuilder();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development-specific middleware
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Localization middleware
app.UseRequestLocalization();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map API controllers for authentication endpoints
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(MotoRent.Client._Imports).Assembly);

app.Run();
