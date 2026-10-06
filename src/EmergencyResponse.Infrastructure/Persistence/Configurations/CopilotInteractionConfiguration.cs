using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class CopilotInteractionConfiguration
    : IEntityTypeConfiguration<CopilotInteraction>
{
    public void Configure(EntityTypeBuilder<CopilotInteraction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Question).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ModelVersion).HasMaxLength(100);
        builder.Property(x => x.PromptVersion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Answer).IsRequired();
        builder.Property(x => x.RetrievedDocumentIdsJson).IsRequired();
        builder.Property(x => x.CitationsJson).IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
