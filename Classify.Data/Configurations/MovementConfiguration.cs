using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classify.Data.Configurations;

public class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
    public void Configure(EntityTypeBuilder<Movement> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired();

        builder.Property(m => m.Order)
            .IsRequired();

        builder.HasOne<Work>()
            .WithMany()
            .HasForeignKey(m => m.WorkId);
        
        
    }
}