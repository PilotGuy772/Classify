using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Service;

/// <summary>
/// Service for scanning audio files from a source.
/// </summary>
public interface IAudioFileScanner
{
    /// <summary>
    /// Finds audio and video files in the given directory non-recursively.
    /// </summary>
    /// <returns>A list of AudioFiles found in the directory.</returns>
    public Task<IEnumerable<AudioFile>> ScanAudioFilesAsync(string path);
}