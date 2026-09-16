using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class IncidentConfiguration
    : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IncidentNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Severity)
            .IsRequired();

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.ReportedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.IncidentNumber)
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.Severity);

        builder.HasIndex(x => x.Priority);

        builder.HasIndex(x => x.ReportedAtUtc);

        builder.HasOne(x => x.IncidentType)
            .WithMany(x => x.Incidents)
            .HasForeignKey(x => x.IncidentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Incidents)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Locations)
            .WithOne(x => x.Incident)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Media)
            .WithOne(x => x.Incident)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Assignments)
            .WithOne(x => x.Incident)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StatusHistory)
            .WithOne(x => x.Incident)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AIAnalyses)
            .WithOne(x => x.Incident)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.SeverityScore)
            .IsRequired();

        builder.Property(x => x.PriorityScore)
            .IsRequired();

        builder.Property(x => x.SeverityReasonsJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.PriorityReasonsJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1536)");

        builder.Property(x => x.EmbeddingModel)
            .HasMaxLength(100);

        builder.Property(x => x.EmbeddingGeneratedAtUtc);

        builder.HasOne(x => x.CanonicalIncident)
            .WithMany()
            .HasForeignKey(x => x.CanonicalIncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CanonicalIncidentId);
    }
}