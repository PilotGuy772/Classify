using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="NibbleMovement"/> entities in the database context.
/// </summary>
/// <param name="context">The database context.</param>
public class NibbleMovementRepository(ClassifyContext context) : Repository<NibbleMovement>(context), INibbleMovementRepository
{
    /// <summary>
    /// Gets all nibble movement entries for a specific nibble ID, ordered by <see cref="NibbleMovement.Order"/>.
    /// </summary>
    /// <param name="nibbleId">The ID of the nibble.</param>
    /// <returns>An ordered collection of matching nibble movements.</returns>
    public async Task<IEnumerable<NibbleMovement>> GetByNibbleIdAsync(int nibbleId)
    {
        return await DbSet.AsNoTracking()
            .Where(nm => nm.NibbleId == nibbleId)
            .OrderBy(nm => nm.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all nibble movement entries associated with a specific movement ID.
    /// </summary>
    /// <param name="movementId">The ID of the movement.</param>
    /// <returns>A collection of matching nibble movements.</returns>
    public async Task<IEnumerable<NibbleMovement>> GetByMovementIdAsync(int movementId)
    {
        return await DbSet.AsNoTracking()
            .Where(nm => nm.MovementId == movementId)
            .ToListAsync();
    }
}
