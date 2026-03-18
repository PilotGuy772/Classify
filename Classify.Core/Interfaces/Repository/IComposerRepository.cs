using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IComposerRepository : IRepository<Composer>
{
	/// <summary>
	/// Find composers whose name matches the provided query (case-insensitive contains). Limit results for performance.
	/// </summary>
	Task<IEnumerable<Composer>> FindByNameAsync(string query, int limit = 15, CancellationToken ct = default);
}
