using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class OperationalForecastConfiguration
    : IEntityTypeConfiguration<OperationalForecast>
{
    public void Configure(EntityTypeBuilder<OperationalForecast> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SeriesKey)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.ModelName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ModelVersion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FeatureSchemaVersion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ForecastJson).IsRequired();
        builder.Property(x => x.LowerBoundJson);
        builder.Property(x => x.UpperBoundJson);
        builder.Property(x => x.MAE).HasPrecision(18, 4);
        builder.Property(x => x.RMSE).HasPrecision(18, 4);

        builder.HasIndex(x => new
        {
            x.ForecastType,
            x.SeriesKey,
            x.ForecastGeneratedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.ForecastType,
            x.SeriesKey,
            x.ForecastStartAtUtc
        });
    }
}
