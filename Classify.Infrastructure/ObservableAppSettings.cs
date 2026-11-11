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
    private string _acceptedFileExtensions;
    private AppAppearance _appearance;

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
    /// Gets or sets the accepted file extensions when scanning for library folders.
    /// </summary>
    public string AcceptedFileExtensions
    {
        get => _acceptedFileExtensions;
        set
        {
            string newValue = value ?? string.Empty;
            if (!string.Equals(_acceptedFileExtensions, newValue, StringComparison.Ordinal))
            {
                _acceptedFileExtensions = newValue;
                try
                {
                    _inner.AcceptedFileExtensions = newValue;
                }
                catch
                {
                    // Ignore store write errors if the inner provider is read-only
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AcceptedFileExtensions)));
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableAppSettings"/> class wrapping an existing settings instance.
    /// </summary>
    /// <param name="inner">The underlying settings instance.</param>
    public ObservableAppSettings(IAppSettings inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        if (string.IsNullOrWhiteSpace(_inner.DatabasePath) && !string.IsNullOrWhiteSpace(_inner.CurrentLibraryPath))
        {
            string computedDatabasePath = GetDefaultDatabasePath(_inner.CurrentLibraryPath);
            _databasePath = computedDatabasePath;
            try
            {
                _inner.DatabasePath = computedDatabasePath;
            }
            catch
            {
                // Ignore store write errors if the inner provider is read-only
            }
        }

        _acceptedFileExtensions = string.IsNullOrWhiteSpace(_inner.AcceptedFileExtensions)
            ? ".mp3,.wav,.flac,.ogg"
            : _inner.AcceptedFileExtensions;

        _appearance = _inner.Appearance == default ? AppAppearance.System : _inner.Appearance;
        if (string.IsNullOrWhiteSpace(_inner.AcceptedFileExtensions))
        {
            try
            {
                _inner.AcceptedFileExtensions = _acceptedFileExtensions;
            }
            catch
            {
                // Ignore store write errors if the inner provider is read-only
            }
        }
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

                if (!string.IsNullOrWhiteSpace(value))
                {
                    string computedDatabasePath = GetDefaultDatabasePath(value);
                    DatabasePath = computedDatabasePath;
                }
            }
        }
    }

    private static string GetDefaultDatabasePath(string libraryPath)
    {
        string libraryFolder = libraryPath.Trim();
        string classifyDirectory = Path.Combine(libraryFolder, ".classify");
        return Path.Combine(classifyDirectory, "library.db");
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

    /// <summary>
    /// Gets or sets the preferred appearance of the application.
    /// </summary>
    public AppAppearance Appearance
    {
        get => _appearance;
        set
        {
            AppAppearance normalized = value;
            if (_appearance != normalized)
            {
                _appearance = normalized;
                try
                {
                    _inner.Appearance = normalized;
                }
                catch
                {
                    // Ignore store write errors if the inner provider is read-only
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Appearance)));
            }
        }
    }
}
