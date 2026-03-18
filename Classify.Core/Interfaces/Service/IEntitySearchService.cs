namespace Classify.Core.Interfaces.Service;

public interface IEntitySearchService
{
    /// <summary>
    /// Search for entities by a free-text query. Implementations should return display-ready results.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken ct = default);
}

