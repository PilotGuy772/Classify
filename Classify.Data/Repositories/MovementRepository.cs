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
    public async Task<IEnumerable<Movement>> FindByNameAsync(string query, int limit = 15, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        string q = query.Trim();
        return await DbSet.AsNoTracking()
            .Where(m => EF.Functions.Like(m.Name, $"%{q}%"))
            .OrderBy(m => m.Order)
            .Take(limit)
            .ToListAsync(ct);
    }
}