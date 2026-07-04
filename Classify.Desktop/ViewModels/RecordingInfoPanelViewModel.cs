using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Right-side Info Panel content for a selected library recording.
/// </summary>
public sealed class RecordingInfoPanelViewModel : InfoPanelViewModelBase
{
    /// <summary>
    /// Gets the collection of work groups containing performed movements.
    /// </summary>
    public ObservableCollection<RecordingWorkGroupViewModel> WorkGroups { get; } = new();

    /// <summary>
    /// Initializes a new instance of <see cref="RecordingInfoPanelViewModel"/> with direct database access.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    public RecordingInfoPanelViewModel(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    /// <summary>
    /// Loads performed movements grouped under their parent Works.
    /// </summary>
    /// <param name="recordingId">The recording identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task LoadAsync(int recordingId)
    {
        WorkGroups.Clear();

        Recording? recording = await unitOfWork.Recordings.GetByIdAsync(recordingId);
        if (recording is null)
        {
            Title = string.Empty;
            return;
        }

        Title = recording.Name;

        IEnumerable<PerformedMovement> pms = await unitOfWork.PerformedMovements.GetByRecordingId(recordingId);
        Dictionary<int, (Work Work, List<(PerformedMovement Pm, Movement Mv)> Items)> grouped = new();

        foreach (PerformedMovement pm in pms)
        {
            Movement? mv = await unitOfWork.Movements.GetByIdAsync(pm.MovementId);
            if (mv is null) continue;

            Work? work = await unitOfWork.Works.GetByIdAsync(mv.WorkId);
            if (work is null) continue;

            if (!grouped.ContainsKey(work.Id))
            {
                grouped[work.Id] = (work, new List<(PerformedMovement, Movement)>());
            }

            grouped[work.Id].Items.Add((pm, mv));
        }

        foreach ((Work Work, List<(PerformedMovement Pm, Movement Mv)> Items) pair in grouped.Values)
        {
            RecordingWorkGroupViewModel group = new(pair.Work.Name, pair.Work.Id, this);
            
            // Sort movements by Order, then Name
            List<(PerformedMovement Pm, Movement Mv)> sortedMovements = pair.Items
                .OrderBy(x => x.Mv.Order)
                .ThenBy(x => x.Mv.Name)
                .ToList();

            foreach ((PerformedMovement Pm, Movement Mv) item in sortedMovements)
            {
                string ordinalLabel = FormatRomanOrdinal(item.Mv.Order);
                group.Movements.Add(new RecordingMovementRowViewModel(
                    ordinalLabel,
                    item.Mv.Name,
                    item.Pm.Id,
                    this));
            }

            WorkGroups.Add(group);
        }
    }

    private static string FormatRomanOrdinal(int indexOneBased)
    {
        string[] table =
        [
            "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
            "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX",
            "XXI", "XXII", "XXIII", "XXIV", "XXV", "XXVI", "XXVII", "XXVIII", "XXIX", "XXX"
        ];
        if (indexOneBased <= 0 || indexOneBased >= table.Length)
            return indexOneBased.ToString(CultureInfo.InvariantCulture) + ".";
        return table[indexOneBased] + ".";
    }

    /// <summary>
    /// Invoked by movement recording row Play buttons (stub).
    /// </summary>
    internal Task PlayMovementRecordingStubAsync(RecordingMovementRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by movement recording row Enqueue buttons (stub).
    /// </summary>
    internal Task EnqueueMovementRecordingStubAsync(RecordingMovementRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by movement recording row Play Next options menu (stub).
    /// </summary>
    internal Task PlayMovementRecordingRowNextStubAsync(RecordingMovementRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by movement recording row Favorite options menu (stub).
    /// </summary>
    internal Task FavoriteMovementRecordingRowStubAsync(RecordingMovementRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by movement recording row Manage Playlists options menu (stub).
    /// </summary>
    internal Task ManagePlaylistsMovementRecordingRowStubAsync(RecordingMovementRowViewModel row)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents a work group containing movement recording rows.
/// </summary>
public sealed class RecordingWorkGroupViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the work title.
    /// </summary>
    public string WorkTitle { get; }

    /// <summary>
    /// Gets the domain work identifier.
    /// </summary>
    public int WorkId { get; }

    /// <summary>
    /// Gets the movement recording rows.
    /// </summary>
    public ObservableCollection<RecordingMovementRowViewModel> Movements { get; } = new();

    /// <summary>
    /// Gets the command to show this work's info panel.
    /// </summary>
    public ICommand ShowWorkCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="RecordingWorkGroupViewModel"/>.
    /// </summary>
    /// <param name="workTitle">The work title.</param>
    /// <param name="workId">The work identifier.</param>
    /// <param name="panel">The owning recording panel.</param>
    public RecordingWorkGroupViewModel(string workTitle, int workId, RecordingInfoPanelViewModel panel)
    {
        WorkTitle = workTitle;
        WorkId = workId;
        ShowWorkCommand = new AsyncRelayCommand(() => panel.OpenInfoPanelAsync(LibraryItemType.Work, workId));
    }
}

/// <summary>
/// Represents a performed movement (movement recording) row under a work group.
/// </summary>
public sealed class RecordingMovementRowViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the ordinal label (e.g. "I.").
    /// </summary>
    public string OrdinalLabel { get; }

    /// <summary>
    /// Gets the movement name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the domain performed movement identifier.
    /// </summary>
    public int PerformedMovementId { get; }

    /// <summary>
    /// Gets the row play action.
    /// </summary>
    public ICommand PlayMovementRecordingRowCommand { get; }

    /// <summary>
    /// Gets the row enqueue action.
    /// </summary>
    public ICommand EnqueueMovementRecordingRowCommand { get; }

    /// <summary>
    /// Gets the command to show this movement recording's info panel.
    /// </summary>
    public ICommand ShowMovementRecordingCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="RecordingMovementRowViewModel"/> with parent callbacks.
    /// </summary>
    /// <param name="ordinalLabel">The Roman numeral label.</param>
    /// <param name="name">The movement name.</param>
    /// <param name="performedMovementId">The performed movement identifier.</param>
    /// <param name="panel">The owning recording panel.</param>
    public RecordingMovementRowViewModel(
        string ordinalLabel,
        string name,
        int performedMovementId,
        RecordingInfoPanelViewModel panel)
    {
        OrdinalLabel = ordinalLabel;
        Name = name;
        PerformedMovementId = performedMovementId;

        PlayMovementRecordingRowCommand = new AsyncRelayCommand(() => panel.PlayMovementRecordingStubAsync(this));
        EnqueueMovementRecordingRowCommand = new AsyncRelayCommand(() => panel.EnqueueMovementRecordingStubAsync(this));
        ShowMovementRecordingCommand = new AsyncRelayCommand(() => panel.OpenInfoPanelAsync(LibraryItemType.MovementRecording, performedMovementId));

        MenuOptions.Clear();
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Now",
            Icon = TablerIcons.Icons.IconPlayerPlay,
            Command = PlayMovementRecordingRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Next",
            Icon = TablerIcons.Icons.IconCornerUpLeft,
            Command = new AsyncRelayCommand(() => panel.PlayMovementRecordingRowNextStubAsync(this))
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Enqueue",
            Icon = TablerIcons.Icons.IconCornerDownLeft,
            Command = EnqueueMovementRecordingRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Favorite",
            Icon = TablerIcons.Icons.IconHeart,
            Command = new AsyncRelayCommand(() => panel.FavoriteMovementRecordingRowStubAsync(this))
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Manage Playlists",
            Icon = TablerIcons.Icons.IconPlaylist,
            Command = new AsyncRelayCommand(() => panel.ManagePlaylistsMovementRecordingRowStubAsync(this))
        });
    }
}
