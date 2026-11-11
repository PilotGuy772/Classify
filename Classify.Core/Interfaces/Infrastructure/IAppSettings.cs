using System;
using System.ComponentModel;

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
    /// Gets or sets the preferred appearance of the application.
    /// </summary>
    public AppAppearance Appearance { get; set; }

    /// <summary>
    /// Occurs when the <see cref="DatabasePath"/> property is updated.
    /// </summary>
    public event EventHandler<string>? DatabasePathChanged;

    /// <summary>
    /// Comma-delimited list with all file extensions accepted as audio files, case-insensitive.
    /// </summary>
    [DefaultValue(".mp3,.wav,.flac,.ogg")]
    public string AcceptedFileExtensions { get; set; }
}

/// <summary>
/// Represents the application's visual appearance preference.
/// </summary>
public enum AppAppearance
{
    System,
    Light,
    Dark
}