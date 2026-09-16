using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class IncidentRelationConfiguration
    : IEntityTypeConfiguration<IncidentRelation>
{
    public void Configure(
        EntityTypeBuilder<IncidentRelation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RelationType)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedByUserId)
            .HasMaxLength(450);

        builder.HasOne(x => x.ParentIncident)
            .WithMany()
            .HasForeignKey(x => x.ParentIncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RelatedIncident)
            .WithMany()
            .HasForeignKey(x => x.RelatedIncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
            {
                x.ParentIncidentId,
                x.RelatedIncidentId,
                x.RelationType
            })
            .IsUnique();
    }
}