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
/// View model for the standalone <see cref="Views.PlayerWindow"/> providing playback metadata, sidebar state, and queue list synchronization.
/// </summary>
public sealed class PlayerWindowViewModel : ViewModelBase
{
    private readonly IQueueService _queueService;
    private readonly IUnitOfWork _unitOfWork;

    private bool _isSidebarExpanded = true;
    private double _windowWidth = 899;
    private string _currentWorkTitle = "—";
    private string _currentMovementOrdinal = "";
    private string _currentMovementName = "—";
    private string _currentComposerName = "—";
    private string _currentPerformersLine = "—";
    private string _currentRecordingDetailsLine = "—";
    private string _currentPositionText = "0:00";
    private string _totalDurationText = "0:00";
    private bool _isEmptyQueue = true;

    /// <summary>
    /// Gets or sets a value indicating whether the queue sidebar is expanded.
    /// </summary>
    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;
        set
        {
            if (_isSidebarExpanded == value) return;
            _isSidebarExpanded = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the target width of the window depending on sidebar expansion state.
    /// </summary>
    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (Math.Abs(_windowWidth - value) < 0.1) return;
            _windowWidth = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the title of the currently playing work.
    /// </summary>
    public string CurrentWorkTitle
    {
        get => _currentWorkTitle;
        private set
        {
            if (_currentWorkTitle == value) return;
            _currentWorkTitle = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the Roman ordinal string for the currently playing movement (e.g. "III.").
    /// </summary>
    public string CurrentMovementOrdinal
    {
        get => _currentMovementOrdinal;
        private set
        {
            if (_currentMovementOrdinal == value) return;
            _currentMovementOrdinal = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the name of the currently playing movement.
    /// </summary>
    public string CurrentMovementName
    {
        get => _currentMovementName;
        private set
        {
            if (_currentMovementName == value) return;
            _currentMovementName = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the composer name for the currently playing item.
    /// </summary>
    public string CurrentComposerName
    {
        get => _currentComposerName;
        private set
        {
            if (_currentComposerName == value) return;
            _currentComposerName = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the conductor and ensemble performers line for the currently playing item.
    /// </summary>
    public string CurrentPerformersLine
    {
        get => _currentPerformersLine;
        private set
        {
            if (_currentPerformersLine == value) return;
            _currentPerformersLine = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the recording details line (label and year) for the currently playing item.
    /// </summary>
    public string CurrentRecordingDetailsLine
    {
        get => _currentRecordingDetailsLine;
        private set
        {
            if (_currentRecordingDetailsLine == value) return;
            _currentRecordingDetailsLine = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the formatted playback position string (e.g. "0:00").
    /// </summary>
    public string CurrentPositionText
    {
        get => _currentPositionText;
        private set
        {
            if (_currentPositionText == value) return;
            _currentPositionText = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the formatted total track duration string.
    /// </summary>
    public string TotalDurationText
    {
        get => _totalDurationText;
        private set
        {
            if (_totalDurationText == value) return;
            _totalDurationText = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the queue is currently empty.
    /// </summary>
    public bool IsEmptyQueue
    {
        get => _isEmptyQueue;
        private set
        {
            if (_isEmptyQueue == value) return;
            _isEmptyQueue = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the collection of queue item view models for the sidebar.
    /// </summary>
    public ObservableCollection<PlayerQueueItemViewModel> QueueItems { get; } = new();

    /// <summary>
    /// Gets the command to toggle sidebar visibility.
    /// </summary>
    public ICommand ToggleSidebarCommand { get; }

    /// <summary>
    /// Gets the command to clear the queue.
    /// </summary>
    public ICommand ClearQueueCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PlayerWindowViewModel"/>.
    /// </summary>
    /// <param name="queueService">The queue service instance.</param>
    /// <param name="unitOfWork">The database unit of work instance.</param>
    public PlayerWindowViewModel(IQueueService queueService, IUnitOfWork unitOfWork)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

        ToggleSidebarCommand = new RelayCommand(() => IsSidebarExpanded = !IsSidebarExpanded);
        ClearQueueCommand = new RelayCommand(_queueService.Clear);

        _queueService.QueueChanged += OnQueueStateChanged;
        _queueService.CurrentItemChanged += OnQueueStateChanged;

        _ = RefreshAsync();
    }

    /// <summary>
    /// Handles updates to the queue service state.
    /// </summary>
    private void OnQueueStateChanged(object? sender, EventArgs e)
    {
        _ = RefreshAsync();
    }

    /// <summary>
    /// Asynchronously refreshes queue sidebar items and active track metadata from the database.
    /// </summary>
    public async Task RefreshAsync()
    {
        IReadOnlyList<QueueItem> rawItems = _queueService.Items;
        int activeNibbleIdx = _queueService.CurrentNibbleIndex;
        int activeMovementIdx = _queueService.CurrentMovementIndex;

        IsEmptyQueue = rawItems.Count == 0;

        // Build queue item view models
        List<PlayerQueueItemViewModel> newQueueVms = new List<PlayerQueueItemViewModel>();

        for (int nibbleIdx = 0; nibbleIdx < rawItems.Count; nibbleIdx++)
        {
            QueueItem item = rawItems[nibbleIdx];
            Work? work = await _unitOfWork.Works.GetByIdAsync(item.Nibble.WorkId);
            Recording? recording = await _unitOfWork.Recordings.GetByIdAsync(item.Nibble.RecordingId);

            string workTitle = work?.Name ?? $"Work #{item.Nibble.WorkId}";
            string subtitle = FormatConductorAndEnsemble(recording);

            PlayerQueueItemViewModel queueVm = new PlayerQueueItemViewModel(
                nibbleIdx,
                workTitle,
                subtitle,
                OnSelectNibble);

            for (int movementIdx = 0; movementIdx < item.Movements.Count; movementIdx++)
            {
                NibbleMovement nm = item.Movements[movementIdx];
                Movement? mv = await _unitOfWork.Movements.GetByIdAsync(nm.MovementId);

                int displayOrder = mv?.Order ?? nm.Order;
                string ordinalLabel = FormatRomanOrdinal(displayOrder);
                string mvName = mv?.Name ?? $"Movement #{nm.MovementId}";
                bool isPlaying = (nibbleIdx == activeNibbleIdx && movementIdx == activeMovementIdx);

                PlayerMovementItemViewModel movementVm = new PlayerMovementItemViewModel(
                    nibbleIdx,
                    movementIndex: movementIdx,
                    ordinalLabel: ordinalLabel,
                    movementName: mvName,
                    isCurrentlyPlaying: isPlaying,
                    onSelect: OnSelectMovement);

                queueVm.Movements.Add(movementVm);
            }

            newQueueVms.Add(queueVm);
        }

        QueueItems.Clear();
        foreach (PlayerQueueItemViewModel vm in newQueueVms)
        {
            QueueItems.Add(vm);
        }

        // Update current track details for main player display
        QueueItem? activeItem = _queueService.CurrentItem;
        NibbleMovement? activeMovement = _queueService.CurrentMovement;

        if (activeItem != null)
        {
            Work? activeWork = await _unitOfWork.Works.GetByIdAsync(activeItem.Nibble.WorkId);
            Recording? activeRec = await _unitOfWork.Recordings.GetByIdAsync(activeItem.Nibble.RecordingId);
            Composer? activeComposer = activeWork != null ? await _unitOfWork.Composers.GetByIdAsync(activeWork.ComposerId) : null;
            Movement? activeMv = activeMovement != null ? await _unitOfWork.Movements.GetByIdAsync(activeMovement.MovementId) : null;

            CurrentWorkTitle = activeWork?.Name ?? "—";
            CurrentMovementOrdinal = activeMv != null ? FormatRomanOrdinal(activeMv.Order) : (activeMovement != null ? FormatRomanOrdinal(activeMovement.Order) : "");
            CurrentMovementName = activeMv?.Name ?? "—";
            CurrentComposerName = activeComposer?.Name ?? "—";
            CurrentPerformersLine = FormatPerformersLine(activeRec);
            CurrentRecordingDetailsLine = FormatRecordingDetails(activeRec);
        }
        else
        {
            CurrentWorkTitle = "No Track Queued";
            CurrentMovementOrdinal = "";
            CurrentMovementName = "Add items to queue to begin";
            CurrentComposerName = "—";
            CurrentPerformersLine = "—";
            CurrentRecordingDetailsLine = "—";
        }
    }

    /// <summary>
    /// Invoked when a nibble container is clicked in the sidebar.
    /// </summary>
    private void OnSelectNibble(int nibbleIndex)
    {
        _queueService.SkipToNibble(nibbleIndex, 0);
    }

    /// <summary>
    /// Invoked when a movement row is clicked in the sidebar.
    /// </summary>
    private void OnSelectMovement(int nibbleIndex, int movementIndex)
    {
        _queueService.SkipToNibble(nibbleIndex, movementIndex);
    }

    /// <summary>
    /// Formats conductor and ensemble for sidebar subtitle.
    /// </summary>
    private static string FormatConductorAndEnsemble(Recording? recording)
    {
        if (recording == null) return "—";
        if (!string.IsNullOrWhiteSpace(recording.Conductor) && !string.IsNullOrWhiteSpace(recording.Ensemble))
        {
            return $"{recording.Conductor} | {recording.Ensemble}";
        }
        if (!string.IsNullOrWhiteSpace(recording.Conductor)) return recording.Conductor;
        if (!string.IsNullOrWhiteSpace(recording.Ensemble)) return recording.Ensemble;
        return recording.Name;
    }

    /// <summary>
    /// Formats conductor and ensemble for main player view.
    /// </summary>
    private static string FormatPerformersLine(Recording? recording)
    {
        if (recording == null) return "—";
        if (!string.IsNullOrWhiteSpace(recording.Conductor) && !string.IsNullOrWhiteSpace(recording.Ensemble))
        {
            return $"{recording.Conductor}, {recording.Ensemble}";
        }
        return recording.Conductor ?? recording.Ensemble ?? recording.Name;
    }

    /// <summary>
    /// Formats label and year for main player view.
    /// </summary>
    private static string FormatRecordingDetails(Recording? recording)
    {
        if (recording == null) return "—";
        if (recording.Year.HasValue)
        {
            return $"{recording.Name} ({recording.Year.Value})";
        }
        return recording.Name;
    }

    /// <summary>
    /// Converts a 1-based order index into a Roman numeral string.
    /// </summary>
    private static string FormatRomanOrdinal(int indexOneBased)
    {
        string[] table =
        [
            "", "I.", "II.", "III.", "IV.", "V.", "VI.", "VII.", "VIII.", "IX.", "X.",
            "XI.", "XII.", "XIII.", "XIV.", "XV.", "XVI.", "XVII.", "XVIII.", "XIX.", "XX.",
            "XXI.", "XXII.", "XXIII.", "XXIV.", "XXV.", "XXVI.", "XXVII.", "XXVIII.", "XXIX.", "XXX."
        ];
        if (indexOneBased <= 0 || indexOneBased >= table.Length)
            return indexOneBased.ToString(CultureInfo.InvariantCulture) + ".";
        return table[indexOneBased];
    }
}
