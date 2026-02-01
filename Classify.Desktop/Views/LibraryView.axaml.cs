using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Classify.Desktop.ViewModels;

namespace Classify.Desktop.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }
    
    private void ItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox lb &&
            lb.SelectedItem is LibraryItemViewModel item &&
            DataContext is LibraryViewModel vm)
        {
            _ = vm.OpenItemAsync(item);
        }
    }
}