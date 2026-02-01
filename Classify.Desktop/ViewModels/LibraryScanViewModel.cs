using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Classify.Desktop.ViewModels;

public record ScannedFileViewModel(string FileName, string Status);

public class LibraryScanViewModel : ViewModelBase
{
    public ObservableCollection<ScannedFileViewModel> ScannedFiles { get; } = new();

    public ICommand ScanLibraryCommand { get; }

    public LibraryScanViewModel()
    {
        ScanLibraryCommand = new RelayCommand(_ => ScanLibrary());

        // seed some example items (skeleton)
        ScannedFiles.Add(new ScannedFileViewModel("example1.mp3", "Added"));
        ScannedFiles.Add(new ScannedFileViewModel("example2.wav", "Awaiting Input"));
    }

    private void ScanLibrary()
    {
        // TODO: implement scanning logic. For now, stub that toggles statuses or adds a placeholder.
        ScannedFiles.Add(new ScannedFileViewModel($"scanned_{ScannedFiles.Count + 1}.mp3", "Awaiting Input"));
        RaisePropertyChanged(nameof(ScannedFiles));
    }
}
