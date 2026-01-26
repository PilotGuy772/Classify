using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

public class ProposedMatchConfiguration : IEntityTypeConfiguration<ProposedMatch>
{
    public void Configure(EntityTypeBuilder<ProposedMatch> builder)
    {
        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.ComposerName)
            .IsRequired(false);
        builder.Property(pm => pm.WorkTitle)
            .IsRequired(false);
        builder.Property(pm => pm.CatalogNumber)
            .IsRequired(false);
        builder.Property(pm => pm.MovementNumber)
            .IsRequired(false);
        builder.Property(pm => pm.MovementTitle)
            .IsRequired(false);
        builder.Property(pm => pm.Source)
            .IsRequired();
        builder.Property(pm => pm.ExternalId)
            .IsRequired(false);
        builder.Property(pm => pm.ConfidenceScore)
            .IsRequired();
        builder.Property(pm => pm.MatchReasoning)
            .IsRequired(false);
        builder.Property(pm => pm.Confirmed)
            .IsRequired();

        builder.HasOne<AudioFile>()
            .WithMany()
            .HasForeignKey(pm => pm.AudioFileId);
    }
}