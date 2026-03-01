using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Desktop.Views;

namespace Classify.Desktop.ViewModels;

public class ProposedMatchesViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly int _audioFileId;

    public string AudioFilePath { get; }
    public ObservableCollection<ProposedMatch> ProposedMatches { get; } = new();

    public ICommand AddMatchCommand { get; }

    public ProposedMatchesViewModel(IUnitOfWork unitOfWork, int audioFileId, string audioFilePath, IEnumerable<ProposedMatch> proposedMatches)
    {
        _unitOfWork = unitOfWork;
        _audioFileId = audioFileId;
        AudioFilePath = audioFilePath;

        foreach (ProposedMatch match in proposedMatches)
        {
            ProposedMatches.Add(match);
        }

        AddMatchCommand = new AsyncRelayCommand(AddProposedMatchAsync);
    }

    public async Task AddProposedMatchAsync()
    {
        ProposedMatchViewModel proposedMatchViewModel = new(_unitOfWork, _audioFileId, AudioFilePath);
        ProposedMatchDialog dialog = new()
        {
            DataContext = proposedMatchViewModel
        };

        await dialog.ShowDialog(App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);

        // Refresh the list after adding a new match
        IEnumerable<ProposedMatch> updatedMatches = await _unitOfWork.ProposedMatches.GetByAudioFileIdAsync(_audioFileId);
        ProposedMatches.Clear();
        foreach (ProposedMatch match in updatedMatches)
        {
            ProposedMatches.Add(match);
        }
    }
}
