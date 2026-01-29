using System;
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
}