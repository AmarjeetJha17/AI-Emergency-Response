using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class CopilotConversationConfiguration
    : IEntityTypeConfiguration<CopilotConversation>
{
    public void Configure(EntityTypeBuilder<CopilotConversation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(4000);
        builder.HasIndex(x => new { x.UserId, x.LastActivityAtUtc });
        builder.HasIndex(x => new { x.IncidentId, x.LastActivityAtUtc });
        builder.HasIndex(x => new { x.SituationId, x.LastActivityAtUtc });
    }
}
