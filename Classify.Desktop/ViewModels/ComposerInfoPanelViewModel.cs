using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Right-side Info Panel content for a selected library composer.
/// </summary>
public sealed class ComposerInfoPanelViewModel : InfoPanelViewModelBase
{
    /// <summary>
    /// Gets the collection of works written by this composer.
    /// </summary>
    public ObservableCollection<WorkRowViewModel> WorkRows { get; } = [];

    private readonly IQueueService _queueService;
    private readonly INibbleBuilderService _nibbleBuilder;
    private int currentComposerId;

    /// <summary>
    /// Main header action: Play all works by composer.
    /// </summary>
    public ICommand PlayComposerCommand { get; }

    /// <summary>
    /// Main header action: Play Next all works by composer.
    /// </summary>
    public ICommand PlayNextComposerCommand { get; }

    /// <summary>
    /// Main header action: Enqueue all works by composer.
    /// </summary>
    public ICommand EnqueueComposerCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ComposerInfoPanelViewModel"/> with direct database access and queue services.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    /// <param name="queueService">The queue service.</param>
    /// <param name="nibbleBuilder">The nibble builder service.</param>
    public ComposerInfoPanelViewModel(IUnitOfWork unitOfWork, IQueueService queueService, INibbleBuilderService nibbleBuilder) : base(unitOfWork)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _nibbleBuilder = nibbleBuilder ?? throw new ArgumentNullException(nameof(nibbleBuilder));

        PlayComposerCommand = new AsyncRelayCommand(PlayComposerAsync);
        PlayNextComposerCommand = new AsyncRelayCommand(PlayNextComposerAsync);
        EnqueueComposerCommand = new AsyncRelayCommand(EnqueueComposerAsync);

        MenuOptions.Clear();
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Favorite",
            Icon = TablerIcons.Icons.IconHeart,
            Command = new AsyncRelayCommand(ToggleFavoriteComposerStubAsync)
        });
    }

    /// <summary>
    /// Loads the composer and their associated works.
    /// </summary>
    /// <param name="composerId">The composer identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task LoadAsync(int composerId)
    {
        currentComposerId = composerId;
        WorkRows.Clear();

        Composer? composer = await unitOfWork.Composers.GetByIdAsync(composerId);
        if (composer is null)
        {
            Title = string.Empty;
            return;
        }

        Title = composer.Name;

        IEnumerable<Work> works = await unitOfWork.Works.GetWorksByComposerIdAsync(composerId);
        foreach (Work work in works)
        {
            WorkRows.Add(new WorkRowViewModel(work.Name, work.Id, this));
        }
    }

    private Task PlayComposerAsync()
    {
        return Task.CompletedTask;
    }

    private async Task PlayNextComposerAsync()
    {
        if (currentComposerId == 0) return;
        IEnumerable<Work> works = await unitOfWork.Works.GetWorksByComposerIdAsync(currentComposerId);
        List<QueueItem> items = [];
        foreach (Work work in works)
        {
            QueueItem? item = await _nibbleBuilder.BuildForWorkAsync(work.Id);
            if (item != null)
            {
                items.Add(item);
            }
        }
        if (items.Count > 0)
        {
            _queueService.EnqueueNextRange(items);
        }
    }

    private async Task EnqueueComposerAsync()
    {
        if (currentComposerId == 0) return;
        IEnumerable<Work> works = await unitOfWork.Works.GetWorksByComposerIdAsync(currentComposerId);
        List<QueueItem> items = [];
        foreach (Work work in works)
        {
            QueueItem? item = await _nibbleBuilder.BuildForWorkAsync(work.Id);
            if (item != null)
            {
                items.Add(item);
            }
        }
        if (items.Count > 0)
        {
            _queueService.EnqueueRange(items);
        }
    }

    /// <summary>
    /// Invoked by work row Play buttons (stub).
    /// </summary>
    /// <param name="row">The work row model.</param>
    /// <returns>A completed task.</returns>
    internal Task PlayWorkRowStubAsync(WorkRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by work row Enqueue buttons.
    /// </summary>
    /// <param name="row">The work row model.</param>
    /// <returns>A task representing the operation.</returns>
    internal async Task EnqueueWorkRowStubAsync(WorkRowViewModel row)
    {
        QueueItem? item = await _nibbleBuilder.BuildForWorkAsync(row.WorkId);
        if (item != null)
        {
            _queueService.Enqueue(item);
        }
    }

    /// <summary>
    /// Invoked by composer Favorite options menu (stub).
    /// </summary>
    internal Task ToggleFavoriteComposerStubAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by work row Play Next options menu.
    /// </summary>
    /// <param name="row">The work row model.</param>
    /// <returns>A task representing the operation.</returns>
    internal async Task PlayWorkRowNextStubAsync(WorkRowViewModel row)
    {
        QueueItem? item = await _nibbleBuilder.BuildForWorkAsync(row.WorkId);
        if (item != null)
        {
            _queueService.EnqueueNext(item);
        }
    }

    /// <summary>
    /// Invoked by work row Favorite options menu (stub).
    /// </summary>
    internal Task FavoriteWorkRowStubAsync(WorkRowViewModel row)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents a work list row item under a composer.
/// </summary>
public sealed class WorkRowViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the work title.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the domain work identifier.
    /// </summary>
    public int WorkId { get; }

    /// <summary>
    /// Gets the row play action.
    /// </summary>
    public ICommand PlayWorkRowCommand { get; }

    /// <summary>
    /// Gets the row enqueue action.
    /// </summary>
    public ICommand EnqueueWorkRowCommand { get; }

    /// <summary>
    /// Gets the command to show this work's info panel.
    /// </summary>
    public ICommand ShowWorkCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="WorkRowViewModel"/> with parent panel callbacks.
    /// </summary>
    /// <param name="name">The work name.</param>
    /// <param name="workId">The work identifier.</param>
    /// <param name="panel">The owning composer panel.</param>
    public WorkRowViewModel(string name, int workId, ComposerInfoPanelViewModel panel)
    {
        Name = name;
        WorkId = workId;
        PlayWorkRowCommand = new AsyncRelayCommand(() => panel.PlayWorkRowStubAsync(this));
        EnqueueWorkRowCommand = new AsyncRelayCommand(() => panel.EnqueueWorkRowStubAsync(this));
        ShowWorkCommand = new AsyncRelayCommand(() => panel.OpenInfoPanelAsync(LibraryItemType.Work, workId));

        MenuOptions.Clear();
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Now",
            Icon = TablerIcons.Icons.IconPlayerPlay,
            Command = PlayWorkRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Play Next",
            Icon = TablerIcons.Icons.IconCornerUpLeft,
            Command = new AsyncRelayCommand(() => panel.PlayWorkRowNextStubAsync(this))
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Enqueue",
            Icon = TablerIcons.Icons.IconCornerDownLeft,
            Command = EnqueueWorkRowCommand
        });
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Favorite",
            Icon = TablerIcons.Icons.IconHeart,
            Command = new AsyncRelayCommand(() => panel.FavoriteWorkRowStubAsync(this))
        });
    }
}
