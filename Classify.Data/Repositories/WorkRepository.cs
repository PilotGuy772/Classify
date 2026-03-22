using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

public class WorkRepository(ClassifyContext context) : Repository<Work>(context), IWorkRepository
{
    public async Task<IEnumerable<Work>> GetWorksByComposerIdAsync(int id)
    {
        return await DbSet.AsNoTracking()
            .Where(w => w.ComposerId == id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Work>> FindByTitleOrCatalogAsync(string query, int limit = 15, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        string q = query.Trim();
        return await DbSet.AsNoTracking()
            .Where(w => EF.Functions.Like(w.Name, $"%{q}%") || EF.Functions.Like(w.CatalogNumber, $"%{q}%"))
            .OrderBy(w => w.Name)
            .Take(limit)
            .ToListAsync(ct);
    }
}
