using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Web.Services;
using EmergencyResponse.Web.Services.Api;
using EmergencyResponse.Web.Middleware;

using Microsoft.AspNetCore.Authentication.Cookies;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;

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

var dataProtectionBlobUri = builder.Configuration["DataProtection:BlobUri"];
var dataProtectionKeyIdentifier = builder.Configuration["DataProtection:KeyIdentifier"];
if (!string.IsNullOrWhiteSpace(dataProtectionBlobUri) ||
    !string.IsNullOrWhiteSpace(dataProtectionKeyIdentifier))
{
    if (string.IsNullOrWhiteSpace(dataProtectionBlobUri) ||
        string.IsNullOrWhiteSpace(dataProtectionKeyIdentifier))
    {
        throw new InvalidOperationException(
            "Both DataProtection:BlobUri and DataProtection:KeyIdentifier must be configured together.");
    }

    var credential = new DefaultAzureCredential();
    builder.Services.AddDataProtection()
        .SetApplicationName("EmergencyResponse.Web")
        .PersistKeysToAzureBlobStorage(new Uri(dataProtectionBlobUri), credential)
        .ProtectKeysWithAzureKeyVault(new Uri(dataProtectionKeyIdentifier), credential);
}

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

// Configure the HTTP request pipeline.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
