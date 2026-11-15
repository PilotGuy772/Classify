using System.Collections.Frozen;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using LibVLCSharp.Shared;

namespace Classify.Infrastructure;

/// <summary>
/// Plays one local audio file at a time through LibVLC.
/// </summary>
public sealed class AudioPlaybackService : IAudioPlaybackService, IDisposable
{
    private const int ParseTimeoutMilliseconds = 10_000;

    private static readonly FrozenDictionary<string, string> ContainersByExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = "MPEG Audio",
        [".flac"] = "FLAC",
        [".wav"] = "WAV",
        [".wave"] = "WAV",
        [".aif"] = "AIFF",
        [".aiff"] = "AIFF",
        [".aac"] = "ADTS",
        [".m4a"] = "MP4",
        [".m4b"] = "MP4",
        [".mp4"] = "MP4",
        [".ogg"] = "Ogg",
        [".oga"] = "Ogg",
        [".opus"] = "Ogg",
        [".wma"] = "ASF",
        [".wv"] = "WavPack",
        [".ape"] = "Monkey's Audio",
        [".dsf"] = "DSD",
        [".dff"] = "DSDIFF",
        [".webm"] = "WebM",
        [".mkv"] = "Matroska",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly object _gate = new();
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private string? _loadedPath;
    private TimeSpan? _pendingSeek;
    private int _isPlaying;
    private long _publishedPositionSecond = -1;
    private long _publishedDurationSecond = -1;
    private int _suppressPlaybackCompleted;
    private int _mediaGeneration;
    private long _knownLengthMs = -1;
    private long _lastTimeMs = -1;
    private bool _disposed;

    /// <summary>
    /// Initializes LibVLC and a single media player for audio output.
    /// When playback time reaches the reported duration, the service raises <see cref="PlaybackCompleted"/>.
    /// LibVLC can keep the clock running past that duration, so the end is not taken from <c>EndReached</c> alone.
    /// </summary>
    public AudioPlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();
        _libVlc = new LibVLC("--no-video");
        _mediaPlayer = new MediaPlayer(_libVlc);
        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.Paused += OnPaused;
        _mediaPlayer.Stopped += OnStopped;
        _mediaPlayer.EndReached += OnEndReached;
        _mediaPlayer.EncounteredError += OnStopped;
        _mediaPlayer.TimeChanged += OnTimeChanged;
        _mediaPlayer.LengthChanged += OnLengthChanged;
    }

    /// <inheritdoc />
    public event EventHandler? PlaybackStateChanged;

    /// <inheritdoc />
    public event EventHandler? PlaybackPositionChanged;

    /// <inheritdoc />
    public event EventHandler? PlaybackCompleted;

    /// <inheritdoc />
    public bool IsPlaying => Volatile.Read(ref _isPlaying) != 0;

    /// <inheritdoc />
    public bool HasMedia
    {
        get
        {
            lock (_gate)
            {
                return _loadedPath is not null;
            }
        }
    }

    /// <inheritdoc />
    public TimeSpan Position
    {
        get
        {
            lock (_gate)
            {
                if (_loadedPath is null)
                {
                    return TimeSpan.Zero;
                }

                long time = _mediaPlayer.Time;
                return time < 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(time);
            }
        }
    }

    /// <inheritdoc />
    public TimeSpan Duration
    {
        get
        {
            lock (_gate)
            {
                if (_loadedPath is null)
                {
                    return TimeSpan.Zero;
                }

                long length = _mediaPlayer.Length;
                return length < 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(length);
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsPlayableAudioFileAsync(string path, CancellationToken cancellationToken = default)
    {
        AudioPlaybackInfo? info = await GetAudioFileInfoAsync(path, cancellationToken).ConfigureAwait(false);
        return info is not null;
    }

    /// <inheritdoc />
    public async Task<AudioPlaybackInfo?> GetAudioFileInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            return null;
        }

        ThrowIfDisposed();

        using Media media = new(_libVlc, path, FromType.FromPath);
        MediaParsedStatus status = await media.Parse(MediaParseOptions.ParseLocal, ParseTimeoutMilliseconds, cancellationToken)
            .ConfigureAwait(false);
        if (status != MediaParsedStatus.Done)
        {
            return null;
        }

        MediaTrack? audioTrack = FindAudioTrack(media);
        if (audioTrack is null)
        {
            return null;
        }

        MediaTrack selectedTrack = audioTrack.Value;
        string format = media.CodecDescription(TrackType.Audio, selectedTrack.Codec);
        if (string.IsNullOrWhiteSpace(format))
        {
            format = DescribeFourCc(selectedTrack.Codec);
        }

        TimeSpan duration = media.Duration < 0
            ? TimeSpan.Zero
            : TimeSpan.FromMilliseconds(media.Duration);

        return new AudioPlaybackInfo
        {
            Path = path,
            Format = format,
            Container = DescribeContainer(path),
            Bitrate = selectedTrack.Bitrate,
            Duration = duration,
            SampleRate = (int)selectedTrack.Data.Audio.Rate,
            Channels = (int)selectedTrack.Data.Audio.Channels,
        };
    }

    /// <inheritdoc />
    public void Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The audio file was not found.", path);
        }

        lock (_gate)
        {
            ThrowIfDisposed();
            Unload();

            Media media = new(_libVlc, path, FromType.FromPath);
            media.AddOption(":no-video");
            _mediaPlayer.Media = media;
            media.Dispose();
            _loadedPath = path;
        }
    }

    /// <inheritdoc />
    public void Play()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureLoaded();

            if (_mediaPlayer.State == VLCState.Paused)
            {
                _mediaPlayer.SetPause(false);
            }
            else if (!_mediaPlayer.IsPlaying)
            {
                bool started = _mediaPlayer.Play();
                if (!started)
                {
                    throw new InvalidOperationException("Playback could not be started.");
                }
            }

            ApplyPendingSeek();
            Interlocked.Exchange(ref _suppressPlaybackCompleted, 0);
        }
    }

    /// <inheritdoc />
    public void Seek(TimeSpan position)
    {
        if (position < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Playback position cannot be negative.");
        }

        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureLoaded();

            VLCState state = _mediaPlayer.State;
            if (state is VLCState.Playing or VLCState.Paused or VLCState.Opening or VLCState.Buffering)
            {
                _pendingSeek = null;
                _mediaPlayer.SeekTo(position);
                return;
            }

            _pendingSeek = position;
        }
    }

    /// <inheritdoc />
    public void Pause()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureLoaded();
            _mediaPlayer.SetPause(true);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            Unload();
        }
    }

    /// <summary>
    /// Stops playback and releases LibVLC.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _mediaPlayer.Playing -= OnPlaying;
            _mediaPlayer.Paused -= OnPaused;
            _mediaPlayer.Stopped -= OnStopped;
            _mediaPlayer.EndReached -= OnEndReached;
            _mediaPlayer.EncounteredError -= OnStopped;
            _mediaPlayer.TimeChanged -= OnTimeChanged;
            _mediaPlayer.LengthChanged -= OnLengthChanged;
            Unload();
            _mediaPlayer.Dispose();
            _libVlc.Dispose();
        }
    }

    /// <summary>
    /// Records that LibVLC has started playback and notifies listeners when that is a change.
    /// </summary>
    private void OnPlaying(object? sender, EventArgs e)
    {
        PublishPlaying(true);
    }

    /// <summary>
    /// Records that LibVLC has stopped playback and notifies listeners when that is a change.
    /// </summary>
    private void OnStopped(object? sender, EventArgs e)
    {
        PublishPlaying(false);
    }

    /// <summary>
    /// Notifies listeners that LibVLC reached the end of its input.
    /// Some files keep playing past the reported duration before this event, so the time check also completes playback.
    /// </summary>
    private void OnEndReached(object? sender, EventArgs e)
    {
        SignalPlaybackCompleted(Volatile.Read(ref _mediaGeneration));
    }

    /// <summary>
    /// Completes playback once the clock reaches the duration shown to the listener.
    /// </summary>
    private void TryCompleteAtReportedDuration()
    {
        long lengthMs = Volatile.Read(ref _knownLengthMs);
        long timeMs = Volatile.Read(ref _lastTimeMs);
        if (lengthMs <= 0 || timeMs < lengthMs)
        {
            return;
        }

        SignalPlaybackCompleted(Volatile.Read(ref _mediaGeneration));
    }

    /// <summary>
    /// Raises <see cref="PlaybackCompleted"/> on a worker thread so the listener can load another file.
    /// A completion for a file that has since been unloaded is ignored.
    /// </summary>
    /// <param name="generation">Media generation captured when the end was detected.</param>
    private void SignalPlaybackCompleted(int generation)
    {
        if (generation != Volatile.Read(ref _mediaGeneration))
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _suppressPlaybackCompleted, 1, 0) != 0)
        {
            return;
        }

        ThreadPool.QueueUserWorkItem(_ =>
        {
            if (generation != Volatile.Read(ref _mediaGeneration))
            {
                return;
            }

            PublishPlaying(false);
            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>
    /// Records that LibVLC has paused playback and notifies listeners when that is a change.
    /// </summary>
    private void OnPaused(object? sender, EventArgs e)
    {
        PublishPlaying(false);
    }

    /// <summary>
    /// Updates the cached playing flag and raises <see cref="PlaybackStateChanged"/> when it changes.
    /// LibVLC invokes this from its own thread, so the method does not call back into the player.
    /// </summary>
    /// <param name="isPlaying">Whether audio is now playing.</param>
    private void PublishPlaying(bool isPlaying)
    {
        int updated = isPlaying ? 1 : 0;
        int previous = Interlocked.Exchange(ref _isPlaying, updated);
        if (previous == updated)
        {
            return;
        }

        Interlocked.Exchange(ref _publishedPositionSecond, -1);
        Interlocked.Exchange(ref _publishedDurationSecond, -1);
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises <see cref="PlaybackPositionChanged"/> when the whole second of the playback position changes.
    /// </summary>
    private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        if (e.Time >= 0)
        {
            Volatile.Write(ref _lastTimeMs, e.Time);
        }

        PublishSecond(ref _publishedPositionSecond, e.Time);
        TryCompleteAtReportedDuration();
    }

    /// <summary>
    /// Raises <see cref="PlaybackPositionChanged"/> when the whole second of the duration changes.
    /// </summary>
    private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        if (e.Length > 0)
        {
            Volatile.Write(ref _knownLengthMs, e.Length);
        }

        PublishSecond(ref _publishedDurationSecond, e.Length);
        TryCompleteAtReportedDuration();
    }

    /// <summary>
    /// Publishes a timeline change when <paramref name="milliseconds"/> falls in a different whole second than last time.
    /// </summary>
    /// <param name="publishedSecond">Last second that was published for this value.</param>
    /// <param name="milliseconds">New time in milliseconds, or a negative value when the player has no time.</param>
    private void PublishSecond(ref long publishedSecond, long milliseconds)
    {
        long second = milliseconds < 0 ? 0 : milliseconds / 1000;
        long previous = Interlocked.Exchange(ref publishedSecond, second);
        if (previous == second)
        {
            return;
        }

        PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Stops the player and releases the loaded media.
    /// </summary>
    private void Unload()
    {
        Interlocked.Exchange(ref _suppressPlaybackCompleted, 1);
        Interlocked.Increment(ref _mediaGeneration);
        Volatile.Write(ref _knownLengthMs, -1);
        Volatile.Write(ref _lastTimeMs, -1);
        _pendingSeek = null;
        _mediaPlayer.Stop();

        Media? media = _mediaPlayer.Media;
        _mediaPlayer.Media = null;
        media?.Dispose();
        _loadedPath = null;
    }

    /// <summary>
    /// Applies a seek that was requested before the player had opened the file.
    /// </summary>
    private void ApplyPendingSeek()
    {
        if (_pendingSeek is not TimeSpan position)
        {
            return;
        }

        _pendingSeek = null;
        _mediaPlayer.SeekTo(position);
    }

    /// <summary>
    /// Throws when no file is loaded.
    /// </summary>
    private void EnsureLoaded()
    {
        if (_loadedPath is null)
        {
            throw new InvalidOperationException("No audio file is loaded.");
        }
    }

    /// <summary>
    /// Throws when the player has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Returns the first audio track in a parsed media, if one exists.
    /// </summary>
    /// <param name="media">Parsed media to inspect.</param>
    private static MediaTrack? FindAudioTrack(Media media)
    {
        foreach (MediaTrack track in media.Tracks)
        {
            if (track.TrackType == TrackType.Audio)
            {
                return track;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a readable container name for the file extension.
    /// </summary>
    /// <param name="path">Full path of the file.</param>
    private static string DescribeContainer(string path)
    {
        string extension = Path.GetExtension(path);
        if (ContainersByExtension.TryGetValue(extension, out string? container))
        {
            return container;
        }

        if (string.IsNullOrEmpty(extension))
        {
            return "Unknown";
        }

        return extension.TrimStart('.').ToUpperInvariant();
    }

    /// <summary>
    /// Turns a codec FourCC into a four-character label when LibVLC has no description.
    /// </summary>
    /// <param name="fourCc">Codec identifier reported by LibVLC.</param>
    private static string DescribeFourCc(uint fourCc)
    {
        char[] characters =
        [
            (char)(fourCc & 0xFF),
            (char)((fourCc >> 8) & 0xFF),
            (char)((fourCc >> 16) & 0xFF),
            (char)((fourCc >> 24) & 0xFF),
        ];

        string label = new string(characters).Trim('\0', ' ');
        return string.IsNullOrWhiteSpace(label) ? "Unknown" : label;
    }
}
