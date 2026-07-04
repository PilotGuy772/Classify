using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Right-side Info Panel content for a selected library composer.
/// </summary>
public sealed class ComposerInfoPanelViewModel : InfoPanelViewModelBase
{
    /// <summary>
    /// Gets the collection of works written by this composer.
    /// </summary>
    public ObservableCollection<WorkRowViewModel> WorkRows { get; } = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ComposerInfoPanelViewModel"/> with direct database access.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    public ComposerInfoPanelViewModel(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
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
    /// Invoked by work row Enqueue buttons (stub).
    /// </summary>
    /// <param name="row">The work row model.</param>
    /// <returns>A completed task.</returns>
    internal Task EnqueueWorkRowStubAsync(WorkRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by composer Favorite options menu (stub).
    /// </summary>
    internal Task ToggleFavoriteComposerStubAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by work row Play Next options menu (stub).
    /// </summary>
    internal Task PlayWorkRowNextStubAsync(WorkRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by work row Favorite options menu (stub).
    /// </summary>
    internal Task FavoriteWorkRowStubAsync(WorkRowViewModel row)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked by work row Manage Playlists options menu (stub).
    /// </summary>
    internal Task ManagePlaylistsWorkRowStubAsync(WorkRowViewModel row)
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
        MenuOptions.Add(new MenuOptionViewModel
        {
            Header = "Manage Playlists",
            Icon = TablerIcons.Icons.IconPlaylist,
            Command = new AsyncRelayCommand(() => panel.ManagePlaylistsWorkRowStubAsync(this))
        });
    }
}
