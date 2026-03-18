using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

public class ComposerRepository(ClassifyContext context) : Repository<Composer>(context), IComposerRepository
{
	public async Task<IEnumerable<Composer>> FindByNameAsync(string query, int limit = 15, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return Enumerable.Empty<Composer>();
		}

		string q = query.Trim();
		return await DbSet
			.AsNoTracking()
			.Where(c => EF.Functions.Like(c.Name, $"%{q}%"))
			.OrderBy(c => c.Name)
			.Take(limit)
			.ToListAsync(ct);
	}
}
