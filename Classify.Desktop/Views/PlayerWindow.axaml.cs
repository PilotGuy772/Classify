using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Classify.Desktop.ViewModels;

namespace Classify.Desktop.Views;

/// <summary>
/// Standalone Player Window providing playback details and a collapsible queue sidebar.
/// </summary>
public partial class PlayerWindow : Window
{
    private const double SidebarWidth = 275;
    private const double BaseMinWidth = 500;

    /// <summary>
    /// How long the pointer must rest on one point of the progress bar before the time hint appears.
    /// </summary>
    private static readonly TimeSpan SeekHintHoverDelay = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// Pointer travel, in pixels, that counts as leaving the hovered point and restarts the hint delay.
    /// </summary>
    private const double SeekHintHoverSlop = 4;

    private Grid? _coverContainer;
    private Border? _coverBorder;
    private Control? _seekBar;
    private Popup? _seekHintPopup;
    private readonly DispatcherTimer _seekHintHoverTimer;
    private Point _seekHoverAnchor;
    private Point _lastSeekPointer;
    private bool _seekPointerHeld;
    private bool? _lastSidebarState;

    /// <summary>
    /// Initializes a new instance of <see cref="PlayerWindow"/>.
    /// </summary>
    public PlayerWindow()
    {
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 36;
        _seekHintHoverTimer = new DispatcherTimer { Interval = SeekHintHoverDelay };
        _seekHintHoverTimer.Tick += OnSeekHintHoverTick;
        InitializeComponent();
        Closed += OnClosed;
        SetupCoverImageResizing();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _coverContainer = this.FindControl<Grid>("CoverContainer");
        _coverBorder = this.FindControl<Border>("CoverBorder");
        _seekBar = this.FindControl<Grid>("SeekBar");
        _seekHintPopup = this.FindControl<Popup>("SeekHintPopup");
        if (_seekHintPopup != null)
        {
            _seekHintPopup.PlacementTarget = _seekBar;
        }
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Starts the long-hover delay for the time hint.
    /// </summary>
    private void OnSeekBarPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_seekBar == null || _seekPointerHeld)
        {
            return;
        }

        _lastSeekPointer = e.GetPosition(_seekBar);
        _seekHoverAnchor = _lastSeekPointer;
        if (DataContext is PlayerWindowViewModel viewModel && viewModel.IsSeekHintVisible)
        {
            MoveSeekHint(_lastSeekPointer);
            viewModel.ShowSeekHint(FractionFromPointer(_lastSeekPointer));
            return;
        }

        RestartSeekHintHoverTimer();
    }

    /// <summary>
    /// Moves the drag preview or the visible time hint with the pointer.
    /// </summary>
    private void OnSeekBarPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_seekBar == null || DataContext is not PlayerWindowViewModel viewModel)
        {
            return;
        }

        Point point = e.GetPosition(_seekBar);
        _lastSeekPointer = point;
        if (_seekPointerHeld)
        {
            MoveSeekHint(point);
            viewModel.UpdateSeekDrag(FractionFromPointer(point));
            e.Handled = true;
            return;
        }

        if (viewModel.IsSeekHintVisible)
        {
            MoveSeekHint(point);
            viewModel.ShowSeekHint(FractionFromPointer(point));
            return;
        }

        if (Distance(point, _seekHoverAnchor) > SeekHintHoverSlop)
        {
            _seekHoverAnchor = point;
            RestartSeekHintHoverTimer();
        }
    }

    /// <summary>
    /// Pauses playback and shows the time at the pressed point.
    /// </summary>
    private void OnSeekBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_seekBar == null || DataContext is not PlayerWindowViewModel viewModel)
        {
            return;
        }

        PointerPoint pointerPoint = e.GetCurrentPoint(_seekBar);
        if (!pointerPoint.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _seekPointerHeld = true;
        _seekHintHoverTimer.Stop();
        _lastSeekPointer = pointerPoint.Position;
        e.Pointer.Capture(_seekBar);
        MoveSeekHint(_lastSeekPointer);
        viewModel.BeginSeekDrag(FractionFromPointer(_lastSeekPointer));
        e.Handled = true;
    }

    /// <summary>
    /// Seeks to the released point and resumes playback when it was playing before the press.
    /// </summary>
    private void OnSeekBarPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_seekPointerHeld || e.InitialPressMouseButton != MouseButton.Left || _seekBar == null)
        {
            return;
        }

        FinishSeekDrag(e.GetPosition(_seekBar), e.Pointer);
        e.Handled = true;
    }

    /// <summary>
    /// Hides the hover hint when the pointer leaves the bar without a drag in progress.
    /// </summary>
    private void OnSeekBarPointerExited(object? sender, PointerEventArgs e)
    {
        if (_seekPointerHeld)
        {
            return;
        }

        _seekHintHoverTimer.Stop();
        if (DataContext is PlayerWindowViewModel viewModel)
        {
            viewModel.HideSeekHint();
        }
    }

    /// <summary>
    /// Seeks to the last point when the pointer capture ends before a release event.
    /// </summary>
    private void OnSeekBarPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_seekPointerHeld)
        {
            return;
        }

        FinishSeekDrag(_lastSeekPointer, null);
    }

    /// <summary>
    /// Shows the time hint after the pointer has rested on the bar.
    /// </summary>
    private void OnSeekHintHoverTick(object? sender, EventArgs e)
    {
        _seekHintHoverTimer.Stop();
        if (_seekPointerHeld || _seekBar == null || DataContext is not PlayerWindowViewModel viewModel)
        {
            return;
        }

        MoveSeekHint(_lastSeekPointer);
        viewModel.ShowSeekHint(FractionFromPointer(_lastSeekPointer));
    }

    /// <summary>
    /// Stops the hover timer and finishes a drag that is still held when the window closes.
    /// </summary>
    private void OnClosed(object? sender, EventArgs e)
    {
        _seekHintHoverTimer.Stop();
        _seekHintHoverTimer.Tick -= OnSeekHintHoverTick;
        if (_seekPointerHeld)
        {
            FinishSeekDrag(_lastSeekPointer, null);
        }
    }

    /// <summary>
    /// Seeks to <paramref name="point"/> and keeps the hint when the pointer is still on the bar.
    /// </summary>
    /// <param name="point">Pointer position relative to the progress bar.</param>
    /// <param name="pointer">Pointer to release, when this drag still owns the capture.</param>
    private void FinishSeekDrag(Point point, IPointer? pointer)
    {
        if (!_seekPointerHeld || _seekBar == null || DataContext is not PlayerWindowViewModel viewModel)
        {
            _seekPointerHeld = false;
            return;
        }

        _seekPointerHeld = false;
        _lastSeekPointer = point;
        pointer?.Capture(null);
        bool keepHintVisible = IsPointInsideSeekBar(point);
        MoveSeekHint(point);
        viewModel.EndSeekDrag(FractionFromPointer(point), keepHintVisible);
        if (keepHintVisible)
        {
            _seekHoverAnchor = point;
        }
    }

    /// <summary>
    /// Places the time hint on the progress-bar point nearest to <paramref name="point"/>.
    /// </summary>
    /// <param name="point">Pointer position relative to the progress bar.</param>
    private void MoveSeekHint(Point point)
    {
        if (_seekHintPopup == null || _seekBar == null)
        {
            return;
        }

        double width = _seekBar.Bounds.Width;
        double height = _seekBar.Bounds.Height;
        double x = width <= 0 ? 0d : Math.Clamp(point.X, 0d, width);
        double y = height <= 0 ? 0d : Math.Clamp(point.Y, 0d, height);
        _seekHintPopup.PlacementRect = new Rect(x, y, 1d, 1d);
    }

    /// <summary>
    /// Returns the fraction of the progress bar width represented by <paramref name="point"/>.
    /// </summary>
    /// <param name="point">Pointer position relative to the progress bar.</param>
    private double FractionFromPointer(Point point)
    {
        if (_seekBar == null || _seekBar.Bounds.Width <= 0d)
        {
            return 0d;
        }

        return point.X / _seekBar.Bounds.Width;
    }

    /// <summary>
    /// Returns whether <paramref name="point"/> lies inside the progress bar.
    /// </summary>
    /// <param name="point">Pointer position relative to the progress bar.</param>
    private bool IsPointInsideSeekBar(Point point)
    {
        if (_seekBar == null)
        {
            return false;
        }

        return point.X >= 0d
            && point.Y >= 0d
            && point.X <= _seekBar.Bounds.Width
            && point.Y <= _seekBar.Bounds.Height;
    }

    /// <summary>
    /// Restarts the long-hover delay from the current moment.
    /// </summary>
    private void RestartSeekHintHoverTimer()
    {
        _seekHintHoverTimer.Stop();
        _seekHintHoverTimer.Start();
    }

    /// <summary>
    /// Returns the distance between two pointer positions.
    /// </summary>
    private static double Distance(Point left, Point right)
    {
        double dx = left.X - right.X;
        double dy = left.Y - right.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
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
