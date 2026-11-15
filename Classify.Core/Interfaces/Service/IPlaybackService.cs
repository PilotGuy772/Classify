namespace Classify.Core.Interfaces.Service;

/// <summary>
/// Plays the queue's current nibble movement and moves between movements.
/// </summary>
public interface IPlaybackService
{
    /// <summary>
    /// Occurs when playback starts or stops.
    /// </summary>
    event EventHandler? PlaybackStateChanged;

    /// <summary>
    /// Occurs when the playback position or duration changes by at least one second, including after a seek.
    /// </summary>
    event EventHandler? PlaybackPositionChanged;

    /// <summary>
    /// Gets a value indicating whether audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Gets the current playback position reported by the audio player.
    /// </summary>
    TimeSpan Position { get; }

    /// <summary>
    /// Gets the duration reported by the audio player.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Plays the loaded audio file. When nothing is loaded, loads the current nibble movement and plays it.
    /// Does nothing when the queue has no current movement.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel loading the audio file.</param>
    Task PlayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses playback and leaves the audio file loaded.
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops playback and unloads the audio file.
    /// </summary>
    void Stop();

    /// <summary>
    /// Unloads the current audio file, moves to the next movement, and plays it.
    /// Moves to the next nibble when the current nibble has no further movements.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel loading the next audio file.</param>
    Task SkipForwardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the queue to the given movement and plays it.
    /// When that movement is already playing, playback continues.
    /// </summary>
    /// <param name="nibbleIndex">Zero-based index of the nibble in the queue.</param>
    /// <param name="movementIndex">Zero-based index of the movement within the nibble.</param>
    /// <param name="cancellationToken">Token used to cancel loading the audio file.</param>
    Task PlayAtAsync(int nibbleIndex, int movementIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeks to the start of the current audio file.
    /// When fewer than five seconds have played and the current movement is not the first playable queue item,
    /// unloads the current file, moves to the previous movement, and plays it from the start.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel loading the previous audio file.</param>
    Task SkipBackwardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the playback position by the given number of seconds.
    /// </summary>
    /// <param name="seconds">Seconds to add. Negative values move backward.</param>
    void Seek(int seconds);

    /// <summary>
    /// Moves the playback position to a fraction of the audio file duration.
    /// </summary>
    /// <param name="percentage">Position from zero to one.</param>
    void SeekToPercentage(float percentage);
}
