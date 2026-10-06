using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class CopilotMessageConfiguration
    : IEntityTypeConfiguration<CopilotMessage>
{
    public void Configure(EntityTypeBuilder<CopilotMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(200);
        builder.Property(x => x.ToolCallsJson);
        builder.Property(x => x.CitationsJson);
        builder.Property(x => x.GroundingStatus).HasMaxLength(100);
        builder.HasIndex(x => new
        {
            x.CopilotConversationId,
            x.SequenceNumber
        }).IsUnique();
        builder.HasOne(x => x.CopilotConversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.CopilotConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
