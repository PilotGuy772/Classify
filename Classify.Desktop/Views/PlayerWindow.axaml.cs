using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Classify.Desktop.ViewModels;

namespace Classify.Desktop.Views;

/// <summary>
/// Standalone Player Window providing playback details and a collapsible queue sidebar.
/// </summary>
public partial class PlayerWindow : Window
{
    private const double SidebarWidth = 275;
    private const double BaseMinWidth = 500;

    private Grid? _coverContainer;
    private Border? _coverBorder;
    private bool? _lastSidebarState;

    /// <summary>
    /// Initializes a new instance of <see cref="PlayerWindow"/>.
    /// </summary>
    public PlayerWindow()
    {
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 36;
        InitializeComponent();
        SetupCoverImageResizing();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _coverContainer = this.FindControl<Grid>("CoverContainer");
        _coverBorder = this.FindControl<Border>("CoverBorder");
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PlayerWindowViewModel vm)
        {
            _lastSidebarState = vm.IsSidebarExpanded;
            MinWidth = vm.IsSidebarExpanded ? BaseMinWidth + SidebarWidth : BaseMinWidth;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerWindowViewModel.IsSidebarExpanded) && DataContext is PlayerWindowViewModel vm)
        {
            if (_lastSidebarState.HasValue && _lastSidebarState.Value != vm.IsSidebarExpanded)
            {
                if (vm.IsSidebarExpanded)
                {
                    Width += SidebarWidth;
                    MinWidth = BaseMinWidth + SidebarWidth;
                }
                else
                {
                    Width = Math.Max(BaseMinWidth, Width - SidebarWidth);
                    MinWidth = BaseMinWidth;
                }
                _lastSidebarState = vm.IsSidebarExpanded;
            }
        }
    }

    private void SetupCoverImageResizing()
    {
        if (_coverContainer == null || _coverBorder == null) return;

        _coverContainer.SizeChanged += (object? sender, SizeChangedEventArgs e) =>
        {
            double availableWidth = e.NewSize.Width;
            double availableHeight = e.NewSize.Height;
            double minDimension = Math.Min(availableWidth, availableHeight);

            if (double.IsNaN(minDimension) || minDimension < 50)
            {
                _coverBorder.IsVisible = false;
            }
            else
            {
                _coverBorder.IsVisible = true;
                double sideLength = minDimension;
                _coverBorder.Width = sideLength;
                _coverBorder.Height = sideLength;
            }
        };
    }
}
