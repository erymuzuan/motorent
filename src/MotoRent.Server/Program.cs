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

// Add Core services
builder.Services.AddScoped<IDirectoryService, SqlDirectoryService>();

// Configure Authentication
builder.Services.AddAuthentication(options =>
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
})
.AddGoogle(options =>
{
    var googleSection = builder.Configuration.GetSection("Authentication:Google");
    options.ClientId = googleSection["ClientId"] ?? "";
    options.ClientSecret = googleSection["ClientSecret"] ?? "";
    options.SignInScheme = "ExternalAuth";
    options.CallbackPath = "/signin-google";
})
.AddMicrosoftAccount(options =>
{
    var microsoftSection = builder.Configuration.GetSection("Authentication:Microsoft");
    options.ClientId = microsoftSection["ClientId"] ?? "";
    options.ClientSecret = microsoftSection["ClientSecret"] ?? "";
    options.SignInScheme = "ExternalAuth";
    options.CallbackPath = "/signin-microsoft";
});

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

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure ObjectBuilder for service resolution
app.Services.ConfigureObjectBuilder();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map API controllers for authentication endpoints
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MotoRent.Client._Imports).Assembly);

app.Run();
