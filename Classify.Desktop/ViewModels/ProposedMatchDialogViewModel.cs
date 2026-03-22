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

    // selected entities from the search controls (non-generic object)
    private object? _selectedComposer;
    public object? SelectedComposer
    {
        get => _selectedComposer;
        set
        {
            if (_selectedComposer == value) return;
            _selectedComposer = value;
            RaisePropertyChanged();
        }
    }

    private object? _selectedWork;
    public object? SelectedWork
    {
        get => _selectedWork;
        set
        {
            if (_selectedWork == value) return;
            _selectedWork = value;
            RaisePropertyChanged();
        }
    }

    private object? _selectedMovement;
    public object? SelectedMovement
    {
        get => _selectedMovement;
        set
        {
            if (_selectedMovement == value) return;
            _selectedMovement = value;
            RaisePropertyChanged();
        }
    }

    private object? _selectedRecording;
    public object? SelectedRecording
    {
        get => _selectedRecording;
        set
        {
            if (_selectedRecording == value) return;
            _selectedRecording = value;
            RaisePropertyChanged();
        }
    }

    public ICommand SubmitCommand { get; }
    public ICommand AddAndAcceptMatchCommand { get; }

    // Added a property for CurrentProposedMatch to resolve the symbol.
    public ProposedMatch? CurrentProposedMatch { get; private set; }

    public Classify.Data.Services.ComposerSearchService ComposerSearchService { get; }
    public Classify.Data.Services.WorkSearchService WorkSearchService { get; }
    public Classify.Data.Services.MovementSearchService MovementSearchService { get; }
    public Classify.Data.Services.RecordingSearchService RecordingSearchService { get; }

    public ProposedMatchDialogViewModel(
        IUnitOfWork uow,
        IIngestionOrchestrationService ingestionOrchestrationService,
        Classify.Data.Services.ComposerSearchService composerSearchService,
        Classify.Data.Services.WorkSearchService workSearchService,
        Classify.Data.Services.MovementSearchService movementSearchService,
        Classify.Data.Services.RecordingSearchService recordingSearchService)
    {
        _uow = uow;
        _ingestionOrchestrationService = ingestionOrchestrationService;

        ComposerSearchService = composerSearchService;
        WorkSearchService = workSearchService;
        MovementSearchService = movementSearchService;
        RecordingSearchService = recordingSearchService;

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

        // apply selected entity FKs if available
        if (SelectedComposer is Composer sc)
            pm.ComposerId = sc.Id;
        if (SelectedWork is Work sw)
            pm.WorkId = sw.Id;
        if (SelectedMovement is Movement sm)
            pm.MovementId = sm.Id;
        if (SelectedRecording is Recording sr)
            pm.RecordingId = sr.Id;

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

        if (SelectedComposer is Composer sc)
            CurrentProposedMatch.ComposerId = sc.Id;
        if (SelectedWork is Work sw)
            CurrentProposedMatch.WorkId = sw.Id;
        if (SelectedMovement is Movement sm)
            CurrentProposedMatch.MovementId = sm.Id;
        if (SelectedRecording is Recording sr)
            CurrentProposedMatch.RecordingId = sr.Id;

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

        // If the ProposedMatch references existing entities, load them and set Selected* so controls show pills
        if (proposedMatch.ComposerId.HasValue)
        {
            var composer = _uow.Composers.GetByIdAsync(proposedMatch.ComposerId.Value).Result;
            if (composer != null) SelectedComposer = composer;
        }

        if (proposedMatch.WorkId.HasValue)
        {
            var work = _uow.Works.GetByIdAsync(proposedMatch.WorkId.Value).Result;
            if (work != null) SelectedWork = work;
        }

        if (proposedMatch.MovementId.HasValue)
        {
            var movement = _uow.Movements.GetByIdAsync(proposedMatch.MovementId.Value).Result;
            if (movement != null) SelectedMovement = movement;
        }

        if (proposedMatch.RecordingId.HasValue)
        {
            var recording = _uow.Recordings.GetByIdAsync(proposedMatch.RecordingId.Value).Result;
            if (recording != null) SelectedRecording = recording;
        }
    }

    public void Initialize(int audioFileId)
    {
        AudioFile af = _uow.AudioFiles.GetByIdAsync(audioFileId).Result!;
        _audioFileId = audioFileId;
        AudioFilePath = af.Path;
    }
}
