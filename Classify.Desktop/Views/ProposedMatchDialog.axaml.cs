using Avalonia.Controls;
using Classify.Desktop.ViewModels;

namespace Classify.Desktop.Views;

public partial class ProposedMatchDialog : Window
{
    public ProposedMatchDialog()
    {
        InitializeComponent();
    }

    private void CancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
