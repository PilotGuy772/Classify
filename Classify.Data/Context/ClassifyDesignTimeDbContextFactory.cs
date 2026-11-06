using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Classify.Data.Context;

/// <summary>
/// Design-time factory for creating <see cref="ClassifyContext"/> instances during EF Core migration operations.
/// </summary>
public class ClassifyDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClassifyContext>
{
    /// <summary>
    /// Creates a new <see cref="ClassifyContext"/> instance configured for design-time tooling.
    /// </summary>
    /// <param name="args">Arguments provided by the design-time tool.</param>
    /// <returns>A new <see cref="ClassifyContext"/> instance.</returns>
    public ClassifyContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ClassifyContext> optionsBuilder = new();
        optionsBuilder.UseSqlite("Data Source=library.db");

        return new ClassifyContext(optionsBuilder.Options);
    }
}
