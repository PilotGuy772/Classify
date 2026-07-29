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
using Classify.Core.Interfaces.Service;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Right-side Info Panel content for a selected library recording.
/// </summary>
public sealed class RecordingInfoPanelViewModel : InfoPanelViewModelBase
{
    private readonly IQueueService _queueService;
    private readonly INibbleBuilderService _nibbleBuilder;
    private int currentRecordingId;

    /// <summary>
    /// Gets the collection of work groups containing performed movements.
    /// </summary>
    public ObservableCollection<RecordingWorkGroupViewModel> WorkGroups { get; } = [];

    /// <summary>
    /// Main header action: Play this recording.
    /// </summary>
    public ICommand PlayRecordingCommand { get; }

    /// <summary>
    /// Main header action: Play Next this recording.
    /// </summary>
    public ICommand PlayNextRecordingCommand { get; }

    /// <summary>
    /// Main header action: Enqueue this recording.
    /// </summary>
    public ICommand EnqueueRecordingCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="RecordingInfoPanelViewModel"/> with direct database access and queue services.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    /// <param name="queueService">The queue service.</param>
    /// <param name="nibbleBuilder">The nibble builder service.</param>
    public RecordingInfoPanelViewModel(IUnitOfWork unitOfWork, IQueueService queueService, INibbleBuilderService nibbleBuilder) : base(unitOfWork)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _nibbleBuilder = nibbleBuilder ?? throw new ArgumentNullException(nameof(nibbleBuilder));

        PlayRecordingCommand = new AsyncRelayCommand(PlayRecordingAsync);
        PlayNextRecordingCommand = new AsyncRelayCommand(PlayNextRecordingAsync);
        EnqueueRecordingCommand = new AsyncRelayCommand(EnqueueRecordingAsync);
    }

    /// <summary>
    /// Loads performed movements grouped under their parent Works.
    /// </summary>
    /// <param name="recordingId">The recording identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task LoadAsync(int recordingId)
    {
        currentRecordingId = recordingId;
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
                grouped[work.Id] = (work, []);
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

    private Task PlayRecordingAsync()
    {
        return Task.CompletedTask;
    }

    private async Task PlayNextRecordingAsync()
    {
        if (currentRecordingId == 0) return;
        QueueItem? item = await _nibbleBuilder.BuildForRecordingAsync(currentRecordingId);
        if (item != null)
        {
            _queueService.EnqueueNext(item);
        }
    }

    private async Task EnqueueRecordingAsync()
    {
        if (currentRecordingId == 0) return;
        QueueItem? item = await _nibbleBuilder.BuildForRecordingAsync(currentRecordingId);
        if (item != null)
        {
            _queueService.Enqueue(item);
        }
    }

    /// <summary>
    /// Invoked by movement recording row Play buttons (stub).
    /// </summary>
    internal Task PlayMovementRecordingStubAsync(RecordingMovementRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by movement recording row Enqueue buttons.
    /// </summary>
    internal async Task EnqueueMovementRecordingStubAsync(RecordingMovementRowViewModel row)
    {
        PerformedMovement? pm = await unitOfWork.PerformedMovements.GetByIdAsync(row.PerformedMovementId);
        if (pm != null)
        {
            QueueItem? item = await _nibbleBuilder.BuildForMovementAsync(pm.MovementId, pm.RecordingId);
            if (item != null)
            {
                _queueService.Enqueue(item);
            }
        }
    }

    /// <summary>
    /// Invoked by movement recording row Play Next options menu.
    /// </summary>
    internal async Task PlayMovementRecordingRowNextStubAsync(RecordingMovementRowViewModel row)
    {
        PerformedMovement? pm = await unitOfWork.PerformedMovements.GetByIdAsync(row.PerformedMovementId);
        if (pm != null)
        {
            QueueItem? item = await _nibbleBuilder.BuildForMovementAsync(pm.MovementId, pm.RecordingId);
            if (item != null)
            {
                _queueService.EnqueueNext(item);
            }
        }
    }

    /// <summary>
    /// Invoked by movement recording row Favorite options menu (stub).
    /// </summary>
    internal Task FavoriteMovementRecordingRowStubAsync(RecordingMovementRowViewModel row)
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
    public ObservableCollection<RecordingMovementRowViewModel> Movements { get; } = [];

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
    }
}
