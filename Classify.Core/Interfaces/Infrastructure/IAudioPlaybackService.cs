using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Infrastructure;

/// <summary>
/// Plays local audio files. The service keeps at most one file loaded.
/// </summary>
public interface IAudioPlaybackService
{
    /// <summary>
    /// Occurs when playback starts or stops.
    /// </summary>
    event EventHandler? PlaybackStateChanged;

    /// <summary>
    /// Gets a value indicating whether audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Gets a value indicating whether a file is loaded.
    /// </summary>
    bool HasMedia { get; }

    /// <summary>
    /// Gets the current playback position. Returns <see cref="TimeSpan.Zero"/> when no file is loaded.
    /// </summary>
    TimeSpan Position { get; }

    /// <summary>
    /// Gets the duration of the loaded file. Returns <see cref="TimeSpan.Zero"/> when no file is loaded or the duration is unknown.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Determines whether the file exists and contains a playable audio track.
    /// </summary>
    /// <param name="path">Full path of the file to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel inspection.</param>
    /// <returns><see langword="true"/> when the file has at least one audio track; otherwise <see langword="false"/>.</returns>
    Task<bool> IsPlayableAudioFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads playback details for a local audio file.
    /// </summary>
    /// <param name="path">Full path of the file to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel inspection.</param>
    /// <returns>The file details, or <see langword="null"/> when the file is missing or has no playable audio track.</returns>
    Task<AudioPlaybackInfo?> GetAudioFileInfoAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a file so it can be played. Replaces any file that is already loaded.
    /// </summary>
    /// <param name="path">Full path of the audio file.</param>
    void Load(string path);

    /// <summary>
    /// Starts or resumes playback of the loaded file.
    /// </summary>
    void Play();

    /// <summary>
    /// Moves playback to the given position. A seek made before playback starts is applied when playback starts.
    /// </summary>
    /// <param name="position">Position from the start of the file.</param>
    void Seek(TimeSpan position);

    /// <summary>
    /// Pauses playback of the loaded file.
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops playback and unloads the current file.
    /// </summary>
    void Stop();
}
