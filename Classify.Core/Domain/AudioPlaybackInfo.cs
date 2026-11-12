namespace Classify.Core.Domain;

/// <summary>
/// Describes a local file that can be played as audio.
/// </summary>
public sealed class AudioPlaybackInfo
{
    /// <summary>
    /// Gets the full path of the file.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the audio codec name reported by the player, such as FLAC or MPEG Audio layer 3.
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// Gets the container format, such as MP4, Ogg, or FLAC.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// Gets the audio bitrate in bits per second. Zero when the file does not report a bitrate.
    /// </summary>
    public required long Bitrate { get; init; }

    /// <summary>
    /// Gets the duration of the audio. <see cref="TimeSpan.Zero"/> when the duration is unknown.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the sample rate in hertz.
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Gets the number of audio channels.
    /// </summary>
    public required int Channels { get; init; }
}
