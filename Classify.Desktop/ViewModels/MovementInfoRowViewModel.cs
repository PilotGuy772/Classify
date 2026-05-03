using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// One row under the Info Panel "Movements" section.
/// </summary>
public sealed class MovementInfoRowViewModel : ViewModelBase
{
    /// <summary>
    /// Ordinal label shown before the movement name (e.g. "I.", "II.").
    /// </summary>
    public string OrdinalLabel { get; }

    /// <summary>
    /// Display name for the movement.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Domain movement id used by row-level commands.
    /// </summary>
    public int MovementId { get; }

    /// <summary>
    /// Row-level play control (wired to panel stubs).
    /// </summary>
    public ICommand PlayMovementRowCommand { get; }

    /// <summary>
    /// Row-level enqueue control (wired to panel stubs).
    /// </summary>
    public ICommand EnqueueMovementRowCommand { get; }

    /// <summary>
    /// Creates a movements section row binding model with commands targeting the owning panel.
    /// </summary>
    public MovementInfoRowViewModel(string ordinalLabel, string name, int movementId, WorkInfoPanelViewModel panel)
    {
        OrdinalLabel = ordinalLabel;
        Name = name;
        MovementId = movementId;
        PlayMovementRowCommand = new AsyncRelayCommand(() => panel.PlayMovementStubAsync(this));
        EnqueueMovementRowCommand = new AsyncRelayCommand(() => panel.EnqueueMovementStubAsync(this));
    }
}
