using Avalonia.Markup.Xaml;

namespace Classify.Desktop.Controls;

public partial class SingleEntitySearchControl : EntitySearchControlBase
{
    public SingleEntitySearchControl()
    {
        InitializeComponent();
        InitializeShared();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void AcceptSuggestion(Core.Interfaces.Service.SearchResult? suggestion)
    {
        if (suggestion == null) return;

        object entity = suggestion.Entity;

        // ensure single-selection semantics: SelectedItems contains exactly one item
        if (SelectedItems == null)
            SelectedItems = new System.Collections.ObjectModel.ObservableCollection<object>();
        else
            SelectedItems.Clear();

        SelectedItems.Add(entity);
        SelectedItem = entity;
        _searchBox?.Clear();
        Suggestions.Clear();
    }
}

