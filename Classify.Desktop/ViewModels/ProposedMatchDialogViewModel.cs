using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Desktop.ViewModels;

public class ProposedMatchDialogViewModel : ViewModelBase, IDialog<ProposedMatch>, IDialog<int>
{
    private readonly IUnitOfWork _uow;
    private readonly IIngestionOrchestrationService _ingestionOrchestrationService;
    private int _audioFileId;

    public string AudioFilePath { get; private set; }

    // free-entry fields only
    public string? ComposerName { get; set; }
    public string? WorkTitle { get; set; }
    public string? CatalogNumber { get; set; }
    public string? ConductorName { get; set; }
    public int? MovementNumber { get; set; }
    public string? MovementTitle { get; set; }
    public int? PerformanceOrder { get; set; }
    public string Source { get; set; } = "Manual";
    public float ConfidenceScore { get; set; } = 0.0f;
    public string? MatchReasoning { get; set; }

    public ICommand SubmitCommand { get; }
    public ICommand AddAndAcceptMatchCommand { get; }

    // Added a property for CurrentProposedMatch to resolve the symbol.
    public ProposedMatch? CurrentProposedMatch { get; private set; }

    public ProposedMatchDialogViewModel(IUnitOfWork uow, IIngestionOrchestrationService ingestionOrchestrationService)
    {
        _uow = uow;
        _ingestionOrchestrationService = ingestionOrchestrationService;

        SubmitCommand = new RelayCommand(o => _ = SubmitAsync());

        AddAndAcceptMatchCommand = new AsyncRelayCommand(async () =>
        {
            await SubmitProposedMatchAsync();
            if (CurrentProposedMatch != null)
            {
                await _ingestionOrchestrationService.AcceptProposedMatchAsync(CurrentProposedMatch.Id, CancellationToken.None);
            }
        });
    }

    private async Task SubmitAsync()
    {
        ProposedMatch pm = new()
        {
            AudioFileId = _audioFileId,
            ComposerName = ComposerName,
            WorkTitle = WorkTitle,
            CatalogNumber = CatalogNumber,
            ConductorName = ConductorName,
            MovementNumber = MovementNumber,
            MovementTitle = MovementTitle,
            PerformanceOrder = PerformanceOrder,
            Source = Source,
            ConfidenceScore = ConfidenceScore,
            MatchReasoning = MatchReasoning,
            Confirmed = false
        };

        await _uow.ProposedMatches.AddAsync(pm);
        await _uow.SaveChangesAsync();
    }

    private async Task SubmitProposedMatchAsync()
    {
        CurrentProposedMatch = new ProposedMatch
        {
            AudioFileId = _audioFileId,
            ComposerName = ComposerName,
            WorkTitle = WorkTitle,
            CatalogNumber = CatalogNumber,
            ConductorName = ConductorName,
            MovementNumber = MovementNumber,
            MovementTitle = MovementTitle,
            PerformanceOrder = PerformanceOrder,
            Source = Source,
            ConfidenceScore = ConfidenceScore,
            MatchReasoning = MatchReasoning,
            Confirmed = false
        };

        await _uow.ProposedMatches.AddAsync(CurrentProposedMatch);
        await _uow.SaveChangesAsync();
    }

    public void Initialize(ProposedMatch proposedMatch)
    {
        Initialize(proposedMatch.AudioFileId);
        
        ComposerName = proposedMatch.ComposerName;
        WorkTitle = proposedMatch.WorkTitle;
        CatalogNumber = proposedMatch.CatalogNumber;
        ConductorName = proposedMatch.ConductorName;
        MovementNumber = proposedMatch.MovementNumber;
        MovementTitle = proposedMatch.MovementTitle;
        PerformanceOrder = proposedMatch.PerformanceOrder;
        Source = proposedMatch.Source;
        ConfidenceScore = proposedMatch.ConfidenceScore;
        MatchReasoning = proposedMatch.MatchReasoning;
    }

    public void Initialize(int audioFileId)
    {
        AudioFile af = _uow.AudioFiles.GetByIdAsync(audioFileId).Result!;
        _audioFileId = audioFileId;
        AudioFilePath = af.Path;
    }
}
