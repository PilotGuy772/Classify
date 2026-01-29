namespace Classify.Core.Interfaces.Infrastructure;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}