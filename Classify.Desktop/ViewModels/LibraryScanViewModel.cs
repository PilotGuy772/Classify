using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Service;
using Classify.Core.Enums;

namespace Classify.Desktop.ViewModels;

public record ScannedFileViewModel(string FileName, string Status);

public class LibraryScanViewModel : ViewModelBase, IDisposable
{
    private readonly IIngestionOrchestrationService _orchestration;
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

    public LibraryScanViewModel(IIngestionOrchestrationService orchestration)
    {
        _orchestration = orchestration;

        ScanLibraryCommand = new RelayCommand(o => _ = ScanLibraryAsync());

        // subscribe to orchestration events
        _orchestration.ScanStateChanged += OnScanStateChanged;

        // initialize properties from current service state
        OnScanStateChanged(_orchestration.State);

        // seed some example items (skeleton)
        ScannedFiles.Add(new ScannedFileViewModel("example1.mp3", "Added"));
        ScannedFiles.Add(new ScannedFileViewModel("example2.wav", "Awaiting Input"));
    }

    private void OnScanStateChanged(LibraryScanState state)
    {
        // map enum to display string
        CurrentState = state.ToString();
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
