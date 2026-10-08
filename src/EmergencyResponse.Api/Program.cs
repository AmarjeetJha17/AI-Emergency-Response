using System.Text;
using EmergencyResponse.Application.Dashboard;
using EmergencyResponse.Application.Escalation;
using EmergencyResponse.Application.Map;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Application.Situations.Jobs;
using EmergencyResponse.Infrastructure.Dashboard;
using EmergencyResponse.Infrastructure.Escalation;
using EmergencyResponse.Infrastructure.Map;
using EmergencyResponse.Infrastructure.Resources;
using EmergencyResponse.Infrastructure.Situations;
using EmergencyResponse.Application.Notifications;
using EmergencyResponse.Infrastructure.Notifications;

using EmergencyResponse.Api.Health;
using EmergencyResponse.Api.Middleware;
using EmergencyResponse.Application.Observability;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using EmergencyResponse.Api.Common.ErrorHandling;
using EmergencyResponse.Api.Hubs;
using EmergencyResponse.Api.Services;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Incidents;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Infrastructure.AI;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using EmergencyResponse.Infrastructure.Services;
using EmergencyResponse.Infrastructure.BackgroundJobs;
using EmergencyResponse.Application.Reliability;
using Azure.Messaging.ServiceBus;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Azure.Identity;

using FluentValidation;
using FluentValidation.AspNetCore;
using EmergencyResponse.Application.Validators.Authentication;
using EmergencyResponse.Application.Security;
using EmergencyResponse.Application.Security.Authorization;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Test"))
    {
        options.UseSqlite(
            builder.Configuration.GetConnectionString("DefaultConnection"));
    }
    else
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

builder.Services.AddDataProtection();
builder.Services.AddOptions<EmergencyResponse.Application.Common.Configuration.RetentionOptions>()
    .Bind(builder.Configuration.GetSection(
        EmergencyResponse.Application.Common.Configuration.RetentionOptions.SectionName))
    .Validate(options => options.CopilotConversationDays > 0 &&
                         options.NotificationDays > 0 &&
                         options.ApplicationLogDays > 0 &&
                         options.FailedProcessingArtifactDays > 0 &&
                         options.CleanupIntervalHours > 0,
        "Retention values must be positive engineering defaults.")
    .ValidateOnStart();
builder.Services.AddHostedService<
    EmergencyResponse.Infrastructure.BackgroundJobs.DataRetentionBackgroundWorker>();

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

builder.Services.Configure<ResourceRecommendationOptions>(
    builder.Configuration.GetSection(
        ResourceRecommendationOptions.SectionName));

builder.Services.Configure<SituationDetectionOptions>(
    builder.Configuration.GetSection(
        SituationDetectionOptions.SectionName));

builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(
        SecurityOptions.SectionName));

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

builder.Services.AddScoped<
    IIncidentAnnotationService,
    IncidentAnnotationService>();

builder.Services.AddScoped<
    ICopilotActionNotificationService,
    CopilotActionNotificationService>();

builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();

builder.Services.AddScoped<
    IEmergencyMapService,
    EmergencyMapService>();

builder.Services.AddScoped<
    IResourceService,
    ResourceService>();

builder.Services.AddScoped<
    IIncidentEscalationService,
    IncidentEscalationService>();

builder.Services.AddScoped<
    ISituationNumberGenerator,
    SituationNumberGenerator>();

builder.Services.AddScoped<
    IOperationalSituationService,
    OperationalSituationService>();
builder.Services.AddScoped<
    ISituationService>(sp => (ISituationService)sp.GetRequiredService<IOperationalSituationService>());

builder.Services.AddSingleton<
    ISituationEvaluationQueue,
    SituationEvaluationQueue>();

builder.Services.AddHostedService<
    IncidentSituationBackgroundWorker>();

builder.Services.AddSingleton<
    INotificationRuleEngine,
    NotificationRuleEngine>();

builder.Services.AddScoped<
    INotificationService,
    NotificationService>();

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

builder.Services.AddSingleton<
        Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider,
        PermissionPolicyProvider>();
builder.Services.AddSingleton<
        Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
        PermissionAuthorizationHandler>();

var securityOptions = builder.Configuration
        .GetSection(SecurityOptions.SectionName)
        .Get<SecurityOptions>()
        ?? new SecurityOptions();

if (builder.Environment.IsEnvironment("Test"))
{
        securityOptions.RateLimits.AuthenticationPerMinute = 10_000;
        securityOptions.RateLimits.IncidentCreatePerMinute = 10_000;
        securityOptions.RateLimits.MediaUploadPerMinute = 10_000;
        securityOptions.RateLimits.CopilotPerMinute = 10_000;
        securityOptions.RateLimits.AdminPerMinute = 10_000;
}

builder.Services.AddRateLimiter(options =>
{
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Security");
            logger.LogWarning(
                "SEC_RATE_LIMIT_EXCEEDED Path={Path} TraceId={TraceId}",
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier);

            await ValueTask.CompletedTask;
        };

        options.AddFixedWindowLimiter(
            "authentication",
            limiter =>
            {
                limiter.PermitLimit =
                    securityOptions.RateLimits.AuthenticationPerMinute;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });

        options.AddFixedWindowLimiter(
            "incident-create",
            limiter =>
            {
                limiter.PermitLimit =
                    securityOptions.RateLimits.IncidentCreatePerMinute;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });

        options.AddFixedWindowLimiter(
            "media-upload",
            limiter =>
            {
                limiter.PermitLimit =
                    securityOptions.RateLimits.MediaUploadPerMinute;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });

        options.AddFixedWindowLimiter(
            "copilot",
            limiter =>
            {
                limiter.PermitLimit =
                    securityOptions.RateLimits.CopilotPerMinute;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });

        options.AddFixedWindowLimiter(
            "admin-api",
            limiter =>
            {
                limiter.PermitLimit =
                    securityOptions.RateLimits.AdminPerMinute;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
});

builder.Services.AddAI(builder.Configuration);

builder.Services.AddScoped<IBackgroundJobScheduler, OutboxBackgroundJobScheduler>();
if (builder.Environment.IsProduction())
{
    var serviceBusNamespace = builder.Configuration["Messaging:ServiceBus:FullyQualifiedNamespace"];
    if (string.IsNullOrWhiteSpace(serviceBusNamespace))
        throw new InvalidOperationException("Production requires Messaging:ServiceBus:FullyQualifiedNamespace.");
    builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential()));
    builder.Services.AddSingleton<IApplicationMessageBus, AzureServiceBusMessageBus>();
    builder.Services.AddHostedService<AzureServiceBusConsumerBackgroundWorker>();
}
else
{
    builder.Services.AddSingleton<IApplicationMessageBus, InMemoryApplicationMessageBus>();
}
builder.Services.AddHostedService<OutboxPublisherBackgroundWorker>();
builder.Services.Configure<ReliableMessagingOptions>(builder.Configuration.GetSection(ReliableMessagingOptions.SectionName));

builder.Services.Configure<AIOptions>(
    builder.Configuration.GetSection(
        AIOptions.SectionName));

builder.Services.Configure<MapOptions>(
    builder.Configuration.GetSection(
        MapOptions.SectionName));

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

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService(
            serviceName: "EmergencyResponse.Api",
            serviceVersion: "1.0.0");
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(EmergencyResponseTelemetry.ActivitySourceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(EmergencyResponseTelemetry.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddConsoleExporter();
    });

builder.Services.AddSingleton<IWorkerHealthState, WorkerHealthState>();

builder.Services.AddHealthChecks()
    .AddCheck<SqlServerHealthCheck>("sqlserver", tags: ["ready"])
    .AddCheck<BackgroundWorkerHealthCheck>("workers")
    .AddCheck<AIProviderHealthCheck>("ai")
    .AddCheck<PredictiveModelHealthCheck>("predictive-model");


var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext =
        services.GetRequiredService<ApplicationDbContext>();

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();

    if (app.Environment.IsEnvironment("Test"))
    {
        await dbContext.Database.EnsureCreatedAsync();
    }

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

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health");

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("WebClient");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<IncidentHub>("/hubs/incidents");

app.MapControllers();

app.Run();
