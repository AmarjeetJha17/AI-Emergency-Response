using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.IntegrationTests.Infrastructure;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.IntegrationTests.Resources;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Security)]
[Collection(IntegrationTestCollection.Name)]
public sealed class ResourceConcurrencyTests : IntegrationTestBase
{
    public ResourceConcurrencyTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ConcurrentUpdates_RejectStaleWriterWithoutOverwritingWinner()
    {
        var departmentId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        await ExecuteDbAsync(async db =>
        {
            db.Departments.Add(new Department
            {
                Id = departmentId,
                Name = "Concurrency test department",
                Code = $"CT-{departmentId:N}"[..12],
                CreatedAtUtc = DateTime.UtcNow
            });
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "Original resource name",
                Type = ResourceType.FireTruck,
                Status = ResourceStatus.Available,
                DepartmentId = departmentId,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var options = Factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
        await using var requestA = new ApplicationDbContext(options);
        await using var requestB = new ApplicationDbContext(options);
        var resourceA = await requestA.Resources.SingleAsync(x => x.Id == resourceId);
        var resourceB = await requestB.Resources.SingleAsync(x => x.Id == resourceId);

        resourceA.Name = "Writer A update";
        await requestA.SaveChangesAsync();

        resourceB.Name = "Writer B stale update";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => requestB.SaveChangesAsync());

        var persistedName = await ExecuteDbAsync(db =>
            db.Resources.Where(x => x.Id == resourceId).Select(x => x.Name).SingleAsync());
        Assert.Equal("Writer A update", persistedName);
    }
}
