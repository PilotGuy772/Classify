using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IAudioFileRepository : IRepository<AudioFile>
{
    /// <summary>
    /// Get all audio files associated with this Recording, one for each Movement.
    /// </summary>
    /// <param name="id">Recording ID</param>
    /// <returns>All Audio Files from this Recording, each associated with a single Movement.</returns>
    // public Task<IEnumerable<AudioFile>> GetAudioFilesByRecordingIdAsync(int id);
    // public Task<IEnumerable<AudioFile>> GetAudioFilesByMovementIdAsync(int id);
    public Task<AudioFile?> GetAudioFileByPath(string path);
}