using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Classify.Core.Enums;
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

    public override async Task<ProposedMatch> AddAsync(ProposedMatch entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // Ensure the related AudioFile is loaded in the current context
        AudioFile? audioFile = await Context.Set<AudioFile>().FindAsync(entity.AudioFileId);
        if (audioFile == null)
        {
            throw new InvalidOperationException($"AudioFile with id {entity.AudioFileId} not found when adding ProposedMatch.");
        }

        // Determine new status according to rules
        if (Math.Abs(entity.ConfidenceScore - 1.0f) < 0.0001f)
        {
            // Confidence == 1.0 -> UserConfirmed (do not downgrade)
            if (audioFile.Status != IngestionStatus.UserConfirmed)
            {
                audioFile.Status = IngestionStatus.UserConfirmed;
                Context.Set<AudioFile>().Update(audioFile);
            }
        }
        else
        {
            // If currently Seen, move to MatchProposed
            if (audioFile.Status == IngestionStatus.Seen)
            {
                audioFile.Status = IngestionStatus.MatchProposed;
                Context.Set<AudioFile>().Update(audioFile);
            }
        }

        // Add the proposed match
        await DbSet.AddAsync(entity);
        return entity;
    }
}