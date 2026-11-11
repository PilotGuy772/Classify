using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Classify.Core.Interfaces.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Context;

/// <summary>
/// Factory for creating <see cref="ClassifyContext"/> instances configured with the dynamic database path from application settings.
/// </summary>
public class ClassifyDbContextFactory : IDbContextFactory<ClassifyContext>
{
    private readonly IAppSettings _appSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassifyDbContextFactory"/> class.
    /// </summary>
    /// <param name="appSettings">The application settings providing the database path configuration.</param>
    public ClassifyDbContextFactory(IAppSettings appSettings)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    /// <summary>
    /// Creates a new <see cref="ClassifyContext"/> instance using the current database path.
    /// </summary>
    /// <returns>A configured <see cref="ClassifyContext"/> instance.</returns>
    public ClassifyContext CreateDbContext()
    {
        string databasePath;
        if (!string.IsNullOrWhiteSpace(_appSettings.DatabasePath))
        {
            databasePath = _appSettings.DatabasePath;
        }
        else if (!string.IsNullOrWhiteSpace(_appSettings.CurrentLibraryPath))
        {
            databasePath = Path.Combine(_appSettings.CurrentLibraryPath, "library.db");
        }
        else
        {
            databasePath = "library.db";
        }

        string? directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DbContextOptionsBuilder<ClassifyContext> optionsBuilder = new();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");

        return new ClassifyContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Asynchronously creates a new <see cref="ClassifyContext"/> instance using the current database path.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the context creation operation containing the configured context.</returns>
    public Task<ClassifyContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}
