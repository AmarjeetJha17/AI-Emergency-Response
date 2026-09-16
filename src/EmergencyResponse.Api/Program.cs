using System.Text;

using EmergencyResponse.Api.Common.ErrorHandling;
using EmergencyResponse.Api.Hubs;
using EmergencyResponse.Api.Services;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Infrastructure.AI;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using EmergencyResponse.Infrastructure.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDataProtection();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<
    IAuthenticationService,
    AuthenticationService>();

builder.Services.Configure<AdminSeedOptions>(
    builder.Configuration.GetSection(
        AdminSeedOptions.SectionName));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(
        JwtOptions.SectionName));

builder.Services.AddScoped<
    IJwtTokenService,
    JwtTokenService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()!;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.SecretKey)),

                ClockSkew = TimeSpan.FromSeconds(30)
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path =
                    context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/incidents"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<
    IIncidentNumberGenerator,
    IncidentNumberGenerator>();

builder.Services.AddScoped<
    IIncidentService,
    IncidentService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentUserService,
    CurrentUserService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "OperatorAccess",
        policy =>
        {
            policy.RequireRole(
                ApplicationRoles.Operator,
                ApplicationRoles.EmergencyAdmin,
                ApplicationRoles.SystemAdmin);
        });

    options.AddPolicy(
        "ResponderAccess",
        policy =>
        {
            policy.RequireRole(
                ApplicationRoles.Responder,
                ApplicationRoles.Operator,
                ApplicationRoles.EmergencyAdmin,
                ApplicationRoles.SystemAdmin);
        });

    options.AddPolicy(
        "AdministrationAccess",
        policy =>
        {
            policy.RequireRole(
                ApplicationRoles.EmergencyAdmin,
                ApplicationRoles.SystemAdmin);
        });
});

builder.Services.AddAI(builder.Configuration);

builder.Services.Configure<AIOptions>(
    builder.Configuration.GetSection(
        AIOptions.SectionName));

builder.Services.AddSignalR();

builder.Services.AddScoped<
    IIncidentNotificationService,
    IncidentNotificationService>();

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "WebClient",
            policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5021",
                        "https://localhost:7201")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext =
        services.GetRequiredService<ApplicationDbContext>();

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();

    await DatabaseSeeder.SeedAsync(dbContext);

    await RoleSeeder.SeedAsync(roleManager);

    var adminOptions =
        services.GetRequiredService<
            IOptions<AdminSeedOptions>>();

    await AdminSeeder.SeedAsync(
        userManager,
        adminOptions);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("WebClient");

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<IncidentHub>("/hubs/incidents");

app.MapControllers();

app.Run();
