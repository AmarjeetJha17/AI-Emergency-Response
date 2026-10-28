using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Web.Services;
using EmergencyResponse.Web.Services.Api;
using EmergencyResponse.Web.Middleware;

using Microsoft.AspNetCore.Authentication.Cookies;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Azure.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using EmergencyResponse.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// OpenTelemetry Configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService(
            serviceName: "EmergencyResponse.Web",
            serviceVersion: "1.0.0");
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("EmergencyResponse.Web")
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddConsoleExporter();
    });

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    if (!builder.Environment.IsDevelopment() &&
        !builder.Environment.IsEnvironment("Test"))
    {
        // Azure Container Apps ingress is the only network path to this container.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

var dataProtectionMode = DataProtectionConfiguration.Configure(
    builder.Services,
    builder.Configuration,
    builder.Environment.EnvironmentName);

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ApiSettings:BaseUrl"]!);
    })
    .AddStandardResilienceHandler();

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        options.Cookie.SameSite =
            SameSiteMode.Lax;
    });

builder.Services
    .AddHttpClient<IIncidentApiClient, IncidentApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<IIncidentTypeApiClient, IncidentTypeApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<IDashboardApiClient, DashboardApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<IEmergencyMapApiClient, EmergencyMapApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<IResourceApiClient, ResourceApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<IEscalationApiClient, EscalationApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<ISituationApiClient, SituationApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<ICopilotApiClient, CopilotApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<INotificationApiClient, NotificationApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services.Configure<MapOptions>(
    builder.Configuration.GetSection(
        MapOptions.SectionName));

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<BearerTokenHandler>();

var syncfusionLicense =
    builder.Configuration["Syncfusion:LicenseKey"];

if (string.IsNullOrWhiteSpace(syncfusionLicense))
{
    throw new InvalidOperationException(
        "Syncfusion license key is not configured.");
}

Syncfusion.Licensing.SyncfusionLicenseProvider
    .RegisterLicense(syncfusionLicense);

var app = builder.Build();

if (dataProtectionMode == DataProtectionMode.PortfolioDemoUnwrappedBlob)
{
    app.Logger.LogWarning(
        "ASP.NET Core Data Protection keys are persisted in Blob Storage without Azure Key Vault wrapping. This reduced-security mode is permitted only for synthetic PortfolioDemo data.");
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health"),
    redirect => redirect.UseHttpsRedirection());
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "live"
}));

app.MapGet("/health/ready", () => Results.Ok(new
{
    status = "ready"
}));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
