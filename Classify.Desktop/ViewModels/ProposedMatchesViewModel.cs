using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;
using Classify.Desktop.Views;

namespace Classify.Desktop.ViewModels;

public class ProposedMatchesViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly int _audioFileId;

    public string AudioFilePath { get; }
    public ObservableCollection<ProposedMatch> ProposedMatches { get; } = new();

    public ICommand AddMatchCommand { get; }
    public ICommand EditMatchCommand { get; }
    public ICommand AcceptMatchCommand { get; }
    public ICommand NonGenericEditMatchCommand { get; }
    public ICommand NonGenericAcceptMatchCommand { get; }

    public ProposedMatchesViewModel(IUnitOfWork unitOfWork, IIngestionOrchestrationService ingestionOrchestrationService, int audioFileId, string audioFilePath, IEnumerable<ProposedMatch> proposedMatches)
    {
        _unitOfWork = unitOfWork;
        _audioFileId = audioFileId;
        AudioFilePath = audioFilePath;

        foreach (var match in proposedMatches)
        {
            ProposedMatches.Add(match);
        }

        AddMatchCommand = new AsyncRelayCommand(AddProposedMatchAsync);
        EditMatchCommand = new AsyncRelayCommand<ProposedMatch>(async match => await EditProposedMatchAsync(match));
        AcceptMatchCommand = new AsyncRelayCommand<ProposedMatch>(async match =>
        {
            await ingestionOrchestrationService.AcceptProposedMatchAsync(match.Id, CancellationToken.None);
            match.Confirmed = true;
            await _unitOfWork.SaveChangesAsync();
        });

        NonGenericEditMatchCommand = new RelayCommand(param =>
        {
            if (param is ProposedMatch match)
            {
                EditMatchCommand.Execute(match);
            }
        });

        NonGenericAcceptMatchCommand = new RelayCommand(param =>
        {
            if (param is ProposedMatch match)
            {
                AcceptMatchCommand.Execute(match);
            }
        });
    }

    public async Task AddProposedMatchAsync()
    {
        ProposedMatchViewModel proposedMatchViewModel = new(_unitOfWork, _audioFileId, AudioFilePath);
        ProposedMatchDialog dialog = new()
        {
            DataContext = proposedMatchViewModel
        };

        var mainWindow = ((Application)Application.Current)?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
        if (mainWindow == null)
        {
            throw new InvalidOperationException("MainWindow is not available.");
        }

        await dialog.ShowDialog(mainWindow);

        // Refresh the list after adding a new match
        IEnumerable<ProposedMatch> updatedMatches = await _unitOfWork.ProposedMatches.GetByAudioFileIdAsync(_audioFileId);
        ProposedMatches.Clear();
        foreach (ProposedMatch match in updatedMatches)
        {
            ProposedMatches.Add(match);
        }
    }

    private async Task EditProposedMatchAsync(ProposedMatch match)
    {
        var proposedMatchViewModel = new ProposedMatchViewModel(_unitOfWork, _audioFileId, AudioFilePath)
        {
            ComposerName = match.ComposerName,
            WorkTitle = match.WorkTitle,
            CatalogNumber = match.CatalogNumber,
            ConductorName = match.ConductorName,
            MovementNumber = match.MovementNumber,
            MovementTitle = match.MovementTitle,
            PerformanceOrder = match.PerformanceOrder,
            Source = match.Source,
            ConfidenceScore = match.ConfidenceScore,
            MatchReasoning = match.MatchReasoning
        };

        var dialog = new ProposedMatchDialog
        {
            DataContext = proposedMatchViewModel
        };

        var mainWindow = ((Application)Application.Current)?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
        if (mainWindow == null)
        {
            throw new InvalidOperationException("MainWindow is not available.");
        }

        await dialog.ShowDialog(mainWindow);

        // Refresh the list after editing
        var updatedMatches = await _unitOfWork.ProposedMatches.GetByAudioFileIdAsync(_audioFileId);
        ProposedMatches.Clear();
        foreach (var updatedMatch in updatedMatches)
        {
            ProposedMatches.Add(updatedMatch);
        }
    }
}