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
    /// Gets the conductor or soloist artist name.
    /// </summary>
    public string ConductorOrArtist { get; }

    /// <summary>
    /// Gets the ensemble name.
    /// </summary>
    public string EnsembleName { get; }

    /// <summary>
    /// Gets the recording year line.
    /// </summary>
    public string YearLine { get; }

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
    /// Gets the command to show this recording's info panel.
    /// </summary>
    public ICommand ShowRecordingCommand { get; }

    /// <summary>
    /// Creates a recordings section row binding model with commands targeting the owning panel.
    /// </summary>
    public RecordingInfoRowViewModel(string displayInfo, int recordingId, bool isFavorite, WorkInfoPanelViewModel panel)
    {
        DisplayInfo = displayInfo;
        RecordingId = recordingId;
        IsFavorite = isFavorite;

        string[] parts = displayInfo.Split(" \u00B7 ", System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4)
        {
            ConductorOrArtist = parts[0] + " / " + parts[1];
            EnsembleName = parts[2];
            YearLine = parts[3];
        }
        else if (parts.Length == 3)
        {
            ConductorOrArtist = parts[0];
            EnsembleName = parts[1];
            YearLine = parts[2];
        }
        else if (parts.Length == 2)
        {
            ConductorOrArtist = parts[0];
            EnsembleName = parts[1];
            YearLine = string.Empty;
        }
        else if (parts.Length == 1)
        {
            ConductorOrArtist = parts[0];
            EnsembleName = string.Empty;
            YearLine = string.Empty;
        }
        else
        {
            ConductorOrArtist = string.Empty;
            EnsembleName = string.Empty;
            YearLine = string.Empty;
        }

        PlayRecordingRowCommand = new AsyncRelayCommand(() => panel.PlayRecordingStubAsync(this));
        EnqueueRecordingRowCommand = new AsyncRelayCommand(() => panel.EnqueueRecordingStubAsync(this));
        ToggleFavoriteRecordingRowCommand = new AsyncRelayCommand(() => panel.ToggleFavoriteRecordingStubAsync(this));
        ShowRecordingCommand = new AsyncRelayCommand(() => panel.OpenInfoPanelAsync(LibraryItemType.Recording, recordingId));

        MenuOptions.Clear();
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Now",
            Icon = TablerIcons.Icons.IconPlayerPlay,
            Command = PlayRecordingRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Next",
            Icon = TablerIcons.Icons.IconCornerUpLeft,
            Command = new AsyncRelayCommand(() => panel.PlayRecordingRowNextStubAsync(this))
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Enqueue",
            Icon = TablerIcons.Icons.IconCornerDownLeft,
            Command = EnqueueRecordingRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Make Default",
            Icon = TablerIcons.Icons.IconStar,
            Command = ToggleFavoriteRecordingRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Favorite",
            Icon = TablerIcons.Icons.IconHeart,
            Command = new AsyncRelayCommand(() => panel.FavoriteRecordingRowStubAsync(this))
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Manage Playlists",
            Icon = TablerIcons.Icons.IconPlaylist,
            Command = new AsyncRelayCommand(() => panel.ManagePlaylistsRecordingRowStubAsync(this))
        });
    }
}
