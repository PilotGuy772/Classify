using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// View model for the standalone <see cref="Views.PlayerWindow"/> providing playback metadata, sidebar state, and queue list synchronization.
/// </summary>
public sealed class PlayerWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IQueueService _queueService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlaybackService _playbackService;

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
    private bool _isPlaying;
    private double _playbackProgress;
    private bool _isSeekDragging;
    private bool _resumeAfterSeekDrag;
    private TimeSpan? _heldSeekPosition;
    private long _heldSeekUntilTicks;
    private bool _isSeekHintVisible;
    private string _seekHintText = "0:00";
    private readonly DispatcherTimer _playbackClock;

    /// <summary>
    /// How long a released seek keeps its preview when the player has not reported the new position yet.
    /// </summary>
    private const int SeekDisplayHoldMilliseconds = 1500;

    /// <summary>
    /// How close the reported position must be to the seek target, in seconds, before the preview is dropped.
    /// </summary>
    private const double SeekDisplayHoldToleranceSeconds = 1.5;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

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
            RaisePropertyChanged(nameof(SidebarToggleIcon));
        }
    }

    /// <summary>
    /// Gets the icon for the queue sidebar toggle button depending on expansion state.
    /// </summary>
    public TablerIcons.Icons SidebarToggleIcon => IsSidebarExpanded
        ? TablerIcons.Icons.IconLayoutSidebarLeftCollapse
        : TablerIcons.Icons.IconLayoutSidebarLeftExpand;

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
    /// Gets the display fill of the progress bar, from 0 to 100.
    /// While the pointer is held on the bar, this previews the dragged point instead of the player position.
    /// </summary>
    public double PlaybackProgress
    {
        get => _playbackProgress;
        private set
        {
            if (Math.Abs(_playbackProgress - value) < 0.0001)
            {
                return;
            }

            _playbackProgress = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the time shown beside the pointer for the progress-bar point under it.
    /// </summary>
    public string SeekHintText
    {
        get => _seekHintText;
        private set
        {
            if (_seekHintText == value)
            {
                return;
            }

            _seekHintText = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the progress-bar time hint is visible.
    /// </summary>
    public bool IsSeekHintVisible
    {
        get => _isSeekHintVisible;
        private set
        {
            if (_isSeekHintVisible == value)
            {
                return;
            }

            _isSeekHintVisible = value;
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
    /// Gets a value indicating whether the player button is showing the playing state.
    /// The value changes only when <see cref="IPlaybackService.PlaybackStateChanged"/> is raised.
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying == value)
            {
                return;
            }

            _isPlaying = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(PlayPauseIcon));
        }
    }

    /// <summary>
    /// Gets the play icon while paused and the pause icon while playing.
    /// </summary>
    public TablerIcons.Icons PlayPauseIcon => IsPlaying
        ? TablerIcons.Icons.IconPlayerPause
        : TablerIcons.Icons.IconPlayerPlay;

    /// <summary>
    /// Gets the command that plays when the button shows play, and pauses when it shows pause.
    /// </summary>
    public ICommand PlayPauseCommand { get; }

    /// <summary>
    /// Gets the command that pauses playback without unloading the audio file.
    /// </summary>
    public ICommand PauseCommand { get; }

    /// <summary>
    /// Gets the command that stops playback and unloads the audio file.
    /// </summary>
    public ICommand StopCommand { get; }

    /// <summary>
    /// Gets the command that plays the previous movement, or restarts the current one.
    /// </summary>
    public ICommand SkipBackwardCommand { get; }

    /// <summary>
    /// Gets the command that plays the next movement.
    /// </summary>
    public ICommand SkipForwardCommand { get; }

    /// <summary>
    /// Gets the command that seeks backward fifteen seconds.
    /// </summary>
    public ICommand SeekBackwardCommand { get; }

    /// <summary>
    /// Gets the command that seeks forward fifteen seconds.
    /// </summary>
    public ICommand SeekForwardCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PlayerWindowViewModel"/>.
    /// </summary>
    /// <param name="queueService">The queue service instance.</param>
    /// <param name="unitOfWork">The database unit of work instance.</param>
    /// <param name="playbackService">The playback service that drives the transport controls.</param>
    public PlayerWindowViewModel(IQueueService queueService, IUnitOfWork unitOfWork, IPlaybackService playbackService)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));

        ToggleSidebarCommand = new RelayCommand(() => IsSidebarExpanded = !IsSidebarExpanded);
        ClearQueueCommand = new RelayCommand(_queueService.Clear);
        PlayPauseCommand = new AsyncRelayCommand(SendPlayOrPauseAsync);
        PauseCommand = new RelayCommand(_playbackService.Pause);
        StopCommand = new RelayCommand(_playbackService.Stop);
        SkipBackwardCommand = new AsyncRelayCommand(() => _playbackService.SkipBackwardAsync());
        SkipForwardCommand = new AsyncRelayCommand(() => _playbackService.SkipForwardAsync());
        SeekBackwardCommand = new RelayCommand(() => _playbackService.Seek(-15));
        SeekForwardCommand = new RelayCommand(() => _playbackService.Seek(15));

        _playbackClock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _playbackClock.Tick += OnPlaybackClockTick;

        _queueService.QueueChanged += OnQueueStateChanged;
        _queueService.CurrentItemChanged += OnQueueStateChanged;
        _playbackService.PlaybackStateChanged += OnPlaybackStateChanged;
        _playbackService.PlaybackPositionChanged += OnPlaybackPositionChanged;
        OnPlaybackStateChanged(this, EventArgs.Empty);
        RefreshPlaybackProgress();
        _playbackClock.Start();

        _ = RefreshAsync();
    }

    /// <summary>
    /// Stops the display refresh and detaches playback notifications.
    /// </summary>
    public void Dispose()
    {
        _playbackClock.Stop();
        _playbackClock.Tick -= OnPlaybackClockTick;
        _playbackService.PlaybackStateChanged -= OnPlaybackStateChanged;
        _playbackService.PlaybackPositionChanged -= OnPlaybackPositionChanged;
        _queueService.QueueChanged -= OnQueueStateChanged;
        _queueService.CurrentItemChanged -= OnQueueStateChanged;
    }

    /// <summary>
    /// Sends play or pause based on the button state. The icon changes only after the playback service reports the new state.
    /// </summary>
    private Task SendPlayOrPauseAsync()
    {
        if (IsPlaying)
        {
            _playbackService.Pause();
            return Task.CompletedTask;
        }

        return _playbackService.PlayAsync();
    }

    /// <summary>
    /// Applies the playback service's current playing flag on the UI thread.
    /// </summary>
    private void OnPlaybackStateChanged(object? sender, EventArgs e)
    {
        bool isPlaying = _playbackService.IsPlaying;
        if (Dispatcher.UIThread.CheckAccess())
        {
            IsPlaying = isPlaying;
            return;
        }

        Dispatcher.UIThread.Post(() => IsPlaying = isPlaying);
    }

    /// <summary>
    /// Re-reads the player's position and duration after the player reports a timeline change.
    /// The read is posted so it does not run inside a player callback.
    /// </summary>
    private void OnPlaybackPositionChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshPlaybackProgress);
    }

    /// <summary>
    /// Re-reads the player's position and duration. The window does not advance the time itself.
    /// </summary>
    private void OnPlaybackClockTick(object? sender, EventArgs e)
    {
        RefreshPlaybackProgress();
    }

    /// <summary>
    /// Shows the time at <paramref name="fraction"/> of the current duration beside the pointer.
    /// Does not move playback.
    /// </summary>
    /// <param name="fraction">Point on the progress bar, where zero is the start and one is the end.</param>
    public void ShowSeekHint(double fraction)
    {
        SeekHintText = FormatPlaybackTime(PositionAtFraction(fraction));
        IsSeekHintVisible = true;
    }

    /// <summary>
    /// Hides the time hint. A drag in progress keeps the hint visible.
    /// </summary>
    public void HideSeekHint()
    {
        if (_isSeekDragging)
        {
            return;
        }

        IsSeekHintVisible = false;
    }

    /// <summary>
    /// Pauses playback and previews <paramref name="fraction"/> on the progress bar.
    /// </summary>
    /// <param name="fraction">Point on the progress bar, where zero is the start and one is the end.</param>
    public void BeginSeekDrag(double fraction)
    {
        if (!_isSeekDragging)
        {
            _resumeAfterSeekDrag = _playbackService.IsPlaying;
            _isSeekDragging = true;
            if (_resumeAfterSeekDrag)
            {
                _playbackService.Pause();
            }
        }

        ApplyDraggedProgress(fraction);
    }

    /// <summary>
    /// Moves the progress-bar preview and time hint to <paramref name="fraction"/>.
    /// </summary>
    /// <param name="fraction">Point on the progress bar, where zero is the start and one is the end.</param>
    public void UpdateSeekDrag(double fraction)
    {
        if (!_isSeekDragging)
        {
            return;
        }

        ApplyDraggedProgress(fraction);
    }

    /// <summary>
    /// Seeks to <paramref name="fraction"/> and resumes playback when it was playing before the drag.
    /// </summary>
    /// <param name="fraction">Point on the progress bar, where zero is the start and one is the end.</param>
    /// <param name="keepHintVisible">When true, the time hint stays visible after the seek.</param>
    public void EndSeekDrag(double fraction, bool keepHintVisible)
    {
        if (!_isSeekDragging)
        {
            return;
        }

        fraction = ClampUnit(fraction);
        bool resume = _resumeAfterSeekDrag;
        _isSeekDragging = false;
        _resumeAfterSeekDrag = false;
        _playbackService.SeekToPercentage((float)fraction);
        HoldDisplayedSeek(fraction);
        RefreshPlaybackProgress();
        if (keepHintVisible)
        {
            ShowSeekHint(fraction);
        }
        else
        {
            IsSeekHintVisible = false;
        }

        if (resume)
        {
            _ = _playbackService.PlayAsync();
        }
    }

    /// <summary>
    /// Copies the current playback position and duration into the display strings and progress fill.
    /// A drag in progress keeps the preview, and a just-released seek keeps its target until the player reports it.
    /// </summary>
    private void RefreshPlaybackProgress()
    {
        if (_isSeekDragging)
        {
            return;
        }

        TimeSpan position = ResolveDisplayedPosition(_playbackService.Position);
        TimeSpan duration = _playbackService.Duration;
        CurrentPositionText = FormatPlaybackTime(position);
        TotalDurationText = FormatPlaybackTime(duration);

        double progress = duration <= TimeSpan.Zero
            ? 0d
            : position.TotalSeconds / duration.TotalSeconds * 100d;
        PlaybackProgress = Math.Clamp(progress, 0d, 100d);
    }

    /// <summary>
    /// Writes the dragged fraction into the progress bar, the position label, and the time hint.
    /// </summary>
    /// <param name="fraction">Point on the progress bar, where zero is the start and one is the end.</param>
    private void ApplyDraggedProgress(double fraction)
    {
        fraction = ClampUnit(fraction);
        TimeSpan position = PositionAtFraction(fraction);
        CurrentPositionText = FormatPlaybackTime(position);
        PlaybackProgress = fraction * 100d;
        SeekHintText = FormatPlaybackTime(position);
        IsSeekHintVisible = true;
    }

    /// <summary>
    /// Remembers <paramref name="fraction"/> so the bar does not snap back before the player reports the seek.
    /// </summary>
    /// <param name="fraction">Point on the progress bar that was just requested.</param>
    private void HoldDisplayedSeek(double fraction)
    {
        TimeSpan duration = _playbackService.Duration;
        if (duration <= TimeSpan.Zero)
        {
            _heldSeekPosition = null;
            return;
        }

        _heldSeekPosition = TimeSpan.FromTicks((long)Math.Round(duration.Ticks * ClampUnit(fraction)));
        _heldSeekUntilTicks = Environment.TickCount64 + SeekDisplayHoldMilliseconds;
    }

    /// <summary>
    /// Returns the seek preview while the player position is still far from the requested point.
    /// </summary>
    /// <param name="reportedPosition">Position currently reported by the audio player.</param>
    private TimeSpan ResolveDisplayedPosition(TimeSpan reportedPosition)
    {
        if (_heldSeekPosition is not TimeSpan held)
        {
            return reportedPosition;
        }

        bool holdExpired = Environment.TickCount64 >= _heldSeekUntilTicks;
        bool playerCaughtUp = Math.Abs((reportedPosition - held).TotalSeconds) <= SeekDisplayHoldToleranceSeconds;
        if (holdExpired || playerCaughtUp)
        {
            _heldSeekPosition = null;
            return reportedPosition;
        }

        return held;
    }

    /// <summary>
    /// Returns the playback time at <paramref name="fraction"/> of the current duration.
    /// </summary>
    /// <param name="fraction">Point on the progress bar, where zero is the start and one is the end.</param>
    private TimeSpan PositionAtFraction(double fraction)
    {
        TimeSpan duration = _playbackService.Duration;
        if (duration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromTicks((long)Math.Round(duration.Ticks * ClampUnit(fraction)));
    }

    /// <summary>
    /// Clamps <paramref name="fraction"/> to the progress bar.
    /// </summary>
    /// <param name="fraction">Unclamped pointer fraction.</param>
    private static double ClampUnit(double fraction)
    {
        return Math.Clamp(fraction, 0d, 1d);
    }

    /// <summary>
    /// Formats a playback time in whole seconds as m:ss, or h:mm:ss once an hour has passed.
    /// </summary>
    /// <param name="time">Time reported by the audio player.</param>
    private static string FormatPlaybackTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
        {
            time = TimeSpan.Zero;
        }

        long totalSeconds = (long)time.TotalSeconds;
        long hours = totalSeconds / 3600;
        long minutes = totalSeconds % 3600 / 60;
        long seconds = totalSeconds % 60;
        if (hours > 0)
        {
            return $"{hours}:{minutes:00}:{seconds:00}";
        }

        return $"{minutes}:{seconds:00}";
    }

    /// <summary>
    /// Handles updates to the queue service state.
    /// Automatic skip raises this off the UI thread, so the refresh is posted before it touches the list.
    /// </summary>
    private void OnQueueStateChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _ = RefreshAsync();
            return;
        }

        Dispatcher.UIThread.Post(() => _ = RefreshAsync());
    }

    /// <summary>
    /// Asynchronously refreshes queue sidebar items and active track metadata from the database.
    /// Overlapping refreshes run one at a time.
    /// </summary>
    public async Task RefreshAsync()
    {
        await _refreshGate.WaitAsync().ConfigureAwait(true);
        try
        {
            await RefreshCoreAsync().ConfigureAwait(true);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    /// <summary>
    /// Loads queue labels from the database and applies them on the UI thread.
    /// </summary>
    private async Task RefreshCoreAsync()
    {
        IReadOnlyList<QueueItem> rawItems = _queueService.Items;
        int activeNibbleIdx = _queueService.CurrentNibbleIndex;
        int activeMovementIdx = _queueService.CurrentMovementIndex;
        bool isEmptyQueue = rawItems.Count == 0;

        List<PlayerQueueItemViewModel> newQueueVms = new();

        for (int nibbleIdx = 0; nibbleIdx < rawItems.Count; nibbleIdx++)
        {
            QueueItem item = rawItems[nibbleIdx];
            Work? work = await _unitOfWork.Works.GetByIdAsync(item.Nibble.WorkId).ConfigureAwait(true);
            Recording? recording = await _unitOfWork.Recordings.GetByIdAsync(item.Nibble.RecordingId).ConfigureAwait(true);

            string workTitle = work?.Name ?? $"Work #{item.Nibble.WorkId}";
            string subtitle = FormatConductorAndEnsemble(recording);

            PlayerQueueItemViewModel queueVm = new(
                nibbleIdx,
                workTitle,
                subtitle,
                OnSelectNibble);

            for (int movementIdx = 0; movementIdx < item.Movements.Count; movementIdx++)
            {
                NibbleMovement nm = item.Movements[movementIdx];
                Movement? mv = await _unitOfWork.Movements.GetByIdAsync(nm.MovementId).ConfigureAwait(true);

                int displayOrder = mv?.Order ?? nm.Order;
                string ordinalLabel = FormatRomanOrdinal(displayOrder);
                string mvName = mv?.Name ?? $"Movement #{nm.MovementId}";
                bool isPlaying = nibbleIdx == activeNibbleIdx && movementIdx == activeMovementIdx;

                PlayerMovementItemViewModel movementVm = new(
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

        QueueItem? activeItem = _queueService.CurrentItem;
        NibbleMovement? activeMovement = _queueService.CurrentMovement;
        string currentWorkTitle = "No Track Queued";
        string currentMovementOrdinal = "";
        string currentMovementName = "Add items to queue to begin";
        string currentComposerName = "—";
        string currentPerformersLine = "—";
        string currentRecordingDetailsLine = "—";

        if (activeItem != null)
        {
            Work? activeWork = await _unitOfWork.Works.GetByIdAsync(activeItem.Nibble.WorkId).ConfigureAwait(true);
            Recording? activeRec = await _unitOfWork.Recordings.GetByIdAsync(activeItem.Nibble.RecordingId).ConfigureAwait(true);
            Composer? activeComposer = activeWork != null
                ? await _unitOfWork.Composers.GetByIdAsync(activeWork.ComposerId).ConfigureAwait(true)
                : null;
            Movement? activeMv = activeMovement != null
                ? await _unitOfWork.Movements.GetByIdAsync(activeMovement.MovementId).ConfigureAwait(true)
                : null;

            currentWorkTitle = activeWork?.Name ?? "—";
            currentMovementOrdinal = activeMv != null
                ? FormatRomanOrdinal(activeMv.Order)
                : (activeMovement != null ? FormatRomanOrdinal(activeMovement.Order) : "");
            currentMovementName = activeMv?.Name ?? "—";
            currentComposerName = activeComposer?.Name ?? "—";
            currentPerformersLine = FormatPerformersLine(activeRec);
            currentRecordingDetailsLine = FormatRecordingDetails(activeRec);
        }

        Action apply = () =>
        {
            IsEmptyQueue = isEmptyQueue;
            QueueItems.Clear();
            foreach (PlayerQueueItemViewModel vm in newQueueVms)
            {
                QueueItems.Add(vm);
            }

            CurrentWorkTitle = currentWorkTitle;
            CurrentMovementOrdinal = currentMovementOrdinal;
            CurrentMovementName = currentMovementName;
            CurrentComposerName = currentComposerName;
            CurrentPerformersLine = currentPerformersLine;
            CurrentRecordingDetailsLine = currentRecordingDetailsLine;
        };

        if (Dispatcher.UIThread.CheckAccess())
        {
            apply();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(apply);
    }

    /// <summary>
    /// Invoked when a nibble container is clicked in the sidebar.
    /// </summary>
    private void OnSelectNibble(int nibbleIndex)
    {
        _ = _playbackService.PlayAtAsync(nibbleIndex, 0);
    }

    /// <summary>
    /// Invoked when a movement row is clicked in the sidebar.
    /// </summary>
    private void OnSelectMovement(int nibbleIndex, int movementIndex)
    {
        _ = _playbackService.PlayAtAsync(nibbleIndex, movementIndex);
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
