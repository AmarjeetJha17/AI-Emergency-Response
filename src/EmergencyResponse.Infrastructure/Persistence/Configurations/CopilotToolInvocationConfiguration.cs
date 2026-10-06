using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class CopilotToolInvocationConfiguration
    : IEntityTypeConfiguration<CopilotToolInvocation>
{
    public void Configure(EntityTypeBuilder<CopilotToolInvocation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ToolName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ArgumentsJson).IsRequired();
        builder.Property(x => x.ResultSummaryJson);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.HasIndex(x => x.CopilotInteractionId);
        builder.HasIndex(x => x.InvokedAtUtc);
        builder.HasOne(x => x.CopilotInteraction)
            .WithMany(x => x.ToolInvocations)
            .HasForeignKey(x => x.CopilotInteractionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
