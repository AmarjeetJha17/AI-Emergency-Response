using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class CopilotActionProposalConfiguration
    : IEntityTypeConfiguration<CopilotActionProposal>
{
    public void Configure(EntityTypeBuilder<CopilotActionProposal> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ArgumentsJson).IsRequired();
        builder.Property(x => x.ProposedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(450);
        builder.Property(x => x.ApprovalComment).HasMaxLength(1000);
        builder.Property(x => x.ExecutionResultJson).HasMaxLength(10000);
        builder.Property(x => x.FailureReason).HasMaxLength(2000);
        builder.Property(x => x.PolicySnapshotJson).HasMaxLength(10000);
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.CopilotInteractionId, x.ProposedAtUtc });
        builder.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
        builder.HasIndex(x => x.IncidentId);
        builder.HasIndex(x => x.SituationId);
        builder.HasIndex(x => x.ResourceId);
        builder.HasIndex(x => x.EscalationId);
        builder.HasIndex(x => x.CorrelationId);
    }
}
