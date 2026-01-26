using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IProposedMatchRepository : IRepository<ProposedMatch>
{
    Task<IEnumerable<ProposedMatch>> GetByFileIdAsync(int fileId);
    Task<ProposedMatch?> GetBestMatchForFileAsync(int fileId);
}