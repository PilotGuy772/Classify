using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

public class ProposedMatchRepository(ClassifyContext context) : Repository<ProposedMatch>(context), IProposedMatchRepository
{
    public async Task<IEnumerable<ProposedMatch>> GetByAudioFileIdAsync(int fileId)
    {
        return await DbSet.AsNoTracking()
            .Where(pm => pm.AudioFileId == fileId)
            .ToListAsync();
    }

    public async Task<ProposedMatch?> GetBestMatchForFileAsync(int fileId)
    {
        return await DbSet.AsNoTracking()
            .OrderByDescending(pm => pm.ConfidenceScore)
            .FirstAsync();
    }
}