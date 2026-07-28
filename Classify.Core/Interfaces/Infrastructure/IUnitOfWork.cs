using Classify.Core.Interfaces.Repository;

namespace Classify.Core.Interfaces.Infrastructure;

/// <summary>
/// Defines the contract for a unit of work that orchestrates repositories and manages transactions.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the repository for audio files.
    /// </summary>
    IAudioFileRepository AudioFiles { get; }

    /// <summary>
    /// Gets the repository for composers.
    /// </summary>
    IComposerRepository Composers { get; }

    /// <summary>
    /// Gets the repository for movements.
    /// </summary>
    IMovementRepository Movements { get; }

    /// <summary>
    /// Gets the repository for nibbles.
    /// </summary>
    INibbleRepository Nibbles { get; }

    /// <summary>
    /// Gets the repository for nibble movement joins.
    /// </summary>
    INibbleMovementRepository NibbleMovements { get; }

    /// <summary>
    /// Gets the repository for performed movements.
    /// </summary>
    IPerformedMovementRepository PerformedMovements { get; }

    /// <summary>
    /// Gets the repository for proposed matches.
    /// </summary>
    IProposedMatchRepository ProposedMatches { get; }

    /// <summary>
    /// Gets the repository for recordings.
    /// </summary>
    IRecordingRepository Recordings { get; }

    /// <summary>
    /// Gets the repository for work recording joins.
    /// </summary>
    IWorkRecordingRepository WorkRecordings { get; }

    /// <summary>
    /// Gets the repository for works.
    /// </summary>
    IWorkRepository Works { get; }

    /// <summary>
    /// Asynchronously saves all changes made in this unit of work to the database.
    /// </summary>
    /// <returns>A task representing the save operation, containing the number of state entries written to the database.</returns>
    public Task<int> SaveChangesAsync();

    /// <summary>
    /// Asynchronously starts a new database transaction.
    /// </summary>
    /// <returns>A task representing the start operation.</returns>
    public Task BeginTransactionAsync();

    /// <summary>
    /// Asynchronously commits the current database transaction.
    /// </summary>
    /// <returns>A task representing the commit operation.</returns>
    public Task CommitAsync();

    /// <summary>
    /// Asynchronously rolls back the current database transaction.
    /// </summary>
    /// <returns>A task representing the rollback operation.</returns>
    public Task RollbackAsync();
}