using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Enums;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;
using Microsoft.Extensions.Options;

namespace Classify.Services.Ingestion;

public class LibraryIngestionOrchestrationService(
    IIngestionService ingestionService,
    IUnitOfWork uow,
    IOptions<AppSettings> settings)
    : IIngestionOrchestrationService
{
    private readonly AppSettings _settings = settings.Value;

    public LibraryScanState State
    {
        get;
        private set
        {
            field = value;
            ScanStateChanged?.Invoke(value);
        }
    }

    public async Task StartScanAsync(CancellationToken cancellationToken)
    {
        State = LibraryScanState.Scanning;

        try
        {
            await ScanAsync(cancellationToken);
            State = LibraryScanState.Completed;
        }
        catch (OperationCanceledException)
        {
            State = LibraryScanState.Canceled;
        }
        catch (Exception)
        {
            State = LibraryScanState.Failed;
            throw;
        }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        // scan library files
        await ingestionService.ScanLibraryAsync(_settings.LibraryPath);
        ct.ThrowIfCancellationRequested();

        // After scanning, stop. ProposedMatch review/acceptance will be handled separately by callers
    }

    public async Task AcceptProposedMatchAsync(int proposedMatchId, CancellationToken ct)
    {
        // Load the ProposedMatch from repository
        ProposedMatch? result = await uow.ProposedMatch.GetByIdAsync(proposedMatchId);
        if (result is null)
            throw new InvalidOperationException($"ProposedMatch with id {proposedMatchId} not found.");

        await uow.BeginTransactionAsync();
        try
        {
            int afId = result.AudioFileId;

            int composerId, workId, movementId, recordingId;
            
            // Composer
            if (result.ComposerId is null)
            {
                Composer composer = new()
                {
                    Name = result.ComposerName ?? ""
                };
                composer = await uow.Composers.AddAsync(composer);
                composerId = composer.Id;
            }
            else
            {
                composerId = result.ComposerId ?? throw new NullReferenceException("How did we get here?");
            }
            
            // Work
            if (result.WorkId is null)
            {
                Work work = new()
                {
                    Name = result.WorkTitle ?? "",
                    CatalogNumber = result.CatalogNumber ?? "",
                    ComposerId = composerId
                };
                work = await uow.Works.AddAsync(work);
                workId = work.Id;
            }
            else
            {
                workId = result.WorkId ?? throw new NullReferenceException("How did we get here?");
            }
            
            // Movement
            if (result.MovementId is null)
            {
                Movement movement = new()
                {
                    Name = result.MovementTitle ?? "",
                    Order = result.MovementNumber ?? 0,
                    WorkId = workId
                };
                movement = await uow.Movements.AddAsync(movement);
                movementId = movement.Id;
            }
            else
            {
                movementId = result.MovementId ?? throw new NullReferenceException("How did we get here?");
            }
            
            // Recording
            if (result.RecordingId is null)
            {
                Recording recording = new()
                {
                    Conductor = result.ConductorName ?? "",
                    WorkId = workId
                };
                recording = await uow.Recordings.AddAsync(recording);
                recordingId = recording.Id;
            }
            else
            {
                recordingId = result.RecordingId ?? throw new NullReferenceException("How did we get here?");
            }
            
            // Join type
            PerformedMovement performedMovement = new()
            {
                AudioFileId = afId,
                Order = result.PerformanceOrder ?? 0,
                RecordingId = recordingId,
                MovementId = movementId
            };

            await uow.PerformedMovements.AddAsync(performedMovement);
            ct.ThrowIfCancellationRequested();

            // Mark as confirmed and update ProposedMatch
            result.Confirmed = true;
            uow.ProposedMatch.Update(result);

            await uow.SaveChangesAsync();
            await uow.CommitAsync();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    public event Action<LibraryScanState>? ScanStateChanged;
}