using System;
using System.Threading.Tasks;
using Classify.Core.Domain.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Root shell view model: navigation between pages and optional right-side work Info Panel.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider serviceProvider;

    private bool isWorkInfoPanelOpen;

    private ViewModelBase currentPage = null!;

    /// <summary>
    /// Currently displayed primary page content.
    /// </summary>
    public ViewModelBase CurrentPage
    {
        get => currentPage;
        set
        {
            currentPage = value;
            RaisePropertyChanged();
            if (value is not LibraryViewModel)
                IsWorkInfoPanelOpen = false;
        }
    }

    /// <summary>
    /// Whether the right Info Panel column is expanded (library work inspector).
    /// </summary>
    public bool IsWorkInfoPanelOpen
    {
        get => isWorkInfoPanelOpen;
        private set
        {
            if (isWorkInfoPanelOpen == value)
                return;
            isWorkInfoPanelOpen = value;
            RaisePropertyChanged();
        }
    }

    private InfoPanelViewModelBase? activeInfoPanel;

    /// <summary>
    /// Gets or sets the currently active right-side Info Panel.
    /// </summary>
    public InfoPanelViewModelBase? ActiveInfoPanel
    {
        get => activeInfoPanel;
        set
        {
            if (activeInfoPanel == value) return;
            activeInfoPanel = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the work inspector panel.
    /// </summary>
    public WorkInfoPanelViewModel WorkInfo { get; }

    /// <summary>
    /// Gets the composer inspector panel.
    /// </summary>
    public ComposerInfoPanelViewModel ComposerInfo { get; }

    /// <summary>
    /// Gets the movement inspector panel.
    /// </summary>
    public MovementInfoPanelViewModel MovementInfo { get; }

    /// <summary>
    /// Gets the recording inspector panel.
    /// </summary>
    public RecordingInfoPanelViewModel RecordingInfo { get; }

    /// <summary>
    /// Gets the movement recording inspector panel.
    /// </summary>
    public MovementRecordingInfoPanelViewModel MovementRecordingInfo { get; }

    public System.Windows.Input.ICommand ShowLibraryScanCommand { get; }
    public System.Windows.Input.ICommand ShowPlaylistsCommand { get; }
    public System.Windows.Input.ICommand ShowBrowseCommand { get; }
    public System.Windows.Input.ICommand ShowFavoritesCommand { get; }
    public System.Windows.Input.ICommand ShowExploreCommand { get; }
    public System.Windows.Input.ICommand ShowRadioCommand { get; }

    /// <summary>
    /// Initializes shell navigation with injected services and all Info Panels.
    /// </summary>
    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        WorkInfoPanelViewModel workInfo,
        ComposerInfoPanelViewModel composerInfo,
        MovementInfoPanelViewModel movementInfo,
        RecordingInfoPanelViewModel recordingInfo,
        MovementRecordingInfoPanelViewModel movementRecordingInfo)
    {
        this.serviceProvider = serviceProvider;
        WorkInfo = workInfo;
        ComposerInfo = composerInfo;
        MovementInfo = movementInfo;
        RecordingInfo = recordingInfo;
        MovementRecordingInfo = movementRecordingInfo;

        WorkInfo.AttachHost(this);
        ComposerInfo.AttachHost(this);
        MovementInfo.AttachHost(this);
        RecordingInfo.AttachHost(this);
        MovementRecordingInfo.AttachHost(this);

        ShowLibraryScanCommand = new RelayCommand(_ => ShowLibraryScan());
        ShowPlaylistsCommand = new RelayCommand(_ => ShowPlaylists());
        ShowBrowseCommand = new RelayCommand(_ => ShowBrowse());
        ShowFavoritesCommand = new RelayCommand(_ => ShowFavorites());
        ShowExploreCommand = new RelayCommand(_ => ShowExplore());
        ShowRadioCommand = new RelayCommand(_ => ShowRadio());
    }

    /// <summary>
    /// Navigates to the default startup page.
    /// </summary>
    public void Initialize()
    {
        ShowHome();
    }

    /// <summary>
    /// Collapses the Info Panel column from shell chrome or panel commands.
    /// </summary>
    public void CloseWorkInfoPanel()
    {
        IsWorkInfoPanelOpen = false;
    }

    /// <summary>
    /// Handles single-selection changes from the library browse list (opens Info Panel for selected entity).
    /// </summary>
    public async Task OnLibraryBrowserSelectionChangedAsync(LibraryItemViewModel? item)
    {
        if (item is null)
        {
            IsWorkInfoPanelOpen = false;
            return;
        }

        switch (item.Type)
        {
            case LibraryItemType.Work:
                await WorkInfo.LoadAsync(item.Id);
                ActiveInfoPanel = WorkInfo;
                IsWorkInfoPanelOpen = true;
                break;
            case LibraryItemType.Composer:
                await ComposerInfo.LoadAsync(item.Id);
                ActiveInfoPanel = ComposerInfo;
                IsWorkInfoPanelOpen = true;
                break;
            case LibraryItemType.Movement:
                await MovementInfo.LoadAsync(item.Id);
                ActiveInfoPanel = MovementInfo;
                IsWorkInfoPanelOpen = true;
                break;
            case LibraryItemType.Recording:
                await RecordingInfo.LoadAsync(item.Id);
                ActiveInfoPanel = RecordingInfo;
                IsWorkInfoPanelOpen = true;
                break;
            default:
                IsWorkInfoPanelOpen = false;
                break;
        }
    }

    /// <summary>
    /// Programmatically opens the movement recording info panel.
    /// </summary>
    /// <param name="performedMovementId">The performed movement identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OpenMovementRecordingInfoPanelAsync(int performedMovementId)
    {
        await MovementRecordingInfo.LoadAsync(performedMovementId);
        ActiveInfoPanel = MovementRecordingInfo;
        IsWorkInfoPanelOpen = true;
    }

    /// <summary>
    /// Shows the Home page.
    /// </summary>
    public void ShowHome()
    {
        CurrentPage = serviceProvider.GetRequiredService<HomeViewModel>();
    }

    /// <summary>
    /// Shows Settings (reserved).
    /// </summary>
    public void ShowSettings()
    {
        CurrentPage = serviceProvider.GetRequiredService<SettingsViewModel>();
    }

    /// <summary>
    /// Shows the Library browse page.
    /// </summary>
    public void ShowLibrary()
    {
        CurrentPage = serviceProvider.GetRequiredService<LibraryViewModel>();
    }

    /// <summary>
    /// Shows the library scan flow page.
    /// </summary>
    public void ShowLibraryScan()
    {
        CurrentPage = serviceProvider.GetRequiredService<LibraryScanViewModel>();
    }

    /// <summary>
    /// Shows Playlists placeholder page.
    /// </summary>
    public void ShowPlaylists()
    {
        CurrentPage = serviceProvider.GetRequiredService<PlaylistsViewModel>();
    }

    /// <summary>
    /// Shows Browse placeholder page (distinct from Library).
    /// </summary>
    public void ShowBrowse()
    {
        CurrentPage = serviceProvider.GetRequiredService<BrowseViewModel>();
    }

    /// <summary>
    /// Shows Favorites placeholder page.
    /// </summary>
    public void ShowFavorites()
    {
        CurrentPage = serviceProvider.GetRequiredService<FavoritesViewModel>();
    }

    /// <summary>
    /// Shows Explore placeholder page.
    /// </summary>
    public void ShowExplore()
    {
        CurrentPage = serviceProvider.GetRequiredService<ExploreViewModel>();
    }

    /// <summary>
    /// Shows Radio placeholder page.
    /// </summary>
    public void ShowRadio()
    {
        CurrentPage = serviceProvider.GetRequiredService<RadioViewModel>();
    }

    /// <summary>
    /// Navigates to a detail page for the given library item type.
    /// </summary>
    public async Task NavigateToDetail(LibraryItemType type, int id)
    {
        ViewModelBase vm = type switch
        {
            LibraryItemType.Composer => serviceProvider.GetRequiredService<ComposerDetailViewModel>(),
            LibraryItemType.Work => serviceProvider.GetRequiredService<WorkDetailViewModel>(),
            LibraryItemType.Movement => serviceProvider.GetRequiredService<MovementDetailViewModel>(),
            LibraryItemType.Recording => serviceProvider.GetRequiredService<RecordingDetailViewModel>(),
            LibraryItemType.AudioFile => serviceProvider.GetRequiredService<AudioFileDetailViewModel>(),
            _ => CurrentPage
        };

        if (vm is IDetailViewModel dvm)
        {
            await dvm.LoadAsync(id);
        }

        CurrentPage = vm;
    }
}
