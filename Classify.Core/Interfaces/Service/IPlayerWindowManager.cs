namespace Classify.Core.Interfaces.Service;

/// <summary>
/// Service contract for managing the standalone PlayerWindow lifecycle and auto-open behavior on queue updates.
/// </summary>
public interface IPlayerWindowManager
{
    /// <summary>
    /// Starts monitoring queue events to automatically open the player window when items are queued.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Explicitly opens or brings the player window to focus.
    /// </summary>
    void ShowPlayerWindow();
}
