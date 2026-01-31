using Classify.Core.Interfaces.Repository;

namespace Classify.Core.Interfaces.Infrastructure;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    // all repositories
    IAudioFileRepository         AudioFiles         { get; }
    IComposerRepository          Composers          { get; }
    IMovementRepository          Movements          { get; }
    IPerformedMovementRepository PerformedMovements { get; }
    IProposedMatchRepository     ProposedMatch      { get; }
    IRecordingRepository         Recordings         { get; }
    IWorkRepository              Works              { get; }

    public Task<int> SaveChangesAsync();
    public Task BeginTransactionAsync();
    public Task CommitAsync();
    public Task RollbackAsync();
}