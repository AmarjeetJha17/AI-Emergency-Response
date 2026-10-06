using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeDocumentChunkConfiguration
    : IEntityTypeConfiguration<KnowledgeDocumentChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocumentChunk> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChunkHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SearchDocumentId).HasMaxLength(300).IsRequired();
        builder.Property(x => x.SectionHeading).HasMaxLength(500);
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => new { x.KnowledgeDocumentId, x.ChunkNumber }).IsUnique();
        builder.HasIndex(x => x.ChunkHash);
        builder.HasIndex(x => x.SearchDocumentId).IsUnique();
        builder.HasOne<KnowledgeDocument>()
            .WithMany()
            .HasForeignKey(x => x.KnowledgeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
