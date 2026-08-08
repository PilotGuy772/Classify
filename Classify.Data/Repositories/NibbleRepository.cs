using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Nibble"/> entities in the database context.
/// </summary>
/// <param name="context">The database context.</param>
public class NibbleRepository(ClassifyContext context) : Repository<Nibble>(context), INibbleRepository
{
    /// <summary>
    /// Gets all nibbles associated with a specific work ID.
    /// </summary>
    /// <param name="workId">The ID of the work.</param>
    /// <returns>A collection of matching nibbles.</returns>
    public async Task<IEnumerable<Nibble>> GetByWorkIdAsync(int workId)
    {
        return await DbSet.AsNoTracking()
            .Where(n => n.WorkId == workId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all nibbles associated with a specific recording ID.
    /// </summary>
    /// <param name="recordingId">The ID of the recording.</param>
    /// <returns>A collection of matching nibbles.</returns>
    public async Task<IEnumerable<Nibble>> GetByRecordingIdAsync(int recordingId)
    {
        return await DbSet.AsNoTracking()
            .Where(n => n.RecordingId == recordingId)
            .ToListAsync();
    }
}
