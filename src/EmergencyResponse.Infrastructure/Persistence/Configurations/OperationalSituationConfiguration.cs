using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class OperationalSituationConfiguration
    : IEntityTypeConfiguration<OperationalSituation>
{
    public void Configure(
        EntityTypeBuilder<OperationalSituation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PortfolioDemoOwnershipKey)
            .HasMaxLength(100);

        builder.Property(x => x.SituationNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(4000);

        builder.Property(x => x.Level)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.SituationScore)
            .HasColumnType("float");

        builder.HasIndex(x => x.SituationNumber)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.Status,
            x.Level,
            x.CreatedAtUtc
        });
    }
}
