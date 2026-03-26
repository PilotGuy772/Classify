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

    private void PlaylistsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowPlaylists();
        UpdateSelection();
    }

    private void BrowseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowBrowse();
        UpdateSelection();
    }

    private void ScanClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm.ShowLibraryScan();
        UpdateSelection();
    }

    private void FavoritesClicked(object? sender, RoutedEventArgs e)
    {
        Vm.ShowFavorites();
        UpdateSelection();
    }

    private void ExploreClicked(object? sender, RoutedEventArgs e)
    {
        Vm.ShowExplore();
        UpdateSelection();
    }

    private void RadioClicked(object? sender, RoutedEventArgs e)
    {
        Vm.ShowRadio();
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
            var playlists = this.FindControl<Button>("PlaylistsButton");
            var browse = this.FindControl<Button>("BrowseButton");
            var scan = this.FindControl<Button>("ScanButton");
            var favorites = this.FindControl<Button>("FavoritesButton");
            var explore = this.FindControl<Button>("ExploreButton");
            var radio = this.FindControl<Button>("RadioButton");

            SetSelected(home, Vm.CurrentPage is HomeViewModel);
            SetSelected(playlists, Vm.CurrentPage is PlaylistsViewModel);
            SetSelected(browse, Vm.CurrentPage is BrowseViewModel);
            SetSelected(scan, Vm.CurrentPage is LibraryScanViewModel);
            SetSelected(favorites, Vm.CurrentPage is FavoritesViewModel);
            SetSelected(explore, Vm.CurrentPage is ExploreViewModel);
            SetSelected(radio, Vm.CurrentPage is RadioViewModel);
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