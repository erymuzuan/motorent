using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MotoRent.Client.Pages;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Server.Components;
using MotoRent.Server.Services;
using MotoRent.Services;
using MotoRent.Services.Core;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add HttpContextAccessor for request context
builder.Services.AddHttpContextAccessor();

// Add request context for user/timezone services
builder.Services.AddScoped<IRequestContext, MotoRentRequestContext>();

// Add MotoRent data context
var connectionString = builder.Configuration.GetConnectionString("MotoRent")
    ?? "Server=localhost;Database=MotoRent;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddMotoRentDataContext(connectionString);

// Add MotoRent services
builder.Services.AddScoped<MotorbikeService>();
builder.Services.AddScoped<RenterService>();
builder.Services.AddScoped<InsuranceService>();
builder.Services.AddScoped<AccessoryService>();
builder.Services.AddScoped<RentalService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<DocumentOcrService>();
builder.Services.AddScoped<PaymentService>();

// Add HttpClient for external API calls (Gemini)
builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Add Core services
builder.Services.AddScoped<IDirectoryService, SqlDirectoryService>();

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

// Add Google authentication only if configured
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrEmpty(googleClientId))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.SignInScheme = "ExternalAuth";
        options.CallbackPath = "/signin-google";
    });
}

// Add Microsoft authentication only if configured
var microsoftClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
if (!string.IsNullOrEmpty(microsoftClientId))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "";
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
});

// Add controllers for authentication endpoints
builder.Services.AddControllers();

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
