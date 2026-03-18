using Avalonia.Markup.Xaml;

namespace Classify.Desktop.Controls;

public partial class MultiEntitySearchControl : EntitySearchControlBase
{
    public MultiEntitySearchControl()
    {
        InitializeComponent();
        InitializeShared();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

