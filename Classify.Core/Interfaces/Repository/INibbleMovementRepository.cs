using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

/// <summary>
/// Defines the repository contract for managing <see cref="NibbleMovement"/> domain entities.
/// </summary>
public interface INibbleMovementRepository : IRepository<NibbleMovement>
{
    /// <summary>
    /// Gets all nibble movement entries for a specific nibble ID, ordered by <see cref="NibbleMovement.Order"/>.
    /// </summary>
    /// <param name="nibbleId">The ID of the nibble.</param>
    /// <returns>An ordered collection of matching nibble movements.</returns>
    Task<IEnumerable<NibbleMovement>> GetByNibbleIdAsync(int nibbleId);

    /// <summary>
    /// Gets all nibble movement entries associated with a specific movement ID.
    /// </summary>
    /// <param name="movementId">The ID of the movement.</param>
    /// <returns>A collection of matching nibble movements.</returns>
    Task<IEnumerable<NibbleMovement>> GetByMovementIdAsync(int movementId);
}
