using System.Windows.Input;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Represents a customizable option in a context menu.
/// </summary>
public sealed class MenuOptionViewModel
{
    /// <summary>
    /// Gets or sets the header text of the option.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon for the option.
    /// </summary>
    public TablerIcons.Icons? Icon { get; set; }

    /// <summary>
    /// Gets or sets the command to execute when the option is selected.
    /// </summary>
    public ICommand? Command { get; set; }

    /// <summary>
    /// Gets or sets the parameter passed to the command.
    /// </summary>
    public object? CommandParameter { get; set; }
}
