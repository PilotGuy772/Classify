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
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Right-side Info Panel content for a selected library work (Figma "Info Panel").
/// </summary>
public sealed class WorkInfoPanelViewModel : InfoPanelViewModelBase
{
    private string _composerLine = string.Empty;
    private string _secondaryLine = string.Empty;
    private readonly IQueueService _queueService;
    private readonly INibbleBuilderService _nibbleBuilder;

    /// <summary>
    /// Creates the panel view model with direct database access and queue services.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    /// <param name="queueService">The queue service.</param>
    /// <param name="nibbleBuilder">The nibble builder service.</param>
    public WorkInfoPanelViewModel(IUnitOfWork unitOfWork, IQueueService queueService, INibbleBuilderService nibbleBuilder) : base(unitOfWork)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _nibbleBuilder = nibbleBuilder ?? throw new ArgumentNullException(nameof(nibbleBuilder));

        PlayWorkCommand = new AsyncRelayCommand(PlayWorkAsync);
        PlayNextWorkCommand = new AsyncRelayCommand(PlayNextWorkAsync);
        EnqueueWorkCommand = new AsyncRelayCommand(EnqueueWorkAsync);
    }

    /// <summary>
    /// Panel header title (work name).
    /// </summary>
    public string PieceTitle
    {
        get => Title;
        private set => Title = value;
    }

    /// <summary>
    /// Composer line in the quick-info block.
    /// </summary>
    public string ComposerLine
    {
        get => _composerLine;
        private set
        {
            if (_composerLine == value) return;
            _composerLine = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Subtitle / catalog line in the quick-info block.
    /// </summary>
    public string SecondaryLine
    {
        get => _secondaryLine;
        private set
        {
            if (_secondaryLine == value) return;
            _secondaryLine = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Rows for the movements section.
    /// </summary>
    public ObservableCollection<MovementInfoRowViewModel> MovementRows { get; } = [];

    /// <summary>
    /// Rows for the recordings section.
    /// </summary>
    public ObservableCollection<RecordingInfoRowViewModel> RecordingRows { get; } = [];

    /// <summary>
    /// Main header action: Play this work.
    /// </summary>
    public ICommand PlayWorkCommand { get; }

    /// <summary>
    /// Main header action: Play Next this work.
    /// </summary>
    public ICommand PlayNextWorkCommand { get; }

    /// <summary>
    /// Main header action: Enqueue this work.
    /// </summary>
    public ICommand EnqueueWorkCommand { get; }

    private int currentWorkId;

    /// <summary>
    /// Loads movement and recording lists for the given work using a scoped unit of work.
    /// </summary>
    public override async Task LoadAsync(int workId)
    {
        currentWorkId = workId;
        MovementRows.Clear();
        RecordingRows.Clear();

        Work? work = await unitOfWork.Works.GetByIdAsync(workId);
        if (work is null)
        {
            PieceTitle = string.Empty;
            ComposerLine = string.Empty;
            SecondaryLine = string.Empty;
            return;
        }

        PieceTitle = work.Name;
        Composer? composer = await unitOfWork.Composers.GetByIdAsync(work.ComposerId);
        ComposerLine = composer?.Name ?? "—";
        SecondaryLine = string.IsNullOrWhiteSpace(work.CatalogNumber) ? "—" : work.CatalogNumber;

        int ordinal = 1;
        foreach (Movement movement in OrderMovements(await unitOfWork.Movements.GetMovementsByWorkIdAsync(workId)))
        {
            string label = FormatRomanOrdinal(ordinal);
            ordinal++;
            MovementRows.Add(new MovementInfoRowViewModel(label, movement.Name, movement.Id, this));
        }

        IEnumerable<Recording> recordings = await unitOfWork.Recordings.GetRecordingsByWorkIdAsync(workId);
        List<RecordingInfoRowViewModel> rows = [];

        foreach (Recording recording in recordings)
        {
            bool isFavorite = recording.Id == work.FavoriteRecordingId;
            rows.Add(new RecordingInfoRowViewModel(recording.Name, recording.Id, isFavorite, this));
        }

        foreach (RecordingInfoRowViewModel row in rows.OrderByDescending(r => r.IsFavorite).ThenBy(r => r.DisplayInfo))
        {
            RecordingRows.Add(row);
        }
    }

    /// <summary>
    /// Invoked by movement row Play buttons (stub).
    /// </summary>
    internal Task PlayMovementStubAsync(MovementInfoRowViewModel row)
    {
        return PlayMovementAsync(row);
    }

    /// <summary>
    /// Invoked by movement row Enqueue buttons (stub).
    /// </summary>
    internal Task EnqueueMovementStubAsync(MovementInfoRowViewModel row)
    {
        return EnqueueMovementAsync(row);
    }

    /// <summary>
    /// Invoked by recording row Play buttons (stub).
    /// </summary>
    internal Task PlayRecordingStubAsync(RecordingInfoRowViewModel row)
    {
        return PlayRecordingAsync(row);
    }

    /// <summary>
    /// Invoked by recording row Enqueue buttons (stub).
    /// </summary>
    internal Task EnqueueRecordingStubAsync(RecordingInfoRowViewModel row)
    {
        return EnqueueRecordingAsync(row);
    }

    /// <summary>
    /// Invoked by recording row Favorite toggles (stub).
    /// </summary>
    internal Task ToggleFavoriteRecordingStubAsync(RecordingInfoRowViewModel row)
    {
        return ToggleFavoriteRecordingAsync(row);
    }

    /// <summary>
    /// Invoked by movement row Play Next options menu.
    /// </summary>
    internal async Task PlayMovementRowNextStubAsync(MovementInfoRowViewModel row)
    {
        QueueItem? item = await _nibbleBuilder.BuildForMovementAsync(row.MovementId);
        if (item != null)
        {
            _queueService.EnqueueNext(item);
        }
    }

    /// <summary>
    /// Invoked by movement row Favorite options menu (stub).
    /// </summary>
    internal Task FavoriteMovementRowStubAsync(MovementInfoRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by recording row Play Next options menu.
    /// </summary>
    internal async Task PlayRecordingRowNextStubAsync(RecordingInfoRowViewModel row)
    {
        QueueItem? item = await _nibbleBuilder.BuildForRecordingAsync(row.RecordingId);
        if (item != null)
        {
            _queueService.EnqueueNext(item);
        }
    }

    /// <summary>
    /// Invoked by recording row Favorite options menu (stub).
    /// </summary>
    internal Task FavoriteRecordingRowStubAsync(RecordingInfoRowViewModel row)
    {
        return Task.CompletedTask;
    }

    private static IEnumerable<Movement> OrderMovements(IEnumerable<Movement> movements)
    {
        List<Movement> list = movements.ToList();
        list.Sort((Movement a, Movement b) =>
        {
            int orderCompare = a.Order.CompareTo(b.Order);
            if (orderCompare != 0)
                return orderCompare;
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });
        return list;
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

    private Task PlayWorkAsync()
    {
        return Task.CompletedTask;
    }

    private async Task PlayNextWorkAsync()
    {
        QueueItem? item = await _nibbleBuilder.BuildForWorkAsync(currentWorkId);
        if (item != null)
        {
            _queueService.EnqueueNext(item);
        }
    }

    private async Task EnqueueWorkAsync()
    {
        QueueItem? item = await _nibbleBuilder.BuildForWorkAsync(currentWorkId);
        if (item != null)
        {
            _queueService.Enqueue(item);
        }
    }

    private Task PlayMovementAsync(MovementInfoRowViewModel row)
    {
        return Task.CompletedTask;
    }

    private async Task EnqueueMovementAsync(MovementInfoRowViewModel row)
    {
        QueueItem? item = await _nibbleBuilder.BuildForMovementAsync(row.MovementId);
        if (item != null)
        {
            _queueService.Enqueue(item);
        }
    }

    private Task PlayRecordingAsync(RecordingInfoRowViewModel row)
    {
        return Task.CompletedTask;
    }

    private async Task EnqueueRecordingAsync(RecordingInfoRowViewModel row)
    {
        QueueItem? item = await _nibbleBuilder.BuildForRecordingAsync(row.RecordingId);
        if (item != null)
        {
            _queueService.Enqueue(item);
        }
    }

    private async Task ToggleFavoriteRecordingAsync(RecordingInfoRowViewModel row)
    {
        Work? work = await unitOfWork.Works.GetByIdAsync(currentWorkId);
        if (work == null) return;

        // Toggle: if already favorite, we could clear it, but user says "makes it the new favorite".
        // I'll toggle it off if already on for better UX, or just keep it.
        // Let's toggle it off if it is already the favorite.
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

        // Update UI properties
        foreach (RecordingInfoRowViewModel r in RecordingRows)
        {
            r.IsFavorite = r.RecordingId == work.FavoriteRecordingId;
        }

        // Re-sort the collection
        List<RecordingInfoRowViewModel> list = RecordingRows.ToList();
        RecordingRows.Clear();
        foreach (RecordingInfoRowViewModel r in list.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.DisplayInfo))
        {
            RecordingRows.Add(r);
        }
    }
}
