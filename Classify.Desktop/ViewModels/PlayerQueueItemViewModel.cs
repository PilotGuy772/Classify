using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Represents a work container (nibble) item displayed inside the player queue sidebar.
/// </summary>
public sealed class PlayerQueueItemViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the zero-based index of this nibble in the queue.
    /// </summary>
    public int NibbleIndex { get; }

    /// <summary>
    /// Gets the work title.
    /// </summary>
    public string WorkTitle { get; }

    /// <summary>
    /// Gets the conductor and ensemble subtitle line.
    /// </summary>
    public string ConductorAndEnsemble { get; }

    /// <summary>
    /// Gets the collection of movement row view models under this nibble.
    /// </summary>
    public ObservableCollection<PlayerMovementItemViewModel> Movements { get; } = new();

    /// <summary>
    /// Gets the command to select this nibble (playing its first movement).
    /// </summary>
    public ICommand SelectNibbleCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PlayerQueueItemViewModel"/>.
    /// </summary>
    /// <param name="nibbleIndex">The nibble index.</param>
    /// <param name="workTitle">The work title.</param>
    /// <param name="conductorAndEnsemble">The conductor and ensemble line.</param>
    /// <param name="onSelect">Callback invoked when selected.</param>
    public PlayerQueueItemViewModel(
        int nibbleIndex,
        string workTitle,
        string conductorAndEnsemble,
        Action<int> onSelect)
    {
        NibbleIndex = nibbleIndex;
        WorkTitle = workTitle;
        ConductorAndEnsemble = conductorAndEnsemble;
        SelectNibbleCommand = new RelayCommand(() => onSelect(nibbleIndex));
    }
}
