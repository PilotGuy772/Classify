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
}