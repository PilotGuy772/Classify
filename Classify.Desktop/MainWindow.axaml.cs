using Avalonia.Controls;
using Classify.Desktop.ViewModels;

namespace Classify.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}