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
}
