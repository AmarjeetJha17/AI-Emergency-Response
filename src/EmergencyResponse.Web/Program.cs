using EmergencyResponse.Web.Services;
using EmergencyResponse.Web.Services.Api;

using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ApiSettings:BaseUrl"]!);
    });

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
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services
    .AddHttpClient<IIncidentTypeApiClient, IncidentTypeApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services
    .AddHttpClient<IDashboardApiClient, DashboardApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiSettings:BaseUrl"]!);
        })
    .AddHttpMessageHandler<BearerTokenHandler>();

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
