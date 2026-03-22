using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Data.Services;

public class RecordingSearchService(IUnitOfWork uow) : IEntitySearchService
{
    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        IEnumerable<Recording> recs = await uow.Recordings.FindByTextAsync(query, 15, ct);
        return recs.Select(r => new SearchResult { Entity = r, DisplayText = $"Conductor: {r.Conductor}" }).ToList();
    }
}

