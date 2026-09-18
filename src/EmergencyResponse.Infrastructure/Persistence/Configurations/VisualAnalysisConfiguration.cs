using System;
using System.Collections.Generic;
using System.Text;

using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class VisualAnalysisConfiguration 
    : IEntityTypeConfiguration<VisualAnalysis>
{
    public void Configure(EntityTypeBuilder<VisualAnalysis> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModelName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ModelVersion)
            .HasMaxLength(100);

        builder.Property(x => x.PromptVersion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.FindingsJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.RawOutputJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.HumanDecision)
            .HasMaxLength(2000);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(4000);

        builder.HasOne(x => x.IncidentMedia)
            .WithMany()
            .HasForeignKey(x => x.IncidentMediaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Incident)
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.IncidentMediaId);
        builder.HasIndex(x => x.IncidentId);
    }
}
