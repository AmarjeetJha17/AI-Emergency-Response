using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class ResourceStatusHistoryConfiguration
    : IEntityTypeConfiguration<ResourceStatusHistory>
{
    public void Configure(EntityTypeBuilder<ResourceStatusHistory> builder)
    {
        builder.ToTable("ResourceStatusHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PreviousStatus)
            .IsRequired();

        builder.Property(x => x.NewStatus)
            .IsRequired();

        builder.Property(x => x.ChangedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.Reason)
            .HasMaxLength(2000);

        builder.Property(x => x.ChangedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Resource)
            .WithMany()
            .HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Incident)
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.ResourceId,
            x.ChangedAtUtc
        });
    }
}
