using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class IncidentAssignmentConfiguration
    : IEntityTypeConfiguration<IncidentAssignment>
{
    public void Configure(EntityTypeBuilder<IncidentAssignment> builder)
    {
        builder.ToTable("IncidentAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Remarks)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.IncidentId);

        builder.HasIndex(x => x.DepartmentId);

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}