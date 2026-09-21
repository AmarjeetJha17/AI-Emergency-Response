using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class ResourceConfiguration
    : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Latitude)
            .HasPrecision(9, 6);

        builder.Property(x => x.Longitude)
            .HasPrecision(9, 6);

        builder.Property(x => x.LocationUpdatedAtUtc);

        builder.Property(x => x.ShowOnOperationalMap)
            .IsRequired();

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.Type);

        builder.HasIndex(x => new
        {
            x.Latitude,
            x.Longitude
        });

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Assignments)
            .WithOne(x => x.Resource)
            .HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}