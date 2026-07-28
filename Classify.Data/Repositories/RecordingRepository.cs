using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

public class RecordingRepository(ClassifyContext context) : Repository<Recording>(context), IRecordingRepository
{
    public async Task<IEnumerable<Recording>> GetRecordingsByWorkIdAsync(int id) => 
        await DbSet.AsNoTracking()
            .Where(r => r.WorkId == id)
            .ToListAsync();

    public async Task<IEnumerable<Recording>> FindByTextAsync(string query, int limit = 15, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<Recording>();
        string q = query.Trim();
        return await DbSet.AsNoTracking()
            .Where(r => EF.Functions.Like(r.Conductor, $"%{q}%"))
            .OrderBy(r => r.Conductor)
            .Take(limit)
            .ToListAsync(ct);
    }
}