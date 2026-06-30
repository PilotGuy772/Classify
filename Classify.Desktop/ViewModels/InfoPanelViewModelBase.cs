using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Abstract base class for right-side library inspector Info Panels.
/// </summary>
public abstract class InfoPanelViewModelBase : ViewModelBase
{
    /// <summary>
    /// Gets the database unit of work.
    /// </summary>
    protected readonly IUnitOfWork unitOfWork;

    /// <summary>
    /// Gets or sets the host window view model.
    /// </summary>
    protected MainWindowViewModel? host;

    private string title = string.Empty;

    /// <summary>
    /// Gets the panel header title.
    /// </summary>
    public string Title
    {
        get => title;
        protected set
        {
            if (title == value) return;
            title = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the command to close/collapse the info panel.
    /// </summary>
    public ICommand ClosePanelCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="InfoPanelViewModelBase"/> with a database unit of work.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    protected InfoPanelViewModelBase(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
        ClosePanelCommand = new AsyncRelayCommand(CloseAsync);
    }

    /// <summary>
    /// Wires the shell host to allow the panel to collapse itself.
    /// </summary>
    /// <param name="shell">The host window view model.</param>
    public void AttachHost(MainWindowViewModel shell)
    {
        host = shell;
    }

    /// <summary>
    /// Collapses the Info Panel.
    /// </summary>
    /// <returns>A completed task.</returns>
    protected Task CloseAsync()
    {
        host?.CloseWorkInfoPanel();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads panel data for the given entity ID.
    /// </summary>
    /// <param name="id">The domain entity identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task LoadAsync(int id);

    /// <summary>
    /// Opens the Info Panel for a given entity type and ID.
    /// </summary>
    /// <param name="type">The type of library item.</param>
    /// <param name="id">The database identifier.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task OpenInfoPanelAsync(LibraryItemType type, int id)
    {
        if (host is not null)
        {
            await host.OpenInfoPanelAsync(type, id);
        }
    }
}
