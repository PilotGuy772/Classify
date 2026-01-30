using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
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

    private readonly IServiceProvider _serviceProvider;
    
    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize()
    {
        ShowHome();
    }

    public void ShowHome()
    {
        CurrentPage = _serviceProvider.GetRequiredService<HomeViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowSettings()
    {
        CurrentPage = _serviceProvider.GetRequiredService<SettingsViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }

    public void ShowLibrary()
    {
        CurrentPage = _serviceProvider.GetRequiredService<LibraryViewModel>();
        RaisePropertyChanged(nameof(CurrentPage));
    }
    
    public async Task NavigateToDetail(LibraryItemType type, int id)
    {
        ViewModelBase vm = type switch
        {
            LibraryItemType.Composer   => _serviceProvider.GetRequiredService<ComposerDetailViewModel>(),
            LibraryItemType.Work       => _serviceProvider.GetRequiredService<WorkDetailViewModel>(),
            LibraryItemType.Movement   => _serviceProvider.GetRequiredService<MovementDetailViewModel>(),
            LibraryItemType.Recording  => _serviceProvider.GetRequiredService<RecordingDetailViewModel>(),
            LibraryItemType.AudioFile  => _serviceProvider.GetRequiredService<AudioFileDetailViewModel>(),
            _ => CurrentPage
        };

        if (vm is IDetailViewModel dvm)
        {
            await dvm.LoadAsync(id);
        }

        CurrentPage = vm;
    }
}