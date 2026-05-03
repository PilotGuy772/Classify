using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// One row under the Info Panel "Recordings" section.
/// </summary>
public sealed class RecordingInfoRowViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the display information for the recording (Soloist, Conductor, Ensemble, Year).
    /// </summary>
    public string DisplayInfo { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this recording is the favorite for the current work.
    /// </summary>
    public bool IsFavorite
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Domain recording id used by row-level commands.
    /// </summary>
    public int RecordingId { get; }

    /// <summary>
    /// Row-level play control (wired to panel stubs).
    /// </summary>
    public ICommand PlayRecordingRowCommand { get; }

    /// <summary>
    /// Row-level enqueue control (wired to panel stubs).
    /// </summary>
    public ICommand EnqueueRecordingRowCommand { get; }

    /// <summary>
    /// Row-level favorite toggle (wired to panel stubs).
    /// </summary>
    public ICommand ToggleFavoriteRecordingRowCommand { get; }

    /// <summary>
    /// Creates a recordings section row binding model with commands targeting the owning panel.
    /// </summary>
    public RecordingInfoRowViewModel(string displayInfo, int recordingId, bool isFavorite, WorkInfoPanelViewModel panel)
    {
        DisplayInfo = displayInfo;
        RecordingId = recordingId;
        IsFavorite = isFavorite;
        PlayRecordingRowCommand = new AsyncRelayCommand(() => panel.PlayRecordingStubAsync(this));
        EnqueueRecordingRowCommand = new AsyncRelayCommand(() => panel.EnqueueRecordingStubAsync(this));
        ToggleFavoriteRecordingRowCommand = new AsyncRelayCommand(() => panel.ToggleFavoriteRecordingStubAsync(this));
    }
}
