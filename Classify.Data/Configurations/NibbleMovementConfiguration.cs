using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="NibbleMovement"/> entity.
/// </summary>
public class NibbleMovementConfiguration : IEntityTypeConfiguration<NibbleMovement>
{
    /// <summary>
    /// Configures the database mapping for <see cref="NibbleMovement"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<NibbleMovement> builder)
    {
        builder.HasKey(nm => nm.Id);

        builder.HasOne<Nibble>()
            .WithMany()
            .HasForeignKey(nm => nm.NibbleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Movement>()
            .WithMany()
            .HasForeignKey(nm => nm.MovementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(nm => new { nm.NibbleId, nm.Order });
    }
}
