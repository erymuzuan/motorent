using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MotoRent.Client.Interops;
using MotoRent.Client.Pages;
using MotoRent.Client.Services;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Storage;
using MotoRent.Server.Components;
using MotoRent.Server.Services;
using MotoRent.Services;
using MotoRent.Services.Core;
using MotoRent.Services.Search;
using MotoRent.Services.Storage;
using MotoRent.Services.Tourist;
using MotoRent.Domain.Search;
using MotoRent.Server.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add HttpContextAccessor for request context
builder.Services.AddHttpContextAccessor();

// Add request context for user/timezone services
// Uses TouristRequestContext for /tourist/* paths (URL-based tenant resolution)
// Uses MotoRentRequestContext for authenticated pages (claims-based tenant resolution)
builder.Services.AddScoped<IRequestContext>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var path = httpContextAccessor.HttpContext?.Request.Path.Value ?? "";

    // Use TouristRequestContext for tourist pages
    if (path.StartsWith("/tourist/", StringComparison.OrdinalIgnoreCase))
    {
        return new TouristRequestContext(httpContextAccessor);
    }

    // Use standard context for authenticated pages
    return new MotoRentRequestContext(httpContextAccessor);
});

// Add MotoRent data context (uses environment variable MOTO_SqlConnectionString)
builder.Services.AddMotoRentDataContext(MotoConfig.SqlConnectionString);

// Add MotoRent services
builder.Services.AddScoped<MotorbikeService>(); // Deprecated: use VehicleService
builder.Services.AddScoped<VehiclePoolService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<VehicleImageService>();
builder.Services.AddScoped<RentalPricingService>();
builder.Services.AddScoped<RenterService>();
builder.Services.AddScoped<InsuranceService>();
builder.Services.AddScoped<AccessoryService>();
builder.Services.AddScoped<RentalService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<ShopScheduleService>();
builder.Services.AddScoped<OperatingHoursService>();
builder.Services.AddScoped<ServiceLocationService>();
builder.Services.AddScoped<DocumentOcrService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<DepositService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddScoped<DamageReportService>();
// Third-party owner services
builder.Services.AddScoped<VehicleOwnerService>();
builder.Services.AddScoped<OwnerPaymentService>();
// Accident services
builder.Services.AddScoped<AccidentService>();

// Add HttpClient for external API calls (Gemini)
builder.Services.AddHttpClient("Gemini", client => { client.Timeout = TimeSpan.FromSeconds(60); });

// Add OpenSearch HttpClient (optional, enabled via MOTO_OpenSearchHost env var)
var openSearchHost = Environment.GetEnvironmentVariable("MOTO_OpenSearchHost");
if (!string.IsNullOrEmpty(openSearchHost))
{
    var openSearchBasicAuth = Environment.GetEnvironmentVariable("MOTO_OpenSearchBasicAuth");
    builder.Services.AddHttpClient("OpenSearchHost", client =>
        {
            client.BaseAddress = new Uri(openSearchHost);
            if (!string.IsNullOrWhiteSpace(openSearchBasicAuth))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(openSearchBasicAuth)));
            }
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            return handler;
        });

    builder.Services.AddScoped<ISearchService, OpenSearchService>();
}

// Add Core services
builder.Services.AddScoped<IDirectoryService, SqlDirectoryService>();
builder.Services.AddScoped<ISubscriptionService, SqlSubscriptionService>();
builder.Services.AddScoped<OrganizationService>();

// Add Tourist services (for multi-tenant tourist pages)
builder.Services.AddScoped<ITenantResolverService, TenantResolverService>();

// Add Binary Storage (AWS S3)
builder.Services.AddSingleton<IBinaryStore, S3BinaryStore>();

// Add Message Broker (RabbitMQ) - optional, enabled via config
var rabbitMqEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled");
if (rabbitMqEnabled)
{
    builder.Services.AddSingleton<MotoRent.Domain.Messaging.IMessageBroker>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<MotoRent.Messaging.RabbitMqMessageBroker>>();
        return new MotoRent.Messaging.RabbitMqMessageBroker(logger);
    });
}

// Add JS Interop services
builder.Services.AddScoped<FileUploadJsInterop>();

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

    // Note: We don't use FallbackPolicy here because:
    // 1. It blocks the Blazor SignalR hub (/_blazor) for anonymous tourist pages
    // 2. AuthorizeRouteView in Routes.razor handles page-level authorization
    // 3. API controllers should use explicit [Authorize] attributes
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

// Add SignalR for real-time features (comments, notifications)
builder.Services.AddSignalR();

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

// Tenant domain resolution middleware (for custom domains and subdomains)
// Must be before authentication so tourist pages work for anonymous users
app.UseTenantDomainResolution();

// Localization middleware
app.UseRequestLocalization();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map API controllers for authentication endpoints
app.MapControllers();

// Map SignalR hubs
app.MapHub<MotoRent.Server.Hubs.CommentHub>("/hub-comments");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(MotoRent.Client._Imports).Assembly);

app.Run(); 
