using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Enums;

namespace Classify.Core.Interfaces.Service;

public interface IIngestionOrchestrationService
{
    public LibraryScanState State { get; }

    public Task StartScanAsync(CancellationToken cancellationToken);
    public Task ProvideUserInputAsync(Dictionary<int, ProposedMatch>? match);

    public event Action<LibraryScanState>? ScanStateChanged;
    public event Action<string>? UserInputRequested;
}