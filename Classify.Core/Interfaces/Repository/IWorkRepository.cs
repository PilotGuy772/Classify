using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IWorkRepository : IRepository<Work>
{
    public Task<IEnumerable<Work>> GetWorksByComposerIdAsync(int id);
}