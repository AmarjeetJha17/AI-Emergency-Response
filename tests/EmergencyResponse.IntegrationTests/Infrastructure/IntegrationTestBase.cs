using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IDisposable
{
    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Factory = fixture.Factory;
        Client = Factory.CreateClient();
    }

    protected EmergencyResponseWebApplicationFactory Factory { get; }
    protected HttpClient Client { get; }

    protected void AuthenticateAs(string userId, string role, string? departmentId = null)
    {
        Client.DefaultRequestHeaders.Remove("X-Test-UserId");
        Client.DefaultRequestHeaders.Remove("X-Test-Role");
        Client.DefaultRequestHeaders.Remove("X-Test-DepartmentId");
        Client.DefaultRequestHeaders.Authorization = null;
        Client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        Client.DefaultRequestHeaders.Add("X-Test-Role", role);
        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            Client.DefaultRequestHeaders.Add("X-Test-DepartmentId", departmentId);
        }
    }

    protected async Task<T> ExecuteDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(db);
    }

    protected async Task ExecuteDbAsync(Func<ApplicationDbContext, Task> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(db);
    }

    public void Dispose() => Client.Dispose();
}
