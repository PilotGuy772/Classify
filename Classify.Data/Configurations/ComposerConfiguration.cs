using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

public class ComposerConfiguration : IEntityTypeConfiguration<Composer>
{
    public void Configure(EntityTypeBuilder<Composer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired();
    }
}