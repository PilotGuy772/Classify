using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

public class PerformedMovementRepository(ClassifyContext context) : Repository<PerformedMovement>(context), IPerformedMovementRepository
{
    public async Task<IEnumerable<PerformedMovement>> GetByMovementId(int id)
    {
        return await DbSet.AsNoTracking()
            .Where(pm => pm.MovementId == id)
            .ToListAsync();
    }

    public async Task<IEnumerable<PerformedMovement>> GetByRecordingId(int id)
    {
        return await DbSet.AsNoTracking()
            .Where(pm => pm.RecordingId == id)
            .ToListAsync();
    }
}