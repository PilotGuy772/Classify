using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop.ViewModels;

public class MainWindowViewModel(IServiceProvider serviceProvider) : ViewModelBase
{
    public ViewModelBase CurrentPage
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

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
}