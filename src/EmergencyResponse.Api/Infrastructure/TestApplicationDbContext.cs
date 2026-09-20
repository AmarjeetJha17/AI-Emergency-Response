using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Api.Infrastructure;

/// <summary>
/// A thin DbContext subclass used in the Test environment only.
/// Ignores SQL Server-specific columns (vector(1536)) that are incompatible
/// with the SQLite in-memory database used by integration tests.
/// </summary>
public sealed class TestApplicationDbContext : ApplicationDbContext
{
    public TestApplicationDbContext(
        DbContextOptions<TestApplicationDbContext> options)
        : base(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(
                options.Extensions
                    .OfType<Microsoft.EntityFrameworkCore.Infrastructure.RelationalOptionsExtension>()
                    .First()
                    .ConnectionString)
            .Options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // vector(1536) is a SQL Server-native type — ignore it in SQLite.
        modelBuilder.Entity<Incident>()
            .Ignore(x => x.Embedding);

        // SQL Server uses nvarchar(max) which SQLite does not understand.
        // Remap all max-length string columns to TEXT.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var colType = property.GetColumnType();
                if (colType is "nvarchar(max)" or "varchar(max)" or
                    "nvarchar(MAX)" or "varchar(MAX)")
                {
                    modelBuilder
                        .Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasColumnType("TEXT");
                }
            }
        }
    }
}
