using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

public class RecordingConfiguration : IEntityTypeConfiguration<Recording>
{
    public void Configure(EntityTypeBuilder<Recording> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Conductor)
            .IsRequired(false);

        // Each recording has many movements (subordinate to works),
        // and each movement has many recordings.
        builder.HasMany<Movement>()
            .WithMany();

        builder.HasOne<Work>()
            .WithMany()
            .HasForeignKey(r => r.WorkId);
    }
}