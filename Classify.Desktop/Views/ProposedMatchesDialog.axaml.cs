using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Classify.Desktop.ViewModels;
using System.Threading.Tasks;

namespace Classify.Desktop.Views;

public partial class ProposedMatchesDialog : Window
{
    public ProposedMatchesDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async Task AddMatchButton_ClickAsync(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProposedMatchesViewModel vm)
        {
            await vm.AddProposedMatchAsync();
        }
    }

    private void AddMatchButton_Click(object? sender, RoutedEventArgs e)
    {
        _ = AddMatchButton_ClickAsync(sender, e);
    }
}
