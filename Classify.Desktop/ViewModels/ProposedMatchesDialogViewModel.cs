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

public class ProposedMatchesDialogViewModel : ViewModelBase, IDialog<int>
{
    private readonly IUnitOfWork _unitOfWork;
    private int _audioFileId;
    private readonly IDialogService _dialogService;

    public string AudioFilePath { get; private set; }
    public ObservableCollection<ProposedMatch> ProposedMatches { get; } = new();

    public ICommand AddMatchCommand { get; }
    public ICommand EditMatchCommand { get; }
    public ICommand AcceptMatchCommand { get; }
    public ICommand NonGenericEditMatchCommand { get; }
    public ICommand NonGenericAcceptMatchCommand { get; }
    public ICommand AddAndAcceptMatchCommand { get; }

    public ProposedMatchesDialogViewModel(IUnitOfWork unitOfWork, IIngestionOrchestrationService ingestionOrchestrationService, IDialogService dialogService)
    {
        _unitOfWork = unitOfWork;
        _dialogService = dialogService;

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
        //ProposedMatchViewModel proposedMatchViewModel = new(_unitOfWork, ingestionOrchestrationService, _audioFileId, AudioFilePath);
        // ProposedMatchDialog dialog = new()
        // {
        //     DataContext = proposedMatchViewModel
        // };
        await _dialogService.ShowDialogAsync<ProposedMatchDialogViewModel, int>(_audioFileId);

        // var mainWindow = ((Application)Application.Current)?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
        // if (mainWindow == null)
        // {
        //     throw new InvalidOperationException("MainWindow is not available.");
        // }

        // await dialog.ShowDialog(mainWindow);

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
        await _dialogService.ShowDialogAsync<ProposedMatchDialogViewModel, ProposedMatch>(match);

        // Refresh the list after editing
        IEnumerable<ProposedMatch> updatedMatches = await _unitOfWork.ProposedMatches.GetByAudioFileIdAsync(_audioFileId);
        ProposedMatches.Clear();
        foreach (ProposedMatch updatedMatch in updatedMatches)
        {
            ProposedMatches.Add(updatedMatch);
        }
    }

    public void Initialize(int afId)
    {
        _audioFileId = afId;
        AudioFile file = _unitOfWork.AudioFiles.GetByIdAsync(afId).Result!;
        AudioFilePath = file.Path;
        foreach (ProposedMatch match in _unitOfWork.ProposedMatches.GetByAudioFileIdAsync(afId).Result)
        {
            ProposedMatches.Add(match);
        }
    }
}