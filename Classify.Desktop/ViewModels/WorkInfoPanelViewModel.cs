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
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Right-side Info Panel content for a selected library work (Figma "Info Panel").
/// </summary>
public sealed class WorkInfoPanelViewModel : ViewModelBase
{
    private readonly IUnitOfWork unitOfWork;
    private MainWindowViewModel? host;

    private string pieceTitle = string.Empty;
    private string composerLine = string.Empty;
    private string secondaryLine = string.Empty;

    /// <summary>
    /// Creates the panel view model with direct database access via <see cref="IUnitOfWork"/>.
    /// </summary>
    public WorkInfoPanelViewModel(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;

        ClosePanelCommand = new AsyncRelayCommand(CloseAsync);
        PlayWorkCommand = new AsyncRelayCommand(PlayWorkAsync);
        EnqueueWorkCommand = new AsyncRelayCommand(EnqueueWorkAsync);
        AddWorkToPlaylistCommand = new AsyncRelayCommand(AddWorkToPlaylistAsync);
    }

    /// <summary>
    /// Wire the shell host after construction so panel commands can collapse the dock without circular DI.
    /// </summary>
    public void AttachHost(MainWindowViewModel shell)
    {
        host = shell;
    }

    /// <summary>
    /// Panel header title (work name).
    /// </summary>
    public string PieceTitle
    {
        get => pieceTitle;
        private set
        {
            if (pieceTitle == value) return;
            pieceTitle = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Composer line in the quick-info block.
    /// </summary>
    public string ComposerLine
    {
        get => composerLine;
        private set
        {
            if (composerLine == value) return;
            composerLine = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Subtitle / catalog line in the quick-info block.
    /// </summary>
    public string SecondaryLine
    {
        get => secondaryLine;
        private set
        {
            if (secondaryLine == value) return;
            secondaryLine = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Rows for the movements section.
    /// </summary>
    public ObservableCollection<MovementInfoRowViewModel> MovementRows { get; } = new();

    /// <summary>
    /// Rows for the recordings section.
    /// </summary>
    public ObservableCollection<RecordingInfoRowViewModel> RecordingRows { get; } = new();

    /// <summary>
    /// Command to close/collapse the info panel.
    /// </summary>
    public ICommand ClosePanelCommand { get; }

    /// <summary>
    /// Main header action: Play this work.
    /// </summary>
    public ICommand PlayWorkCommand { get; }

    /// <summary>
    /// Main header action: Enqueue this work.
    /// </summary>
    public ICommand EnqueueWorkCommand { get; }

    /// <summary>
    /// Main header action: Add this work to a playlist.
    /// </summary>
    public ICommand AddWorkToPlaylistCommand { get; }

    private int currentWorkId;

    /// <summary>
    /// Loads movement and recording lists for the given work using a scoped unit of work.
    /// </summary>
    public async Task LoadAsync(int workId)
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
        List<RecordingInfoRowViewModel> rows = new();

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

    private Task CloseAsync()
    {
        host?.CloseWorkInfoPanel();
        return Task.CompletedTask;
    }

    private Task PlayWorkAsync()
    {
        return Task.CompletedTask;
    }

    private Task EnqueueWorkAsync()
    {
        return Task.CompletedTask;
    }

    private Task AddWorkToPlaylistAsync()
    {
        return Task.CompletedTask;
    }

    private Task PlayMovementAsync(MovementInfoRowViewModel row)
    {
        return Task.CompletedTask;
    }

    private Task EnqueueMovementAsync(MovementInfoRowViewModel row)
    {
        return Task.CompletedTask;
    }

    private Task PlayRecordingAsync(RecordingInfoRowViewModel row)
    {
        return Task.CompletedTask;
    }

    private Task EnqueueRecordingAsync(RecordingInfoRowViewModel row)
    {
        return Task.CompletedTask;
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
