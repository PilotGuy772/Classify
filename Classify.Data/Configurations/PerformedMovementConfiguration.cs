using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

public class PerformedMovementConfiguration : IEntityTypeConfiguration<PerformedMovement>
{
    public void Configure(EntityTypeBuilder<PerformedMovement> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Order)
            .IsRequired();

        builder.HasOne<Movement>()
            .WithMany()
            .HasForeignKey(p => p.MovementId);

        builder.HasOne<Recording>()
            .WithMany()
            .HasForeignKey(p => p.RecordingId);
    }
}