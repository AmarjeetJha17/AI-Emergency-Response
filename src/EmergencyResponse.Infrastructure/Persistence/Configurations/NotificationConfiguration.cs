using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(x => x.TargetRole)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.DeduplicationKey)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(x => x.MetadataJson)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.Status,
            x.CreatedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.TargetRole,
            x.Status,
            x.CreatedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.DeduplicationKey,
            x.UserId,
            x.Status
        });

        builder.HasIndex(x => x.IncidentId);
        builder.HasIndex(x => x.OperationalSituationId);
        builder.HasIndex(x => x.IncidentEscalationId);

        builder.HasOne(x => x.Incident)
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.OperationalSituation)
            .WithMany()
            .HasForeignKey(x => x.OperationalSituationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.IncidentEscalation)
            .WithMany()
            .HasForeignKey(x => x.IncidentEscalationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}