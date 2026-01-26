using Microsoft.EntityFrameworkCore;
using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

public class WorkConfiguration : IEntityTypeConfiguration<Work>
{
    public void Configure(EntityTypeBuilder<Work> builder)
    {
        builder.HasKey(w => w.Id);
        
        // required fields
        builder.Property(w => w.Name)
            .IsRequired();
        
        // optional fields
        builder.Property(w => w.CatalogNumber)
            .IsRequired(false);

        builder.HasOne<Composer>()
            .WithMany()
            .HasForeignKey(w => w.ComposerId)
            .IsRequired();
    }
}