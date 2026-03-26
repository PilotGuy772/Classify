using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

public class AudioFileRepository(ClassifyContext context) : Repository<AudioFile>(context), IAudioFileRepository
{
    // public async Task<IEnumerable<AudioFile>> GetAudioFilesByRecordingIdAsync(int id)
    // {
    //     return await DbSet.AsNoTracking()
    //         .Where(a => a.RecordingId == id)
    //         .ToListAsync();
    // }
    //
    // public async Task<IEnumerable<AudioFile>> GetAudioFilesByMovementIdAsync(int id)
    // {
    //     return await DbSet.AsNoTracking()
    //         .Where(a => a.MovementId == id)
    //         .ToListAsync();
    // }

    public async Task<AudioFile?> GetAudioFileByPath(string path)
    {
        return await DbSet.FirstOrDefaultAsync(a => a.Path.Equals(path));
    }

    public async Task<IReadOnlyList<AudioFile>> GetByIdsOrderedAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return Array.Empty<AudioFile>();

        List<AudioFile> files = await DbSet.AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync(ct);

        Dictionary<int, AudioFile> byId = files.ToDictionary(a => a.Id);

        List<AudioFile> ordered = new(ids.Count);
        foreach (int id in ids)
        {
            if (byId.TryGetValue(id, out AudioFile? file))
                ordered.Add(file);
        }

        return ordered;
    }
}