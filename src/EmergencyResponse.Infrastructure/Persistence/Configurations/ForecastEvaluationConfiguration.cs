using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class ForecastEvaluationConfiguration
    : IEntityTypeConfiguration<ForecastEvaluation>
{
    public void Configure(EntityTypeBuilder<ForecastEvaluation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DetailsJson).IsRequired();
        builder.Property(x => x.MAE).HasPrecision(18, 4);
        builder.Property(x => x.RMSE).HasPrecision(18, 4);

        builder.HasOne(x => x.OperationalForecast)
            .WithMany()
            .HasForeignKey(x => x.OperationalForecastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.OperationalForecastId,
            x.EvaluationAtUtc
        });
    }
}
