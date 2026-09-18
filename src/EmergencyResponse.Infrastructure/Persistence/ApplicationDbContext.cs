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

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<IncidentLocation> IncidentLocations => Set<IncidentLocation>();

    public DbSet<IncidentMedia> IncidentMedia => Set<IncidentMedia>();

    public DbSet<IncidentAssignment> IncidentAssignments => Set<IncidentAssignment>();

    public DbSet<IncidentStatusHistory> IncidentStatusHistory => Set<IncidentStatusHistory>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<ResourceAssignment> ResourceAssignments => Set<ResourceAssignment>();

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

        return await base.SaveChangesAsync(cancellationToken);
    }

}
