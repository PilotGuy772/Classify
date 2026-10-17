using System;

namespace Classify.Core.Interfaces.Infrastructure;

/// <summary>
/// Defines application settings and notification contracts for library and database configurations.
/// </summary>
public interface IAppSettings
{
    /// <summary>
    /// Gets or sets the path to the current library directory.
    /// </summary>
    public string CurrentLibraryPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the SQLite database file.
    /// </summary>
    public string DatabasePath { get; set; }

    /// <summary>
    /// Occurs when the <see cref="DatabasePath"/> property is updated.
    /// </summary>
    public event EventHandler<string>? DatabasePathChanged;
}