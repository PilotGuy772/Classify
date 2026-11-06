using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// View model for the application settings page displaying editable configuration fields.
/// </summary>
public class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IAppSettings _settings;
    private readonly IFileSystemService _fileSystemService;
    private bool _isDisposed;

    /// <summary>
    /// Gets the collection of editable setting fields.
    /// </summary>
    public ObservableCollection<SettingFieldViewModel> Fields { get; } = [];

    /// <summary>
    /// Gets the field model for the current library path setting.
    /// </summary>
    public SettingFieldViewModel CurrentLibraryPathField { get; }

    /// <summary>
    /// Gets the field model for the database path setting.
    /// </summary>
    public SettingFieldViewModel DatabasePathField { get; }

    /// <summary>
    /// Gets the field model for the accepted file extensions setting.
    /// </summary>
    public SettingFieldViewModel AcceptedFileExtensionsField { get; }

    /// <summary>
    /// Gets the field model for the appearance setting.
    /// </summary>
    public SettingFieldViewModel AppearanceField { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class with injected application settings.
    /// </summary>
    /// <param name="settings">The application settings singleton.</param>
    public SettingsViewModel(IAppSettings settings)
        : this(settings, new FileSystemService(new PlatformService()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class with injected application settings and file-system services.
    /// </summary>
    /// <param name="settings">The application settings singleton.</param>
    /// <param name="fileSystemService">The file-system service used to reveal or pick paths.</param>
    public SettingsViewModel(IAppSettings settings, IFileSystemService fileSystemService)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));

        CurrentLibraryPathField = new SettingFieldViewModel(
            nameof(IAppSettings.CurrentLibraryPath),
            "Library Path",
            "Root folder containing music and audio files to scan.",
            _settings.CurrentLibraryPath,
            newValue => _settings.CurrentLibraryPath = newValue,
            path => _fileSystemService.TryRevealInFileManager(path),
            currentPath => _fileSystemService.PickDirectory(currentPath));

        DatabasePathField = new SettingFieldViewModel(
            nameof(IAppSettings.DatabasePath),
            "Database Path",
            "File path to the SQLite database (defaults to library.db).",
            _settings.DatabasePath,
            newValue => _settings.DatabasePath = newValue,
            path => _fileSystemService.TryRevealInFileManager(path),
            currentPath => _fileSystemService.PickFile(currentPath));

        AcceptedFileExtensionsField = new SettingFieldViewModel(
            nameof(IAppSettings.AcceptedFileExtensions),
            "Supported File Extensions",
            "Comma-delimited list of extension values to include when scanning the library.",
            _settings.AcceptedFileExtensions,
            newValue => _settings.AcceptedFileExtensions = newValue);

        AppearanceField = new SettingFieldViewModel(
            nameof(IAppSettings.Appearance),
            "Appearance",
            "Choose the app theme. System follows the OS light or dark setting.",
            _settings.Appearance.ToString(),
            newValue =>
            {
                _settings.Appearance = Enum.TryParse<AppAppearance>(newValue, true, out AppAppearance parsed)
                    ? parsed
                    : AppAppearance.System;
            },
            _ => { },
            currentValue => Enum.GetNames<AppAppearance>().FirstOrDefault(name => string.Equals(name, currentValue, StringComparison.OrdinalIgnoreCase)) ?? nameof(AppAppearance.System));

        Fields.Add(CurrentLibraryPathField);
        Fields.Add(DatabasePathField);
        Fields.Add(AcceptedFileExtensionsField);
        Fields.Add(AppearanceField);

        _settings.DatabasePathChanged += OnDatabasePathChanged;

        if (_settings is INotifyPropertyChanged observableSettings)
        {
            observableSettings.PropertyChanged += OnSettingsPropertyChanged;
        }
    }

    /// <summary>
    /// Handles database path changes from application settings.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="newPath">The updated database file path.</param>
    private void OnDatabasePathChanged(object? sender, string newPath)
    {
        DatabasePathField.UpdateSavedValue(newPath);
    }

    /// <summary>
    /// Handles property changed events from observable application settings.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The property changed event arguments.</param>
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppSettings.CurrentLibraryPath))
        {
            CurrentLibraryPathField.UpdateSavedValue(_settings.CurrentLibraryPath);
        }
        else if (e.PropertyName == nameof(IAppSettings.DatabasePath))
        {
            DatabasePathField.UpdateSavedValue(_settings.DatabasePath);
        }
        else if (e.PropertyName == nameof(IAppSettings.AcceptedFileExtensions))
        {
            AcceptedFileExtensionsField.UpdateSavedValue(_settings.AcceptedFileExtensions);
        }
        else if (e.PropertyName == nameof(IAppSettings.Appearance))
        {
            AppearanceField.UpdateSavedValue(_settings.Appearance.ToString());
            Classify.Desktop.Services.AppThemeService.Apply(_settings);
        }
    }

    /// <summary>
    /// Disposes managed resources and unregisters event handlers.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _settings.DatabasePathChanged -= OnDatabasePathChanged;

                if (_settings is INotifyPropertyChanged observableSettings)
                {
                    observableSettings.PropertyChanged -= OnSettingsPropertyChanged;
                }
            }

            _isDisposed = true;
        }
    }
}
