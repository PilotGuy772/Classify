using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IMovementRepository : IRepository<Movement>
{
    public Task<IEnumerable<Movement>> GetMovementsByWorkIdAsync(int id);
    
    /// <summary>
    /// Find movements by free-text query against name.
    /// </summary>
    Task<IEnumerable<Movement>> FindByNameAsync(string query, int limit = 15, CancellationToken ct = default);
}