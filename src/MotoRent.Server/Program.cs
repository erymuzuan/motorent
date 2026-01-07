using MotoRent.Client.Pages;
using MotoRent.Domain.DataContext;
using MotoRent.Server.Components;
using MotoRent.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add MotoRent data context
var connectionString = builder.Configuration.GetConnectionString("MotoRent")
    ?? "Server=localhost;Database=MotoRent;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddMotoRentDataContext(connectionString);

// Add MotoRent services
builder.Services.AddScoped<MotorbikeService>();
builder.Services.AddScoped<RenterService>();

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
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MotoRent.Client._Imports).Assembly);

app.Run();
