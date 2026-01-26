using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

public class MovementRepository(ClassifyContext context) : Repository<Movement>(context), IMovementRepository
{
    public async Task<IEnumerable<Movement>> GetMovementsByWorkIdAsync(int id)
    {
        return await DbSet.AsNoTracking()
            .Where(m => m.WorkId == id)
            .ToListAsync();
    }
    
}