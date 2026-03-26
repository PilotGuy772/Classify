namespace Classify.Core.Domain;

/// <summary>
/// Represents an entity that can be played with a media player.
/// </summary>
public interface IPlayable
{
    /// <summary>
    /// Display-ready name for UI list rows. This may be a fallback; richer names can be resolved via query services.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional key or identifier the UI can later map to an image (e.g. composer photo, album art).
    /// </summary>
    public string? PhotoKey { get; }

    /// <summary>
    /// Underlying object for info panels / navigation.
    /// </summary>
    public object BaseObject { get; }

    /// <summary>
    /// Ordered audio file IDs for this playable, when known without additional queries.
    /// Prefer resolving via a service when this returns empty.
    /// </summary>
    public IReadOnlyList<int> GetOrderedAudioFileIds();
}