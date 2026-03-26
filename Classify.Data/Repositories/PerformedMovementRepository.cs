using Classify.Core.Domain;
using Classify.Core.Interfaces.Service;
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

    public async Task<PlayableResolution> GetPlayableResolutionByIdAsync(int performedMovementId, CancellationToken ct = default)
    {
        var row = await (from pm in DbSet.AsNoTracking()
                         where pm.Id == performedMovementId
                         join m in Context.Movements.AsNoTracking() on pm.MovementId equals m.Id into mg
                         from m in mg.DefaultIfEmpty()
                         join r in Context.Recordings.AsNoTracking() on pm.RecordingId equals r.Id into rg
                         from r in rg.DefaultIfEmpty()
                         join w in Context.Works.AsNoTracking() on (r != null ? r.WorkId : 0) equals w.Id into wg
                         from w in wg.DefaultIfEmpty()
                         select new
                         {
                             pm.Id,
                             pm.AudioFileId,
                             MovementName = m != null ? m.Name : null,
                             Conductor = r != null ? r.Conductor : null,
                             WorkName = w != null ? w.Name : null
                         }).SingleAsync(ct);

        string displayName =
            !string.IsNullOrWhiteSpace(row.WorkName) && !string.IsNullOrWhiteSpace(row.MovementName)
                ? $"{row.WorkName} — {row.MovementName}"
                : !string.IsNullOrWhiteSpace(row.MovementName)
                    ? row.MovementName
                    : !string.IsNullOrWhiteSpace(row.WorkName)
                        ? row.WorkName
                        : $"Performed Movement #{row.Id}";

        if (!string.IsNullOrWhiteSpace(row.Conductor))
            displayName = $"{displayName} ({row.Conductor})";

        return new PlayableResolution
        {
            DisplayName = displayName,
            PhotoKey = null,
            OrderedAudioFileIds = new[] { row.AudioFileId }
        };
    }
}