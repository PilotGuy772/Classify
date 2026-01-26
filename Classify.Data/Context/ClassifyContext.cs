using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Context;

public class ClassifyContext(DbContextOptions<ClassifyContext> options) : DbContext(options)
{
    public DbSet<Work> Works => Set<Work>();
    public DbSet<Movement> Movements => Set<Movement>();
    public DbSet<Composer> Composers => Set<Composer>();
    public DbSet<Recording> Recordings => Set<Recording>();
    public DbSet<AudioFile> AudioFiles => Set<AudioFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClassifyContext).Assembly);
    }
}