using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Right-side Info Panel content for a selected library movement.
/// </summary>
public sealed class MovementInfoPanelViewModel : InfoPanelViewModelBase
{
    private string composerName = string.Empty;
    private string workName = string.Empty;
    private int parentWorkId;
    private int parentComposerId;

    /// <summary>
    /// Gets the parent composer name.
    /// </summary>
    public string ComposerName
    {
        get => composerName;
        private set
        {
            if (composerName == value) return;
            composerName = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the parent work name.
    /// </summary>
    public string WorkName
    {
        get => workName;
        private set
        {
            if (workName == value) return;
            workName = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the command to play the parent work.
    /// </summary>
    public ICommand PlayWorkCommand { get; }

    /// <summary>
    /// Gets the command to enqueue the parent work.
    /// </summary>
    public ICommand EnqueueWorkCommand { get; }

    /// <summary>
    /// Gets the command to show the parent composer's info panel.
    /// </summary>
    public ICommand ShowComposerCommand { get; }

    /// <summary>
    /// Gets the command to show the parent work's info panel.
    /// </summary>
    public ICommand ShowWorkCommand { get; }

    /// <summary>
    /// Gets the collection of movement recording rows.
    /// </summary>
    public ObservableCollection<MovementRecordingRowViewModel> RecordingRows { get; } = new();

    /// <summary>
    /// Initializes a new instance of <see cref="MovementInfoPanelViewModel"/> with direct database access.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    public MovementInfoPanelViewModel(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        PlayWorkCommand = new AsyncRelayCommand(PlayWorkAsync);
        EnqueueWorkCommand = new AsyncRelayCommand(EnqueueWorkAsync);
        ShowComposerCommand = new AsyncRelayCommand(() => OpenInfoPanelAsync(LibraryItemType.Composer, parentComposerId));
        ShowWorkCommand = new AsyncRelayCommand(() => OpenInfoPanelAsync(LibraryItemType.Work, parentWorkId));

        MenuOptions.Clear();
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Now",
            Icon = TablerIcons.Icons.IconPlayerPlay,
            Command = PlayWorkCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Next",
            Icon = TablerIcons.Icons.IconCornerUpLeft,
            Command = new AsyncRelayCommand(PlayWorkNextStubAsync)
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Enqueue",
            Icon = TablerIcons.Icons.IconCornerDownLeft,
            Command = EnqueueWorkCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Favorite",
            Icon = TablerIcons.Icons.IconHeart,
            Command = new AsyncRelayCommand(FavoriteWorkStubAsync)
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Manage Playlists",
            Icon = TablerIcons.Icons.IconPlaylist,
            Command = new AsyncRelayCommand(ManagePlaylistsWorkStubAsync)
        });
    }

    /// <summary>
    /// Loads parent work details, composer name, and movement recordings.
    /// </summary>
    /// <param name="movementId">The movement identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task LoadAsync(int movementId)
    {
        RecordingRows.Clear();

        Movement? movement = await unitOfWork.Movements.GetByIdAsync(movementId);
        if (movement is null)
        {
            Title = string.Empty;
            ComposerName = string.Empty;
            WorkName = string.Empty;
            parentWorkId = 0;
            parentComposerId = 0;
            return;
        }

        Title = movement.Name;

        Work? work = await unitOfWork.Works.GetByIdAsync(movement.WorkId);
        if (work is not null)
        {
            parentWorkId = work.Id;
            WorkName = work.Name;

            Composer? composer = await unitOfWork.Composers.GetByIdAsync(work.ComposerId);
            ComposerName = composer?.Name ?? "—";
            parentComposerId = composer?.Id ?? 0;
        }
        else
        {
            parentWorkId = 0;
            parentComposerId = 0;
            WorkName = "—";
            ComposerName = "—";
        }

        IEnumerable<PerformedMovement> pms = await unitOfWork.PerformedMovements.GetByMovementId(movementId);
        List<MovementRecordingRowViewModel> rows = new();

        foreach (PerformedMovement pm in pms)
        {
            Recording? rec = await unitOfWork.Recordings.GetByIdAsync(pm.RecordingId);
            if (rec is not null)
            {
                bool isFavorite = work is not null && rec.Id == work.FavoriteRecordingId;
                rows.Add(new MovementRecordingRowViewModel(rec.Name, rec.Id, pm.Id, isFavorite, this));
            }
        }

        // Sort by favorite first, then alphabetically
        foreach (MovementRecordingRowViewModel row in rows.OrderByDescending(r => r.IsFavorite).ThenBy(r => r.DisplayText))
        {
            RecordingRows.Add(row);
        }
    }

    private Task PlayWorkAsync()
    {
        return Task.CompletedTask;
    }

    private Task EnqueueWorkAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by recording row Play buttons (stub).
    /// </summary>
    internal Task PlayRecordingStubAsync(MovementRecordingRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by recording row Enqueue buttons (stub).
    /// </summary>
    internal Task EnqueueRecordingStubAsync(MovementRecordingRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by parent work Play Next options menu (stub).
    /// </summary>
    internal Task PlayWorkNextStubAsync() => Task.CompletedTask;

    /// <summary>
    /// Invoked by parent work Favorite options menu (stub).
    /// </summary>
    internal Task FavoriteWorkStubAsync() => Task.CompletedTask;

    /// <summary>
    /// Invoked by parent work Manage Playlists options menu (stub).
    /// </summary>
    internal Task ManagePlaylistsWorkStubAsync() => Task.CompletedTask;

    /// <summary>
    /// Invoked by recording row Play Next options menu (stub).
    /// </summary>
    internal Task PlayRecordingNextStubAsync(MovementRecordingRowViewModel row) => Task.CompletedTask;

    /// <summary>
    /// Invoked by recording row Favorite options menu (stub).
    /// </summary>
    internal Task FavoriteRecordingStubAsync(MovementRecordingRowViewModel row) => Task.CompletedTask;

    /// <summary>
    /// Invoked by recording row Manage Playlists options menu (stub).
    /// </summary>
    internal Task ManagePlaylistsRecordingStubAsync(MovementRecordingRowViewModel row) => Task.CompletedTask;

    /// <summary>
    /// Invoked by recording row Favorite toggles.
    /// </summary>
    internal async Task ToggleFavoriteRecordingStubAsync(MovementRecordingRowViewModel row)
    {
        if (parentWorkId == 0) return;

        Work? work = await unitOfWork.Works.GetByIdAsync(parentWorkId);
        if (work is null) return;

        if (work.FavoriteRecordingId == row.RecordingId)
        {
            work.FavoriteRecordingId = null;
        }
        else
        {
            work.FavoriteRecordingId = row.RecordingId;
        }

        unitOfWork.Works.Update(work);
        await unitOfWork.SaveChangesAsync();

        // Refresh UI properties
        foreach (MovementRecordingRowViewModel r in RecordingRows)
        {
            r.IsFavorite = r.RecordingId == work.FavoriteRecordingId;
        }

        // Re-sort the collection
        List<MovementRecordingRowViewModel> list = RecordingRows.ToList();
        RecordingRows.Clear();
        foreach (MovementRecordingRowViewModel r in list.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.DisplayText))
        {
            RecordingRows.Add(r);
        }
    }
}

/// <summary>
/// Represents a recording row item under a movement.
/// </summary>
public sealed class MovementRecordingRowViewModel : ViewModelBase
{
    private bool isFavorite;

    /// <summary>
    /// Gets the display name of the recording.
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// Gets the domain recording identifier.
    /// </summary>
    public int RecordingId { get; }

    /// <summary>
    /// Gets the domain performed movement (movement recording) identifier.
    /// </summary>
    public int PerformedMovementId { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this recording is marked as favorite.
    /// </summary>
    public bool IsFavorite
    {
        get => isFavorite;
        set
        {
            if (isFavorite == value) return;
            isFavorite = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the play command.
    /// </summary>
    public ICommand PlayRecordingRowCommand { get; }

    /// <summary>
    /// Gets the enqueue command.
    /// </summary>
    public ICommand EnqueueRecordingRowCommand { get; }

    /// <summary>
    /// Gets the toggle favorite command.
    /// </summary>
    public ICommand ToggleFavoriteRecordingRowCommand { get; }

    /// <summary>
    /// Gets the command to show this movement recording's info panel.
    /// </summary>
    public ICommand ShowMovementRecordingCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="MovementRecordingRowViewModel"/> with parent callbacks.
    /// </summary>
    /// <param name="displayText">The display text of the recording.</param>
    /// <param name="recordingId">The recording identifier.</param>
    /// <param name="performedMovementId">The performed movement identifier.</param>
    /// <param name="isFavorite">Whether it is the favorite.</param>
    /// <param name="panel">The owning movement panel.</param>
    public MovementRecordingRowViewModel(
        string displayText,
        int recordingId,
        int performedMovementId,
        bool isFavorite,
        MovementInfoPanelViewModel panel)
    {
        DisplayText = displayText;
        RecordingId = recordingId;
        PerformedMovementId = performedMovementId;
        IsFavorite = isFavorite;
        PlayRecordingRowCommand = new AsyncRelayCommand(() => panel.PlayRecordingStubAsync(this));
        EnqueueRecordingRowCommand = new AsyncRelayCommand(() => panel.EnqueueRecordingStubAsync(this));
        ToggleFavoriteRecordingRowCommand = new AsyncRelayCommand(() => panel.ToggleFavoriteRecordingStubAsync(this));
        ShowMovementRecordingCommand = new AsyncRelayCommand(() => panel.OpenInfoPanelAsync(LibraryItemType.MovementRecording, performedMovementId));

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
            Command = new AsyncRelayCommand(() => panel.PlayRecordingNextStubAsync(this))
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
            Command = new AsyncRelayCommand(() => panel.FavoriteRecordingStubAsync(this))
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Manage Playlists",
            Icon = TablerIcons.Icons.IconPlaylist,
            Command = new AsyncRelayCommand(() => panel.ManagePlaylistsRecordingStubAsync(this))
        });
    }
}
