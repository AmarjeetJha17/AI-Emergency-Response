using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeDocumentConfiguration
    : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.IndexName).HasMaxLength(200);
        builder.Property(x => x.FailureReason).HasMaxLength(2000);
        builder.Property(x => x.SecurityRiskReason).HasMaxLength(2000);
        builder.Property(x => x.SecuritySignalsJson).HasMaxLength(4000);
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.HasIndex(x => new
        {
            x.Status,
            x.SecurityScanStatus,
            x.DepartmentId,
            x.IsGlobal
        });
        builder.HasIndex(x => new { x.ContentHash, x.Version });
    }
}
