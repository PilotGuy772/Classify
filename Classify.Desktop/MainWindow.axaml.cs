using Avalonia.Controls;
using Classify.Desktop.ViewModels;
using System.ComponentModel;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Classify.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // When DataContext (ViewModel) is set, attach handlers to keep the selected state in sync.
        this.DataContextChanged += (_, _) => AttachViewModelHandlers();
        AttachViewModelHandlers();
    }

    private MainWindowViewModel Vm => (MainWindowViewModel)DataContext!;

    private void HomeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowHome();
        UpdateSelection();
    }

    private void SettingsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowSettings();
        UpdateSelection();
    }

    private void LibraryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowLibrary();
        UpdateSelection();
    }

    private void LibraryScanClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowLibraryScan();
        UpdateSelection();
    }

    private void AttachViewModelHandlers()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged -= VmOnPropertyChanged;
            vm.PropertyChanged += VmOnPropertyChanged;
            UpdateSelection();
        }
    }

    private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentPage))
        {
            Dispatcher.UIThread.Post(UpdateSelection);
        }
    }

    private void UpdateSelection()
    {
        try
        {
            var home = this.FindControl<Button>("HomeButton");
            var settings = this.FindControl<Button>("SettingsButton");
            var library = this.FindControl<Button>("LibraryButton");
            var scan = this.FindControl<Button>("ScanButton");

            SetSelected(home, Vm.CurrentPage is HomeViewModel);
            SetSelected(settings, Vm.CurrentPage is SettingsViewModel);
            SetSelected(library, Vm.CurrentPage is LibraryViewModel);
            SetSelected(scan, Vm.CurrentPage is LibraryScanViewModel);
        }
        catch
        {
            // Swallow exceptions - selection is visual-only and should not crash the app.
        }
    }

    private void SetSelected(Button? btn, bool selected)
    {
        if (btn is null) return;
        const string sel = "selected";
        if (selected)
        {
            if (!btn.Classes.Contains(sel)) btn.Classes.Add(sel);
        }
        else
        {
            if (btn.Classes.Contains(sel)) btn.Classes.Remove(sel);
        }
    }
}