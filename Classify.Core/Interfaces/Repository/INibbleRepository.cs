using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

/// <summary>
/// Defines the repository contract for managing <see cref="Nibble"/> domain entities.
/// </summary>
public interface INibbleRepository : IRepository<Nibble>
{
    /// <summary>
    /// Gets all nibbles associated with a specific work ID.
    /// </summary>
    /// <param name="workId">The ID of the work.</param>
    /// <returns>A collection of matching nibbles.</returns>
    Task<IEnumerable<Nibble>> GetByWorkIdAsync(int workId);

    /// <summary>
    /// Gets all nibbles associated with a specific recording ID.
    /// </summary>
    /// <param name="recordingId">The ID of the recording.</param>
    /// <returns>A collection of matching nibbles.</returns>
    Task<IEnumerable<Nibble>> GetByRecordingIdAsync(int recordingId);
}
