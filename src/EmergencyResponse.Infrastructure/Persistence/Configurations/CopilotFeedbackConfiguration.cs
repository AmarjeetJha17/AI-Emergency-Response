using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class CopilotFeedbackConfiguration
    : IEntityTypeConfiguration<CopilotFeedback>
{
    public void Configure(EntityTypeBuilder<CopilotFeedback> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.HasIndex(x => x.CopilotMessageId);
        builder.HasOne(x => x.CopilotMessage)
            .WithMany()
            .HasForeignKey(x => x.CopilotMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
