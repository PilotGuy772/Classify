using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Data.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Services;

/// <summary>
/// Service managing database connection lifecycles, configuration updates, and switching between SQLite data sources.
/// </summary>
public class DatabaseConnectionService : IDatabaseConnectionService, IDisposable
{
    private readonly IAppSettings _appSettings;
    private readonly IDbContextFactory<ClassifyContext> _contextFactory;
    private bool _isDisposed;

    /// <summary>
    /// Occurs when the database path or data source has changed.
    /// </summary>
    public event EventHandler<string>? DatabasePathChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConnectionService"/> class.
    /// </summary>
    /// <param name="appSettings">The application settings providing configuration access.</param>
    /// <param name="contextFactory">The factory used to create database context instances.</param>
    public DatabaseConnectionService(
        IAppSettings appSettings,
        IDbContextFactory<ClassifyContext> contextFactory)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        _appSettings.DatabasePathChanged += HandleSettingsDatabasePathChanged;
    }

    /// <summary>
    /// Gets the current database connection string based on the active database path.
    /// </summary>
    /// <returns>The SQLite connection string.</returns>
    public string GetConnectionString()
    {
        return $"Data Source={GetDatabasePath()}";
    }

    /// <summary>
    /// Gets the current database file path from settings or the default path if unspecified.
    /// </summary>
    /// <returns>The active database file path.</returns>
    public string GetDatabasePath()
    {
        if (!string.IsNullOrWhiteSpace(_appSettings.DatabasePath))
        {
            return _appSettings.DatabasePath;
        }

        if (!string.IsNullOrWhiteSpace(_appSettings.CurrentLibraryPath))
        {
            return Path.Combine(_appSettings.CurrentLibraryPath, "library.db");
        }

        return "library.db";
    }

    /// <summary>
    /// Disconnects from the current SQLite data source by clearing all active connection pools.
    /// </summary>
    public void Disconnect()
    {
        SqliteConnection.ClearAllPools();
    }

    /// <summary>
    /// Connects to the database corresponding to the current database path and applies pending migrations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the connection and migration operation.</returns>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        string databasePath = GetDatabasePath();
        string? directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using ClassifyContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Switches the active database to a new file path, disconnecting the previous database and migrating the new one.
    /// </summary>
    /// <param name="newDatabasePath">The path to the new SQLite database file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the database switch operation.</returns>
    public async Task ChangeDatabaseAsync(string newDatabasePath, CancellationToken cancellationToken = default)
    {
        Disconnect();

        _appSettings.DatabasePath = newDatabasePath;

        await ConnectAsync(cancellationToken);

        DatabasePathChanged?.Invoke(this, newDatabasePath);
    }

    /// <summary>
    /// Handles external database path updates triggered directly on the observable settings instance.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="newPath">The new database path.</param>
    private void HandleSettingsDatabasePathChanged(object? sender, string newPath)
    {
        Disconnect();
        DatabasePathChanged?.Invoke(this, newPath);
    }

    /// <summary>
    /// Disposes resources used by the database connection service.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed resources when disposing.
    /// </summary>
    /// <param name="disposing">A value indicating whether managed resources should be disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _appSettings.DatabasePathChanged -= HandleSettingsDatabasePathChanged;
            }

            _isDisposed = true;
        }
    }
}
