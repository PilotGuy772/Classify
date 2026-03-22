using System;
using Avalonia.Controls;
using Classify.Desktop.ViewModels;

namespace Classify.Desktop.Views;

public partial class ProposedMatchDialog : Window
{
    public ProposedMatchDialog()
    {
        InitializeComponent();
        this.DataContextChanged += ProposedMatchDialog_DataContextChanged;
    }

    private void ProposedMatchDialog_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is Classify.Desktop.ViewModels.ProposedMatchDialogViewModel vm)
        {
            // set control SelectedItem explicitly in case ViewModel initialized them before view was created
            if (ComposerSearch is not null)
                ComposerSearch.SelectedItem = vm.SelectedComposer;
            if (WorkSearch is not null)
                WorkSearch.SelectedItem = vm.SelectedWork;
            if (MovementSearch is not null)
                MovementSearch.SelectedItem = vm.SelectedMovement;
            if (RecordingSearch is not null)
                RecordingSearch.SelectedItem = vm.SelectedRecording;
        }
    }

    private void CancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
