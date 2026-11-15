using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Services.Playback;

/// <summary>
/// Plays queue movements by resolving each movement to an audio file and sending it to the audio player.
/// </summary>
public sealed class PlaybackService : IPlaybackService
{
    private static readonly TimeSpan RestartThreshold = TimeSpan.FromSeconds(5);

    private readonly IQueueService _queueService;
    private readonly IAudioPlaybackService _audioPlaybackService;
    private readonly IAppSettings _appSettings;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackService"/> class.
    /// </summary>
    /// <param name="queueService">Queue that supplies the current nibble and movement.</param>
    /// <param name="audioPlaybackService">Player that loads and plays audio files.</param>
    /// <param name="appSettings">Application settings used to resolve library-relative audio paths.</param>
    /// <param name="unitOfWorkFactory">Creates a unit of work for each audio-file lookup.</param>
    public PlaybackService(
        IQueueService queueService,
        IAudioPlaybackService audioPlaybackService,
        IAppSettings appSettings,
        Func<IUnitOfWork> unitOfWorkFactory)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _audioPlaybackService = audioPlaybackService ?? throw new ArgumentNullException(nameof(audioPlaybackService));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
        _audioPlaybackService.PlaybackStateChanged += OnAudioPlaybackStateChanged;
    }

    /// <inheritdoc />
    public event EventHandler? PlaybackStateChanged;

    /// <inheritdoc />
    public bool IsPlaying => _audioPlaybackService.IsPlaying;

    /// <inheritdoc />
    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_queueService.CurrentMovement is null)
            {
                return;
            }

            if (!_audioPlaybackService.HasMedia)
            {
                await TryPlayCurrentAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            _audioPlaybackService.Play();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Pause()
    {
        _gate.Wait();
        try
        {
            if (_audioPlaybackService.HasMedia)
            {
                _audioPlaybackService.Pause();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        _gate.Wait();
        try
        {
            _audioPlaybackService.Stop();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SkipForwardAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _audioPlaybackService.Stop();
            while (_queueService.Next())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await TryPlayCurrentAsync(cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SkipBackwardAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            bool nearStart = _audioPlaybackService.Position < RestartThreshold;
            if (!nearStart || await IsFirstPlayableAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_audioPlaybackService.HasMedia)
                {
                    _audioPlaybackService.Seek(TimeSpan.Zero);
                }

                return;
            }

            _audioPlaybackService.Stop();
            while (_queueService.Previous())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await TryPlayCurrentAsync(cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Seek(int seconds)
    {
        _gate.Wait();
        try
        {
            if (!_audioPlaybackService.HasMedia)
            {
                return;
            }

            TimeSpan position = _audioPlaybackService.Position + TimeSpan.FromSeconds(seconds);
            if (position < TimeSpan.Zero)
            {
                position = TimeSpan.Zero;
            }

            TimeSpan duration = _audioPlaybackService.Duration;
            if (duration > TimeSpan.Zero && position > duration)
            {
                position = duration;
            }

            _audioPlaybackService.Seek(position);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void SeekToPercentage(float percentage)
    {
        if (percentage < 0f || percentage > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between zero and one.");
        }

        _gate.Wait();
        try
        {
            if (!_audioPlaybackService.HasMedia)
            {
                return;
            }

            long ticks = (long)(_audioPlaybackService.Duration.Ticks * (double)percentage);
            _audioPlaybackService.Seek(TimeSpan.FromTicks(ticks));
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Forwards a playback-state change from the audio player.
    /// </summary>
    private void OnAudioPlaybackStateChanged(object? sender, EventArgs e)
    {
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Loads and plays the queue's current movement when it has an audio file.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns><see langword="true"/> when playback started.</returns>
    private async Task<bool> TryPlayCurrentAsync(CancellationToken cancellationToken)
    {
        QueueItem? item = _queueService.CurrentItem;
        NibbleMovement? movement = _queueService.CurrentMovement;
        if (item is null || movement is null)
        {
            return false;
        }

        string? path = await ResolveAudioPathAsync(item, movement, cancellationToken).ConfigureAwait(false);
        if (path is null)
        {
            return false;
        }

        _audioPlaybackService.Load(path);
        _audioPlaybackService.Play();
        return true;
    }

    /// <summary>
    /// Determines whether the current movement is the first queued movement that has an audio file.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns><see langword="true"/> when no earlier movement has an audio file.</returns>
    private async Task<bool> IsFirstPlayableAsync(CancellationToken cancellationToken)
    {
        int currentNibbleIndex = _queueService.CurrentNibbleIndex;
        int currentMovementIndex = _queueService.CurrentMovementIndex;
        if (currentNibbleIndex < 0 || currentMovementIndex < 0)
        {
            return true;
        }

        IReadOnlyList<QueueItem> items = _queueService.Items;
        using IUnitOfWork unitOfWork = _unitOfWorkFactory();
        for (int nibbleIndex = 0; nibbleIndex < items.Count; nibbleIndex++)
        {
            QueueItem item = items[nibbleIndex];
            for (int movementIndex = 0; movementIndex < item.Movements.Count; movementIndex++)
            {
                string? path = await ResolveAudioPathAsync(unitOfWork, item, item.Movements[movementIndex], cancellationToken)
                    .ConfigureAwait(false);
                if (path is null)
                {
                    continue;
                }

                return nibbleIndex == currentNibbleIndex && movementIndex == currentMovementIndex;
            }
        }

        return true;
    }

    /// <summary>
    /// Resolves the full path of the audio file for a queued movement.
    /// </summary>
    /// <param name="item">Queue item that owns the movement.</param>
    /// <param name="movement">Movement to resolve.</param>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns>The full audio path, or <see langword="null"/> when no playable file exists.</returns>
    private async Task<string?> ResolveAudioPathAsync(QueueItem item, NibbleMovement movement, CancellationToken cancellationToken)
    {
        using IUnitOfWork unitOfWork = _unitOfWorkFactory();
        return await ResolveAudioPathAsync(unitOfWork, item, movement, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the full path of the audio file for a queued movement.
    /// </summary>
    /// <param name="unitOfWork">Database session used for the lookup.</param>
    /// <param name="item">Queue item that owns the movement.</param>
    /// <param name="movement">Movement to resolve.</param>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns>The full audio path, or <see langword="null"/> when no playable file exists.</returns>
    private async Task<string?> ResolveAudioPathAsync(
        IUnitOfWork unitOfWork,
        QueueItem item,
        NibbleMovement movement,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<PerformedMovement> performedMovements = await unitOfWork.PerformedMovements
            .GetByRecordingId(item.Nibble.RecordingId)
            .ConfigureAwait(false);

        PerformedMovement? match = null;
        foreach (PerformedMovement performedMovement in performedMovements)
        {
            if (performedMovement.MovementId != movement.MovementId)
            {
                continue;
            }

            match = performedMovement;
            if (performedMovement.Order == movement.Order)
            {
                break;
            }
        }

        if (match is null)
        {
            return null;
        }

        AudioFile? audioFile = await unitOfWork.AudioFiles.GetByIdAsync(match.AudioFileId).ConfigureAwait(false);
        if (audioFile is null)
        {
            return null;
        }

        string fullPath = audioFile.GetFullPath(_appSettings);
        return File.Exists(fullPath) ? fullPath : null;
    }
}
