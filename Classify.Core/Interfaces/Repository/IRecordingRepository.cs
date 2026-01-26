using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IRecordingRepository : IRepository<Recording>
{
    public Task<IEnumerable<Recording>> GetRecordingsByWorkIdAsync(int id);
}