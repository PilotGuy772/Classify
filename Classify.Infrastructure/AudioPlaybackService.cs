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
    private int _playbackGeneration;
    private bool _disposed;

    /// <summary>
    /// Initializes LibVLC and a single media player for audio output.
    /// </summary>
    public AudioPlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();
        _libVlc = new LibVLC("--no-video");
        _mediaPlayer = new MediaPlayer(_libVlc);
        _mediaPlayer.EndReached += OnEndReached;
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
            _mediaPlayer.EndReached -= OnEndReached;
            Unload();
            _mediaPlayer.Dispose();
            _libVlc.Dispose();
        }
    }

    /// <summary>
    /// Pauses and unloads the file when playback reaches the end.
    /// LibVLC raises this on a player thread, so the player is released on another thread.
    /// The callback itself does not call back into LibVLC or take the playback lock, which would deadlock <see cref="MediaPlayer.Stop"/>.
    /// </summary>
    private void OnEndReached(object? sender, EventArgs e)
    {
        int generation = Volatile.Read(ref _playbackGeneration);
        ThreadPool.QueueUserWorkItem(_ => FinishPlayback(generation));
    }

    /// <summary>
    /// Pauses and unloads the file that reached its end, ignoring the callback when a newer file is loaded.
    /// </summary>
    /// <param name="generation">Playback generation captured when the file ended.</param>
    private void FinishPlayback(int generation)
    {
        lock (_gate)
        {
            if (_disposed || generation != _playbackGeneration)
            {
                return;
            }

            _mediaPlayer.SetPause(true);
            Unload();
        }
    }

    /// <summary>
    /// Stops the player and releases the loaded media.
    /// </summary>
    private void Unload()
    {
        Interlocked.Increment(ref _playbackGeneration);
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
