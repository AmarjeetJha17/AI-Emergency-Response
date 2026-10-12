using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.ExternalContext;
using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Infrastructure.AI;
using EmergencyResponse.Infrastructure.BackgroundJobs;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.LoadTests.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmergencyResponse.LoadTests.Infrastructure;

public sealed class LoadTestHostFactory : WebApplicationFactory<global::Program>
{
    private readonly string _connectionString;
    private readonly LoadTestOptions _options;

    public LoadTestFaultController Faults { get; } = new();

    public LoadTestHostFactory(LoadTestOptions options)
    {
        _options = options;
        _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LoadTestDatabase")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__LoadTestDatabase to a dedicated, disposable SQL Server database before using LocalTestHost mode.");

        var databaseName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
        if (!databaseName.Contains("LoadTest", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("EmergencyResponse_IntegrationTests", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("EmergencyResponseDb", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Load tests require a database whose name includes LoadTest. Integration-test and application databases are blocked.");
        }
    }

    public LoadTestDatabaseSnapshot Data { get; private set; } = new();
    public string AccessToken { get; private set; } = string.Empty;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var dataset = string.Equals(_options.Dataset, "Large", StringComparison.OrdinalIgnoreCase)
            ? TestDataOptions.ForProfile(_options.Profile, _options.Dataset)
            : _options.TestData;
        Data = await LoadTestDatasetSeeder.SeedAsync(
            Services,
            dataset,
            _options.GeographicDistribution,
            _options.AdminEmail,
            cancellationToken);

        using var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = _options.AdminEmail, Password = _options.AdminPassword }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The load-test host did not issue a JWT.");
        AccessToken = authentication.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(FindApiContentRoot());
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestDatabase"] = _connectionString,
                ["Test:RelaxRateLimits"] = "false",
                ["Jwt:Issuer"] = "EmergencyResponse.LoadTests",
                ["Jwt:Audience"] = "EmergencyResponse.LoadTests",
                ["Jwt:SecretKey"] = "load-test-secret-key-at-least-32-characters-long",
                ["Jwt:ExpirationMinutes"] = "180",
                ["AdminSeed:Email"] = _options.AdminEmail,
                ["AdminSeed:Password"] = _options.AdminPassword,
                ["AdminSeed:FirstName"] = "Load",
                ["AdminSeed:LastName"] = "Test",
                ["AI:Endpoint"] = "https://load-test.invalid/",
                ["AI:Deployment"] = "load-test-fake",
                ["AI:Model"] = "load-test-fake",
                ["AI:EmbeddingDeployment"] = "load-test-fake",
                ["IncidentMediaStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "EmergencyResponseLoadTests")
            };
            if (!string.Equals(_options.Scenario, "rate-limit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var policy in new[] { "Authentication", "IncidentCreate", "MediaUpload", "Copilot", "Admin" })
                {
                    settings[$"Security:RateLimits:{policy}PerMinute"] = "100000";
                }
            }
            else
            {
                settings["Security:RateLimits:IncidentCreatePerMinute"] = "10";
            }

            configuration.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(Faults);
            services.AddSingleton<LoadTestDbCommandInterceptor>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>((provider, dbOptions) =>
            {
                dbOptions.UseSqlServer(_connectionString);
                dbOptions.AddInterceptors(provider.GetRequiredService<LoadTestDbCommandInterceptor>());
            });

            services.RemoveAll<IIncidentAnalysisService>();
            services.AddScoped<IIncidentAnalysisService, LoadTestIncidentAnalysisService>();
            services.RemoveAll<IIncidentVisualAnalysisService>();
            services.AddScoped<IIncidentVisualAnalysisService, LoadTestVisualAnalysisService>();
            services.RemoveAll<IVideoFrameExtractor>();
            services.AddScoped<IVideoFrameExtractor, LoadTestVideoFrameExtractor>();
            services.RemoveAll<ISituationAnalysisService>();
            services.AddScoped<ISituationAnalysisService, LoadTestSituationAnalysisService>();
            services.RemoveAll<IIncidentEmbeddingService>();
            services.AddScoped<IIncidentEmbeddingService, LoadTestEmbeddingService>();
            services.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(provider =>
                new LoadTestEmbeddingGenerator(provider.GetRequiredService<LoadTestFaultController>()));
            services.RemoveAll<IChatClient>();
            services.AddSingleton<IChatClient, LoadTestChatClient>();
            services.RemoveAll<IKnowledgeSearchService>();
            services.AddScoped<IKnowledgeSearchService, LoadTestKnowledgeSearchService>();
            services.RemoveAll<IWeatherContextClient>();
            services.AddSingleton<IWeatherContextClient, LoadTestWeatherContextClient>();
            services.RemoveAll<IIncidentMediaStorage>();
            services.RemoveAll<LoadTestMediaStorage>();
            services.AddSingleton<LoadTestMediaStorage>();
            services.AddSingleton<IIncidentMediaStorage>(provider => provider.GetRequiredService<LoadTestMediaStorage>());
            services.RemoveAll<IApplicationMessageBus>();
            services.AddSingleton<IApplicationMessageBus, LoadTestApplicationMessageBus>();
        });
    }

    private static string FindApiContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var projectRoot = Path.Combine(directory.FullName, "src", "EmergencyResponse.Api");
            if (File.Exists(Path.Combine(projectRoot, "EmergencyResponse.Api.csproj")))
            {
                return projectRoot;
            }
        }

        throw new DirectoryNotFoundException("Could not locate src/EmergencyResponse.Api from the load-test output directory.");
    }
}
