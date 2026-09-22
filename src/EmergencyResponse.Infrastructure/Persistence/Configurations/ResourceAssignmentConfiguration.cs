using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class ResourceAssignmentConfiguration
    : IEntityTypeConfiguration<ResourceAssignment>
{
    public void Configure(EntityTypeBuilder<ResourceAssignment> builder)
    {
        builder.ToTable("ResourceAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Remarks)
            .HasMaxLength(1000);

        builder.Property(x => x.ReleaseNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.AssignedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.AssignedAtUtc)
            .IsRequired();

        // Concurrency protection: guarantees at most one active assignment per resource
        builder.HasIndex(x => x.ResourceId)
            .HasFilter("IsActive = 1")
            .IsUnique();

        builder.HasIndex(x => x.IncidentId);

        builder.HasOne(x => x.Resource)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Incident)
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}