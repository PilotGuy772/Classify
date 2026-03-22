using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Data.Services;

public class ComposerSearchService(IUnitOfWork uow) : IEntitySearchService
{
    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<SearchResult>();

        IEnumerable<Composer> composers = await uow.Composers.FindByNameAsync(query, 25, ct);
        return composers.Select(c => new SearchResult { Entity = c, DisplayText = c.Name }).ToList();
    }
}

