using Classify.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Context;

/// <summary>
/// Represents the database context for the Classify application.
/// </summary>
public class ClassifyContext : DbContext
{
    /// <summary>
    /// Gets the DbSet for works.
    /// </summary>
    public DbSet<Work> Works => Set<Work>();

    /// <summary>
    /// Gets the DbSet for movements.
    /// </summary>
    public DbSet<Movement> Movements => Set<Movement>();

    /// <summary>
    /// Gets the DbSet for composers.
    /// </summary>
    public DbSet<Composer> Composers => Set<Composer>();

    /// <summary>
    /// Gets the DbSet for recordings.
    /// </summary>
    public DbSet<Recording> Recordings => Set<Recording>();

    /// <summary>
    /// Gets the DbSet for audio files.
    /// </summary>
    public DbSet<AudioFile> AudioFiles => Set<AudioFile>();

    /// <summary>
    /// Gets the DbSet for work-recording joins.
    /// </summary>
    public DbSet<WorkRecording> WorkRecordings => Set<WorkRecording>();

    /// <summary>
    /// Gets the DbSet for nibbles.
    /// </summary>
    public DbSet<Nibble> Nibbles => Set<Nibble>();

    /// <summary>
    /// Gets the DbSet for nibble movement joins.
    /// </summary>
    public DbSet<NibbleMovement> NibbleMovements => Set<NibbleMovement>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassifyContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public ClassifyContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassifyContext"/> class with default settings.
    /// </summary>
    public ClassifyContext() : this(new DbContextOptionsBuilder().UseSqlite("Data Source=library.db").Options)
    {
    }

    /// <summary>
    /// Configures the model properties, keys, and configurations.
    /// </summary>
    /// <param name="modelBuilder">The builder to configure model mappings.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClassifyContext).Assembly);
    }
}