namespace Classify.Desktop.ViewModels;

/// <summary>
/// Specifies the type of an item in the library browser.
/// </summary>
public enum LibraryItemType
{
    /// <summary>
    /// A composer entity.
    /// </summary>
    Composer,

    /// <summary>
    /// A musical composition/work.
    /// </summary>
    Work,

    /// <summary>
    /// A movement of a work.
    /// </summary>
    Movement,

    /// <summary>
    /// A recording of a work.
    /// </summary>
    Recording,

    /// <summary>
    /// An audio file.
    /// </summary>
    AudioFile,

    /// <summary>
    /// A performed movement recording.
    /// </summary>
    MovementRecording
}
