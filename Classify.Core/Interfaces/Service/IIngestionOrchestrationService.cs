using Classify.Core.Enums;

namespace Classify.Core.Interfaces.Service;

public interface IIngestionOrchestrationService
{
    public LibraryScanState State { get; }

    public Task StartScanAsync(CancellationToken cancellationToken);

    // accept a proposed match by ID (service will load the ProposedMatch from DB and persist changes)
    public Task AcceptProposedMatchAsync(int proposedMatchId, CancellationToken cancellationToken);

    public event Action<LibraryScanState>? ScanStateChanged;
}