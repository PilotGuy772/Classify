using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;
using Classify.Desktop.ViewModels;
using Classify.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Classify.Desktop.Services;

/// <summary>
/// Service managing the lifecycle of <see cref="PlayerWindow"/> and auto-opening it when queue state changes from empty to non-empty.
/// </summary>
public sealed class PlayerWindowManager : IPlayerWindowManager
{
    private readonly IQueueService _queueService;
    private readonly IServiceProvider _serviceProvider;
    private PlayerWindow? _playerWindow;
    private int _previousCount;
    private volatile bool _showInProgress;

    /// <summary>
    /// Initializes a new instance of <see cref="PlayerWindowManager"/>.
    /// </summary>
    /// <param name="queueService">The queue service instance.</param>
    /// <param name="serviceProvider">The DI service provider.</param>
    public PlayerWindowManager(IQueueService queueService, IServiceProvider serviceProvider)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _previousCount = _queueService.Items.Count;
    }

    /// <summary>
    /// Starts listening to queue service events.
    /// </summary>
    public void Initialize()
    {
        _queueService.QueueChanged += OnQueueChanged;
    }

    /// <summary>
    /// Handles queue change notifications and opens the player window when queue transitions from empty to non-empty.
    /// </summary>
    private void OnQueueChanged(object? sender, EventArgs e)
    {
        int currentCount = _queueService.Items.Count;
        if (_previousCount == 0 && currentCount > 0)
        {
            Dispatcher.UIThread.Post(ShowPlayerWindow);
        }
        _previousCount = currentCount;
    }

    /// <summary>
    /// Displays or focuses the standalone <see cref="PlayerWindow"/>.
    /// The first call waits for LibVLC without blocking the UI thread.
    /// </summary>
    public void ShowPlayerWindow()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ShowPlayerWindow);
            return;
        }

        if (ActivateExistingWindow())
        {
            return;
        }

        if (_showInProgress)
        {
            return;
        }

        _showInProgress = true;
        _ = ShowPlayerWindowAsync();
    }

    /// <summary>
    /// Focuses the player window when it is already open.
    /// </summary>
    /// <returns><see langword="true"/> when an existing window was focused.</returns>
    private bool ActivateExistingWindow()
    {
        if (_playerWindow is null)
        {
            return false;
        }

        if (_playerWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
        {
            _playerWindow.WindowState = Avalonia.Controls.WindowState.Normal;
        }

        _playerWindow.Activate();
        return true;
    }

    /// <summary>
    /// Waits for the audio engine, then creates the player window on the UI thread.
    /// </summary>
    private async Task ShowPlayerWindowAsync()
    {
        try
        {
            await AudioPlaybackLifetime.EnsureStartedAsync(_serviceProvider);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    CreatePlayerWindow();
                }
                finally
                {
                    _showInProgress = false;
                }
            });
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            if (_showInProgress)
            {
                await Dispatcher.UIThread.InvokeAsync(() => _showInProgress = false);
            }
        }
    }

    /// <summary>
    /// Creates and shows the player window after the audio engine is ready.
    /// </summary>
    private void CreatePlayerWindow()
    {
        if (ActivateExistingWindow())
        {
            return;
        }

        IUnitOfWork uow = _serviceProvider.GetRequiredService<IUnitOfWork>();
        IPlaybackService playbackService = _serviceProvider.GetRequiredService<IPlaybackService>();
        PlayerWindowViewModel vm = new(_queueService, uow, playbackService);

        _playerWindow = new PlayerWindow
        {
            DataContext = vm
        };

        _playerWindow.Closed += (object? sender, EventArgs args) =>
        {
            _playerWindow = null;
            playbackService.Stop();
            if (vm is IDisposable playbackView)
            {
                playbackView.Dispose();
            }

            _queueService.Clear();
        };

        _playerWindow.Show();
    }
}
