using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Represents a row item in the hierarchical library view list.
/// </summary>
public class LibraryItemViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isAlternate;

    /// <summary>
    /// Gets the unique identifier for the domain entity.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the formatted display name of the item.
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// Gets the item type.
    /// </summary>
    public LibraryItemType Type { get; }

    /// <summary>
    /// Gets the nesting level (0 = Composer, 1 = Work, 2 = Recording, 3 = MovementRecording).
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets the thickness representing the left indentation margin.
    /// </summary>
    public Avalonia.Thickness IndentMargin => new(Level * 25, 0, 0, 0);

    /// <summary>
    /// Gets or sets the Roman numeral ordinal label (applicable to Level 3 MovementRecordings).
    /// </summary>
    public string? Ordinal { get; set; }

    /// <summary>
    /// Gets a value indicating whether this item can be expanded.
    /// </summary>
    public bool IsExpandable => Type != LibraryItemType.MovementRecording;

    /// <summary>
    /// Gets or sets a value indicating whether this item is currently expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CaretIcon));
            ToggleExpandCallback?.Invoke(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this item uses the alternate list row backdrop color.
    /// </summary>
    public bool IsAlternate
    {
        get => _isAlternate;
        set
        {
            if (_isAlternate == value) return;
            _isAlternate = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the name of the Tabler icon to display for expansion state.
    /// </summary>
    public string CaretIcon => IsExpanded ? "IconCaretDown" : "IconCaretRight";

    /// <summary>
    /// Gets or sets the callback invoked when the expand/collapse state changes.
    /// </summary>
    public Action<LibraryItemViewModel>? ToggleExpandCallback { get; set; }

    /// <summary>
    /// Gets the list of loaded children view models.
    /// </summary>
    public List<LibraryItemViewModel>? Children { get; set; }

    /// <summary>
    /// Gets the command to toggle expansion.
    /// </summary>
    public ICommand ToggleExpandCommand { get; }

    /// <summary>
    /// Resets the expansion state recursively for this item and all its descendants without invoking the callback.
    /// </summary>
    public void ResetExpansionState()
    {
        _isExpanded = false;
        RaisePropertyChanged(nameof(IsExpanded));
        RaisePropertyChanged(nameof(CaretIcon));

        if (Children != null)
        {
            foreach (LibraryItemViewModel child in Children)
            {
                child.ResetExpansionState();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LibraryItemViewModel"/>.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="displayText">The display name.</param>
    /// <param name="type">The item type.</param>
    /// <param name="level">The nesting level.</param>
    public LibraryItemViewModel(int id, string displayText, LibraryItemType type, int level)
    {
        Id = id;
        DisplayText = displayText;
        Type = type;
        Level = level;
        ToggleExpandCommand = new AsyncRelayCommand(() =>
        {
            IsExpanded = !IsExpanded;
            return Task.CompletedTask;
        });
    }
}