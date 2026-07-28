using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Nibble"/> entity.
/// </summary>
public class NibbleConfiguration : IEntityTypeConfiguration<Nibble>
{
    /// <summary>
    /// Configures the database mapping for <see cref="Nibble"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Nibble> builder)
    {
        builder.HasKey(n => n.Id);

        builder.HasOne<Work>()
            .WithMany()
            .HasForeignKey(n => n.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Recording>()
            .WithMany()
            .HasForeignKey(n => n.RecordingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
