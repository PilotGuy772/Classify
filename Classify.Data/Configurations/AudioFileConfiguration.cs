using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

public class AudioFileConfiguration : IEntityTypeConfiguration<AudioFile>
{
    public void Configure(EntityTypeBuilder<AudioFile> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Path)
            .IsRequired();

        builder.Property(a => a.Hash)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired();

        // builder.HasOne<Recording>()
        //     .WithMany()
        //     .HasForeignKey(a => a.RecordingId);
        //
        // builder.HasOne<Movement>()
        //     .WithMany()
        //     .HasForeignKey(a => a.MovementId);
    }
}