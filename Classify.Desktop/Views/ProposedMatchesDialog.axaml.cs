using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Classify.Desktop.ViewModels;
using Classify.Core.Domain;
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
        _ = sender; // Suppress unused parameter warning
        _ = e;      // Suppress unused parameter warning

        if (DataContext is ProposedMatchesDialogViewModel vm)
        {
            await vm.AddProposedMatchAsync();
        }
    }

    private async Task AddMatchButton_Click(object? sender, RoutedEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        _ = e;      // Suppress unused parameter warning

        if (DataContext is ProposedMatchesDialogViewModel vm)
        {
            await vm.AddProposedMatchAsync();
        }
    }

    private void OnEditMatchButtonClick(object? sender, RoutedEventArgs _)
    {
        if (DataContext is ProposedMatchesDialogViewModel viewModel && sender is Button button && button.Tag is ProposedMatch match)
        {
            viewModel.EditMatchCommand.Execute(match);
        }
    }

    private void OnAcceptMatchButtonClick(object? sender, RoutedEventArgs _)
    {
        if (DataContext is ProposedMatchesDialogViewModel viewModel && sender is Button button && button.Tag is ProposedMatch match)
        {
            viewModel.AcceptMatchCommand.Execute(match);
        }
    }
}