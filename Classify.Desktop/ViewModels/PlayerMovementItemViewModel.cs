using System;
using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Represents a movement row item displayed inside the player queue sidebar.
/// </summary>
public sealed class PlayerMovementItemViewModel : ViewModelBase
{
    private bool _isCurrentlyPlaying;

    /// <summary>
    /// Gets the zero-based index of the parent nibble in the queue.
    /// </summary>
    public int NibbleIndex { get; }

    /// <summary>
    /// Gets the zero-based index of this movement within the parent nibble.
    /// </summary>
    public int MovementIndex { get; }

    /// <summary>
    /// Gets the Roman numeral ordinal label (e.g. "I.").
    /// </summary>
    public string OrdinalLabel { get; }

    /// <summary>
    /// Gets the movement name.
    /// </summary>
    public string MovementName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this movement is currently playing.
    /// </summary>
    public bool IsCurrentlyPlaying
    {
        get => _isCurrentlyPlaying;
        set
        {
            if (_isCurrentlyPlaying == value) return;
            _isCurrentlyPlaying = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the command to select this movement as the currently playing movement.
    /// </summary>
    public ICommand SelectMovementCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PlayerMovementItemViewModel"/>.
    /// </summary>
    /// <param name="nibbleIndex">The parent nibble index.</param>
    /// <param name="movementIndex">The movement index within the nibble.</param>
    /// <param name="ordinalLabel">The ordinal label.</param>
    /// <param name="movementName">The movement name.</param>
    /// <param name="isCurrentlyPlaying">Whether this movement is currently playing.</param>
    /// <param name="onSelect">Callback invoked when selected.</param>
    public PlayerMovementItemViewModel(
        int nibbleIndex,
        int movementIndex,
        string ordinalLabel,
        string movementName,
        bool isCurrentlyPlaying,
        Action<int, int> onSelect)
    {
        NibbleIndex = nibbleIndex;
        MovementIndex = movementIndex;
        OrdinalLabel = ordinalLabel;
        MovementName = movementName;
        _isCurrentlyPlaying = isCurrentlyPlaying;
        SelectMovementCommand = new RelayCommand(() => onSelect(nibbleIndex, movementIndex));
    }
}
