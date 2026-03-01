using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class ProposedMatchViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    private readonly int _audioFileId;

    public string AudioFilePath { get; }

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

    public ProposedMatchViewModel(IUnitOfWork uow, int audioFileId, string audioFilePath)
    {
        _uow = uow;
        _audioFileId = audioFileId;
        AudioFilePath = audioFilePath;

        SubmitCommand = new RelayCommand(o => _ = SubmitAsync());
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
}
