using Classify.Core.Interfaces;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Classify.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ClassifyContext _context;
    
    // All repositories share the SAME DbContext instance

    
    public UnitOfWork(IDbContextFactory<ClassifyContext> contextFactory)
    {
        _context = contextFactory.CreateDbContext();

        Composers = new ComposerRepository(_context);
        Works = new WorkRepository(_context);
        Movements = new MovementRepository(_context);
        Recordings = new RecordingRepository(_context);
        AudioFiles = new AudioFileRepository(_context);
    }
    

    // Lazy initialization: create repository only when first accessed
    public IAudioFileRepository AudioFiles =>
        field ??= new AudioFileRepository(_context);

    public IComposerRepository Composers =>
        field ??= new ComposerRepository(_context);

    public IMovementRepository Movements =>
        field ??= new MovementRepository(_context);

    public IPerformedMovementRepository PerformedMovements =>
        field ??= new PerformedMovementRepository(_context);

    public IProposedMatchRepository ProposedMatch =>
        field ??= new ProposedMatchRepository(_context);

    public IRecordingRepository Recordings =>
        field ??= new RecordingRepository(_context);

    public IWorkRepository Works =>
        field ??= new WorkRepository(_context);

    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    public async Task RollbackAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}