using System;
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
    /// </summary>
    public void ShowPlayerWindow()
    {
        if (_playerWindow != null)
        {
            _playerWindow.Activate();
            return;
        }

        IUnitOfWork uow = _serviceProvider.GetRequiredService<IUnitOfWork>();
        PlayerWindowViewModel vm = new PlayerWindowViewModel(_queueService, uow);

        _playerWindow = new PlayerWindow
        {
            DataContext = vm
        };

        _playerWindow.Closed += (object? sender, EventArgs args) =>
        {
            _playerWindow = null;
            _queueService.Clear();
        };

        _playerWindow.Show();
    }
}
