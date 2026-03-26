using Classify.Core.Interfaces.Service;
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

    public async Task<PlayableResolution> GetPlayableResolutionByIdAsync(int recordingId, CancellationToken ct = default)
    {
        var header = await (from r in DbSet.AsNoTracking()
                            where r.Id == recordingId
                            join w in Context.Works.AsNoTracking() on r.WorkId equals w.Id into wg
                            from w in wg.DefaultIfEmpty()
                            select new
                            {
                                r.Id,
                                r.Conductor,
                                WorkName = w != null ? w.Name : null
                            }).SingleAsync(ct);

        List<int> audioIds = await Context.Set<PerformedMovement>().AsNoTracking()
            .Where(pm => pm.RecordingId == recordingId)
            .OrderBy(pm => pm.Order)
            .Select(pm => pm.AudioFileId)
            .ToListAsync(ct);

        string conductor = header.Conductor ?? string.Empty;
        string displayName =
            !string.IsNullOrWhiteSpace(header.WorkName) && !string.IsNullOrWhiteSpace(conductor)
                ? $"{header.WorkName} — {conductor}"
                : !string.IsNullOrWhiteSpace(header.WorkName)
                    ? header.WorkName
                    : !string.IsNullOrWhiteSpace(conductor)
                        ? conductor
                        : $"Recording #{header.Id}";

        return new PlayableResolution
        {
            DisplayName = displayName,
            PhotoKey = null,
            OrderedAudioFileIds = audioIds
        };
    }

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