using System;
using System.Threading.Tasks;
using Classify.Core.Domain.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider serviceProvider;

    public ViewModelBase CurrentPage
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    public System.Windows.Input.ICommand ShowLibraryScanCommand { get; }
    public System.Windows.Input.ICommand ShowPlaylistsCommand { get; }
    public System.Windows.Input.ICommand ShowBrowseCommand { get; }
    public System.Windows.Input.ICommand ShowFavoritesCommand { get; }
    public System.Windows.Input.ICommand ShowExploreCommand { get; }
    public System.Windows.Input.ICommand ShowRadioCommand { get; }

    public void Initialize()
    {
        ShowHome();
    }

    public void ShowHome()
    {
        CurrentPage = serviceProvider.GetRequiredService<HomeViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowSettings()
    {
        CurrentPage = serviceProvider.GetRequiredService<SettingsViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowLibrary()
    {
        CurrentPage = serviceProvider.GetRequiredService<LibraryViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowLibraryScan()
    {
        CurrentPage = serviceProvider.GetRequiredService<LibraryScanViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowPlaylists()
    {
        CurrentPage = serviceProvider.GetRequiredService<PlaylistsViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowBrowse()
    {
        CurrentPage = serviceProvider.GetRequiredService<BrowseViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowFavorites()
    {
        CurrentPage = serviceProvider.GetRequiredService<FavoritesViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowExplore()
    {
        CurrentPage = serviceProvider.GetRequiredService<ExploreViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowRadio()
    {
        CurrentPage = serviceProvider.GetRequiredService<RadioViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }
    
    public async Task NavigateToDetail(LibraryItemType type, int id)
    {
        ViewModelBase vm = type switch
        {
            LibraryItemType.Composer   => serviceProvider.GetRequiredService<ComposerDetailViewModel>(),
            LibraryItemType.Work       => serviceProvider.GetRequiredService<WorkDetailViewModel>(),
            LibraryItemType.Movement   => serviceProvider.GetRequiredService<MovementDetailViewModel>(),
            LibraryItemType.Recording  => serviceProvider.GetRequiredService<RecordingDetailViewModel>(),
            LibraryItemType.AudioFile  => serviceProvider.GetRequiredService<AudioFileDetailViewModel>(),
            _ => CurrentPage
        };

        if (vm is IDetailViewModel dvm)
        {
            await dvm.LoadAsync(id);
        }

        CurrentPage = vm;
    }

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        ShowLibraryScanCommand = new RelayCommand(_ => ShowLibraryScan());
        ShowPlaylistsCommand = new RelayCommand(_ => ShowPlaylists());
        ShowBrowseCommand = new RelayCommand(_ => ShowBrowse());
        ShowFavoritesCommand = new RelayCommand(_ => ShowFavorites());
        ShowExploreCommand = new RelayCommand(_ => ShowExplore());
        ShowRadioCommand = new RelayCommand(_ => ShowRadio());
    }
}