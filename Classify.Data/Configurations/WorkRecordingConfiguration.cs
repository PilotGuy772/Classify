using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

/// <summary>
/// Configures the EF Core database mappings for the <see cref="WorkRecording"/> entity.
/// </summary>
public class WorkRecordingConfiguration : IEntityTypeConfiguration<WorkRecording>
{
    /// <summary>
    /// Configures the database schema for the <see cref="WorkRecording"/> entity.
    /// </summary>
    /// <param name="builder">The builder to configure the entity.</param>
    public void Configure(EntityTypeBuilder<WorkRecording> builder)
    {
        builder.HasKey(wr => wr.Id);

        builder.HasOne<Work>()
            .WithMany()
            .HasForeignKey(wr => wr.WorkId);

        builder.HasOne<Recording>()
            .WithMany()
            .HasForeignKey(wr => wr.RecordingId);
    }
}
