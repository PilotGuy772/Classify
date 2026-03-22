using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IWorkRepository : IRepository<Work>
{
    public Task<IEnumerable<Work>> GetWorksByComposerIdAsync(int id);
    
    /// <summary>
    /// Find works by free-text query against title or catalog number.
    /// </summary>
    Task<IEnumerable<Work>> FindByTitleOrCatalogAsync(string query, int limit = 15, CancellationToken ct = default);
}