using Avalonia.Controls;
using Classify.Desktop.ViewModels;

namespace Classify.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel Vm => (MainWindowViewModel)DataContext!;

    private void HomeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowHome();
    }

    private void SettingsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowSettings();
    }

    private void LibraryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowLibrary();
    }

    private void LibraryScanClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowLibraryScan();
    }
}