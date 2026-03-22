using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Data.Services;

public class WorkSearchService(IUnitOfWork uow) : IEntitySearchService
{
    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        IEnumerable<Work> works = await uow.Works.FindByTitleOrCatalogAsync(query, 15, ct);
        return works.Select(w => new SearchResult { Entity = w, DisplayText = $"{w.Name} — {w.CatalogNumber}" }).ToList();
    }
}

