using Avalonia.Controls;
using Classify.Desktop.ViewModels;
using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Classify.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Hint to Avalonia the titlebar height when using extended client area.
        // Use an explicit int to follow repo style.
        ExtendClientAreaTitleBarHeightHint = 36;

        // Show/hide the macOS spacer only on macOS.
        try
        {
            if (System.OperatingSystem.IsMacOS())
            {
                Border? spacer = this.FindControl<Border>("MacTrafficLightsSpacer");
                spacer?.IsVisible = true;
            }
            else
            {
                Border? spacer = this.FindControl<Border>("MacTrafficLightsSpacer");
                if (spacer is not null)
                {
                    spacer.IsVisible = false;
                }
            }
        }
        catch
        {
            // Ignore - visibility is cosmetic.
        }

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
        Vm.ShowLibrary();
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
            Button? home = this.FindControl<Button>("HomeButton");
            Button? playlists = this.FindControl<Button>("PlaylistsButton");
            Button? browse = this.FindControl<Button>("BrowseButton");
            Button? scan = this.FindControl<Button>("ScanButton");
            Button? favorites = this.FindControl<Button>("FavoritesButton");
            Button? explore = this.FindControl<Button>("ExploreButton");
            Button? radio = this.FindControl<Button>("RadioButton");

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

    private void MacTrafficLightsSpacer_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e is null) return;
        PointerPressedEventArgs args = e;
        try
        {
            PointerPoint current = args.GetCurrentPoint(this);
            if (current.Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(args);
            }
        }
        catch
        {
            // Swallow - failing to begin drag should not crash the app.
        }
    }
}