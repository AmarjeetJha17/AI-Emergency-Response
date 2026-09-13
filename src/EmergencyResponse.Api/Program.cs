using EmergencyResponse.Api.Common.ErrorHandling;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using EmergencyResponse.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EmergencyResponse.Application.Common.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EmergencyResponse.Infrastructure.Services;

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
    });

builder.Services.AddScoped<
    IIncidentNumberGenerator,
    IncidentNumberGenerator>();

builder.Services.AddScoped<
    IIncidentService,
    IncidentService>();

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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
