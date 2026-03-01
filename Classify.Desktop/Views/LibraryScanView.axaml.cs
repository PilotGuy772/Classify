using Avalonia.Controls;
using Classify.Desktop.ViewModels;
using System.Threading.Tasks;

namespace Classify.Desktop.Views;

public partial class LibraryScanView : UserControl
{
    public LibraryScanView()
    {
        InitializeComponent();
    }

    private async Task FileItemDoubleTappedAsync(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LibraryScanViewModel vm && sender is ListBox { SelectedItem: ScannedFileViewModel item })
        {
            await vm.OpenProposedMatchesDialogAsync(item.Id, item.FileName);
        }
    }

    private void FileItemDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = FileItemDoubleTappedAsync(sender, e);
    }
}
