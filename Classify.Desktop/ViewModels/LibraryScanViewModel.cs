using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Service;
using Classify.Core.Enums;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Desktop.Views;

namespace Classify.Desktop.ViewModels;

public record ScannedFileViewModel(int Id, string FileName, string Status);

public class LibraryScanViewModel : ViewModelBase, IDisposable
{
    private readonly IIngestionOrchestrationService _orchestration;
    private readonly IUnitOfWork _unitOfWork;
    private CancellationTokenSource? _cts;

    public ObservableCollection<ScannedFileViewModel> ScannedFiles { get; } = new();

    public ICommand ScanLibraryCommand { get; }

    public string CurrentState
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            RaisePropertyChanged();
        }
    } = "Idle";

    public LibraryScanViewModel(IIngestionOrchestrationService orchestration, IUnitOfWork unitOfWork)
    {
        _orchestration = orchestration;
        _unitOfWork = unitOfWork;

        ScanLibraryCommand = new RelayCommand(o => _ = ScanLibraryAsync());

        // subscribe to orchestration events
        _orchestration.ScanStateChanged += OnScanStateChanged;

        // initialize properties from current service state
        OnScanStateChanged(_orchestration.State);

        _ = LoadIncompleteAudioFilesAsync();
    }

    private void OnScanStateChanged(LibraryScanState state)
    {
        // map enum to display string
        CurrentState = state.ToString();

        if (state == LibraryScanState.Completed)
        {
            // fire-and-forget load; UI will update when complete
            _ = LoadIncompleteAudioFilesAsync();
        }
    }

    public async Task LoadIncompleteAudioFilesAsync()
    {
        try
        {
            IEnumerable<AudioFile> all = await _unitOfWork.AudioFiles.GetAllAsync();
            IEnumerable<AudioFile> incomplete = all.Where(a => a.Status != IngestionStatus.Complete);

            ScannedFiles.Clear();
            foreach (AudioFile a in incomplete)
            {
                ScannedFiles.Add(new ScannedFileViewModel(a.Id, a.Path, a.Status.ToString()));
            }

            // ObservableCollection changed; UI updates automatically
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    private async Task ScanLibraryAsync()
    {
        Console.WriteLine("Scanning library.");
        //await _cts?.CancelAsync()!;
        _cts = new CancellationTokenSource();
        try
        {
            await _orchestration.StartScanAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // user cancelled
        }
        catch (Exception e)
        {
            // log or show error - omitted here
            Console.Error.WriteLine(e);
            throw;
        }
    }

    public async Task FileItemDoubleTappedAsync(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: ScannedFileViewModel item }) return;

        ProposedMatchViewModel pmVm = new (_unitOfWork, item.Id, item.FileName);
        ProposedMatchDialog dialog = new()
        {
            DataContext = pmVm
        };

        // Show the dialog and refresh the list after it closes
        await dialog.ShowDialog((Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null) ?? throw new InvalidOperationException());
        await LoadIncompleteAudioFilesAsync();
    }

    public async Task OpenProposedMatchesDialogAsync(int audioFileId, string audioFilePath)
    {
        IEnumerable<ProposedMatch> proposedMatches = await _unitOfWork.ProposedMatches.GetByAudioFileIdAsync(audioFileId);
        ProposedMatchesViewModel viewModel = new(_unitOfWork, audioFileId, audioFilePath, proposedMatches);

        ProposedMatchesDialog dialog = new()
        {
            DataContext = viewModel
        };

        await dialog.ShowDialog(App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
    }

    public void Dispose()
    {
        if (_orchestration is not null)
        {
            _orchestration.ScanStateChanged -= OnScanStateChanged;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
