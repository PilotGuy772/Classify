using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Classify.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data;

/// <summary>
/// Implements the Unit of Work pattern to orchestrate repository access and transaction management.
/// </summary>
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ClassifyContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="contextFactory">The factory to create the database context.</param>
    public UnitOfWork(IDbContextFactory<ClassifyContext> contextFactory)
    {
        _context = contextFactory.CreateDbContext();

        Composers = new ComposerRepository(_context);
        Works = new WorkRepository(_context);
        Movements = new MovementRepository(_context);
        Recordings = new RecordingRepository(_context);
        AudioFiles = new AudioFileRepository(_context);
        WorkRecordings = new WorkRecordingRepository(_context);
    }

    /// <summary>
    /// Gets the audio files repository.
    /// </summary>
    public IAudioFileRepository AudioFiles =>
        field ??= new AudioFileRepository(_context);

    /// <summary>
    /// Gets the composers repository.
    /// </summary>
    public IComposerRepository Composers =>
        field ??= new ComposerRepository(_context);

    /// <summary>
    /// Gets the movements repository.
    /// </summary>
    public IMovementRepository Movements =>
        field ??= new MovementRepository(_context);

    /// <summary>
    /// Gets the nibbles repository.
    /// </summary>
    public INibbleRepository Nibbles =>
        field ??= new NibbleRepository(_context);

    /// <summary>
    /// Gets the nibble movements repository.
    /// </summary>
    public INibbleMovementRepository NibbleMovements =>
        field ??= new NibbleMovementRepository(_context);

    /// <summary>
    /// Gets the performed movements repository.
    /// </summary>
    public IPerformedMovementRepository PerformedMovements =>
        field ??= new PerformedMovementRepository(_context);

    /// <summary>
    /// Gets the proposed matches repository.
    /// </summary>
    public IProposedMatchRepository ProposedMatches =>
        field ??= new ProposedMatchRepository(_context);

    /// <summary>
    /// Gets the recordings repository.
    /// </summary>
    public IRecordingRepository Recordings =>
        field ??= new RecordingRepository(_context);

    /// <summary>
    /// Gets the work recordings repository.
    /// </summary>
    public IWorkRecordingRepository WorkRecordings =>
        field ??= new WorkRecordingRepository(_context);

    /// <summary>
    /// Gets the works repository.
    /// </summary>
    public IWorkRepository Works =>
        field ??= new WorkRepository(_context);

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Starts a new database transaction.
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Commits the active database transaction.
    /// </summary>
    public async Task CommitAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    /// <summary>
    /// Rolls back the active database transaction.
    /// </summary>
    public async Task RollbackAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }

    /// <summary>
    /// Disposes the database context.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the database context.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}