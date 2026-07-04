using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// Base class for all ViewModels, implementing INotifyPropertyChanged.
/// </summary>
public class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the collection of customizable context menu options.
    /// </summary>
    public ObservableCollection<MenuOptionViewModel> MenuOptions { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
    /// </summary>
    public ViewModelBase()
    {
        for (int i = 1; i <= 5; i++)
        {
            MenuOptions.Add(new MenuOptionViewModel
            {
                Header = $"Placeholder Option {i}",
                Icon = TablerIcons.Icons.IconChevronRight
            });
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the property changed event.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    protected void RaisePropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}