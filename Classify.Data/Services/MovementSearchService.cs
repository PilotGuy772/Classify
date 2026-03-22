using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Data.Services;

public class MovementSearchService(IUnitOfWork uow) : IEntitySearchService
{
    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        IEnumerable<Movement> movements = await uow.Movements.FindByNameAsync(query, 15, ct);
        return movements.Select(m => new SearchResult { Entity = m, DisplayText = $"{m.Order}. {m.Name}" }).ToList();
    }
}

