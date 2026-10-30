using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.Infrastructure.BackgroundJobs;

public static class DataRetentionWorkerRegistration
{
    public static IServiceCollection AddDataRetentionBackgroundWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Default to the historical behavior outside deployments that explicitly
        // opt out. Portfolio environments can prevent startup cleanup/deletion.
        if (configuration.GetValue<bool?>("DataRetention:BackgroundWorkerEnabled") ?? true)
        {
            services.AddHostedService<DataRetentionBackgroundWorker>();
        }

        return services;
    }
}
