using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Desktop.ViewModels;
using Classify.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Services;

public class DialogService : IDialogService
{
    private readonly IServiceProvider _provider;
    
    private readonly Dictionary<Type, Type> _dialogMap = new()
    {
        { typeof(ProposedMatchDialogViewModel), typeof(ProposedMatchDialog) },
        { typeof(ProposedMatchesDialogViewModel), typeof(ProposedMatchesDialog) }
    };
    public DialogService(IServiceProvider provider)
    {
        _provider = provider;
    }

    public async Task ShowDialogAsync<TViewModel>()
        where TViewModel : class
    {
        // Resolve the VM from DI
        TViewModel vm = _provider.GetRequiredService<TViewModel>();

        Type windowType = _dialogMap[typeof(TViewModel)];
        Window window = (Window)Activator.CreateInstance(windowType)!;

        window.DataContext = vm;

        // Copy platform classes from main window so dialogs inherit platform-specific styling hooks.
        try
        {
            var main = GetMainWindow();
            foreach (var cls in main.Classes)
            {
                if (!window.Classes.Contains(cls))
                    window.Classes.Add(cls);
            }
        }
        catch
        {
            // Don't let styling failures block dialog display.
        }

        // If you're using ViewLocator, Avalonia will resolve the view automatically.
        // Otherwise you'd map VM → Window here.

        await window.ShowDialog(GetMainWindow());
    }
    
    public async Task ShowDialogAsync<TViewModel, TParam>(TParam parameter) 
        where TViewModel : class, IDialog<TParam> 
    {
        // Resolve the VM from DI
        TViewModel vm = _provider.GetRequiredService<TViewModel>();
        vm.Initialize(parameter);

        Type windowType = _dialogMap[typeof(TViewModel)];
        Window window = (Window)Activator.CreateInstance(windowType)!;

        window.DataContext = vm;

        // If you're using ViewLocator, Avalonia will resolve the view automatically.
        // Otherwise you'd map VM → Window here.

        await window.ShowDialog(GetMainWindow());
        
    }

    private Window GetMainWindow()
        => (Application.Current?.ApplicationLifetime
               as IClassicDesktopStyleApplicationLifetime)?.MainWindow
           ?? throw new InvalidOperationException("No main window.");
}