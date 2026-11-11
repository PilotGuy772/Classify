using System;
using System.Threading;
using System.Threading.Tasks;

namespace Classify.Core.Interfaces.Infrastructure;

/// <summary>
/// Service responsible for managing database connection lifecycles, configuration, and data source switching.
/// </summary>
public interface IDatabaseConnectionService
{
    /// <summary>
    /// Occurs when the database path or data source has changed.
    /// </summary>
    public event EventHandler<string>? DatabasePathChanged;

    /// <summary>
    /// Gets the current database connection string.
    /// </summary>
    /// <returns>The SQLite connection string.</returns>
    public string GetConnectionString();

    /// <summary>
    /// Gets the current database file path.
    /// </summary>
    /// <returns>The database file path.</returns>
    public string GetDatabasePath();

    /// <summary>
    /// Disconnects from the current data source and releases SQLite connection pools.
    /// </summary>
    public void Disconnect();

    /// <summary>
    /// Connects to the current database and applies any pending migrations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the connection and migration operation.</returns>
    public Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the database path, disconnecting the previous data source, updating settings, and connecting/migrating the new database.
    /// </summary>
    /// <param name="newDatabasePath">The path to the new database file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the switch operation.</returns>
    public Task ChangeDatabaseAsync(string newDatabasePath, CancellationToken cancellationToken = default);
}
