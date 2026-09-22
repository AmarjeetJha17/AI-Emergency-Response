using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<IncidentType> IncidentTypes => Set<IncidentType>();

    public DbSet<IncidentTypeResourceRequirement> IncidentTypeResourceRequirements => Set<IncidentTypeResourceRequirement>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<IncidentLocation> IncidentLocations => Set<IncidentLocation>();

    public DbSet<IncidentMedia> IncidentMedia => Set<IncidentMedia>();

    public DbSet<IncidentAssignment> IncidentAssignments => Set<IncidentAssignment>();

    public DbSet<IncidentStatusHistory> IncidentStatusHistory => Set<IncidentStatusHistory>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<ResourceAssignment> ResourceAssignments => Set<ResourceAssignment>();

    public DbSet<ResourceStatusHistory> ResourceStatusHistories => Set<ResourceStatusHistory>();

    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<IncidentDuplicateMatch>
        IncidentDuplicateMatches
        => Set<IncidentDuplicateMatch>();

    public DbSet<IncidentRelation> IncidentRelations => Set<IncidentRelation>();

    public DbSet<VisualAnalysis> VisualAnalyses =>
        Set<VisualAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        if (Database.ProviderName?.EndsWith("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            modelBuilder.Entity<Incident>().Ignore(x => x.Embedding);
            modelBuilder.Entity<Resource>().Property(x => x.RowVersion).ValueGeneratedNever();

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetColumnType() is "nvarchar(max)" or "varchar(max)")
                    {
                        modelBuilder.Entity(entityType.ClrType)
                            .Property(property.Name)
                            .HasColumnType("TEXT");
                    }
                }
            }
        }
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        if (Database.ProviderName?.EndsWith("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entry in ChangeTracker.Entries<Resource>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
                }
            }
        }

        // SQL Server's native vector(1536) type does not round-trip cleanly
        // through EF change tracking — including it in an UPDATE statement
        // causes a DbUpdateConcurrencyException ("expected 1 row, affected 0").
        // Mark it as unchanged on every Modified Incident entry where the
        // Embedding was not explicitly set during this unit of work.
        // Guard with a model metadata check — test fixtures may ignore this
        // property entirely via Ignore(), in which case we skip it.
        foreach (var entry in ChangeTracker.Entries<Incident>())
        {
            if (entry.State == EntityState.Modified)
            {
                var embeddingMetadata = entry.Metadata
                    .FindProperty(nameof(Incident.Embedding));

                if (embeddingMetadata is not null)
                {
                    var embeddingProp = entry.Property(
                        nameof(Incident.Embedding));

                    if (!embeddingProp.IsModified)
                    {
                        embeddingProp.IsModified = false;
                    }
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
