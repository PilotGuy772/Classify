using System.ComponentModel;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Infrastructure;

/// <summary>
/// Wraps <see cref="IAppSettings"/> to provide change notifications and observable properties when settings update.
/// </summary>
public class ObservableAppSettings : IAppSettings, INotifyPropertyChanged
{
    private readonly IAppSettings _inner;
    private string? _currentLibraryPath;
    private string? _databasePath;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs when the <see cref="DatabasePath"/> property changes.
    /// </summary>
    public event EventHandler<string>? DatabasePathChanged;

    /// <summary>
    /// Occurs when the <see cref="CurrentLibraryPath"/> property changes.
    /// </summary>
    public event EventHandler<string>? CurrentLibraryPathChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableAppSettings"/> class wrapping an existing settings instance.
    /// </summary>
    /// <param name="inner">The underlying settings instance.</param>
    public ObservableAppSettings(IAppSettings inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Gets or sets the path to the current library directory.
    /// </summary>
    public string CurrentLibraryPath
    {
        get => _currentLibraryPath ?? _inner.CurrentLibraryPath;
        set
        {
            if (CurrentLibraryPath != value)
            {
                _currentLibraryPath = value;
                try
                {
                    _inner.CurrentLibraryPath = value;
                }
                catch
                {
                    // Ignore store write errors if the inner provider is read-only
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLibraryPath)));
                CurrentLibraryPathChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the path to the SQLite database file.
    /// </summary>
    public string DatabasePath
    {
        get => _databasePath ?? _inner.DatabasePath;
        set
        {
            if (DatabasePath != value)
            {
                _databasePath = value;
                try
                {
                    _inner.DatabasePath = value;
                }
                catch
                {
                    // Ignore store write errors if the inner provider is read-only
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DatabasePath)));
                DatabasePathChanged?.Invoke(this, value);
            }
        }
    }
}
