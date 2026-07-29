using System.Threading;
using System.Threading.Tasks;
using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Service;

/// <summary>
/// Service contract for constructing playable <see cref="Nibble"/> objects and their <see cref="QueueItem"/> representations from domain entity identifiers.
/// </summary>
public interface INibbleBuilderService
{
    /// <summary>
    /// Constructs a <see cref="QueueItem"/> for a work, choosing its favorite recording or falling back to the first recording by conductor name alphabetically, with all movements ordered.
    /// </summary>
    /// <param name="workId">The work identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The constructed queue item, or <c>null</c> if the work or a recording is not found.</returns>
    Task<QueueItem?> BuildForWorkAsync(int workId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs a <see cref="QueueItem"/> for a specific recording and all movements of its work ordered.
    /// </summary>
    /// <param name="recordingId">The recording identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The constructed queue item, or <c>null</c> if the recording is not found.</returns>
    Task<QueueItem?> BuildForRecordingAsync(int recordingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs a <see cref="QueueItem"/> for a single movement, resolving a specified recording or falling back to the work's favorite/alphabetical recording.
    /// </summary>
    /// <param name="movementId">The movement identifier.</param>
    /// <param name="recordingId">Optional explicit recording identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The constructed queue item, or <c>null</c> if the movement or recording is not found.</returns>
    Task<QueueItem?> BuildForMovementAsync(int movementId, int? recordingId = null, CancellationToken cancellationToken = default);
}
