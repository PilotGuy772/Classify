using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
// using Avalonia.Controls.Primitives; // not required
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Classify.Core.Interfaces.Service;

namespace Classify.Desktop.Controls;

public partial class EntitySearchControl : UserControl
{
    private CancellationTokenSource? _cts;
    public ObservableCollection<SearchResult> Suggestions { get; } = [];

    private readonly TextBox? _searchBox;
    private readonly ListBox? _suggestionsListControl;
    // private ItemsControl? _pillsPanelControl; // not needed; items are bound in XAML

    public static readonly StyledProperty<int> SuggestionLimitProperty = AvaloniaProperty.Register<EntitySearchControl, int>(nameof(SuggestionLimit), 10);
    public int SuggestionLimit
    {
        get => GetValue(SuggestionLimitProperty);
        set => SetValue(SuggestionLimitProperty, value);
    }

    public static readonly StyledProperty<TimeSpan> DebounceProperty = AvaloniaProperty.Register<EntitySearchControl, TimeSpan>(nameof(Debounce), TimeSpan.FromMilliseconds(300));
    public TimeSpan Debounce
    {
        get => GetValue(DebounceProperty);
        set => SetValue(DebounceProperty, value);
    }

    public static readonly StyledProperty<IEntitySearchService?> SearchServiceProperty = AvaloniaProperty.Register<EntitySearchControl, IEntitySearchService?>(nameof(SearchService));
    public IEntitySearchService? SearchService
    {
        get => GetValue(SearchServiceProperty);
        set => SetValue(SearchServiceProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<object>?> SelectedItemsProperty = AvaloniaProperty.Register<EntitySearchControl, ObservableCollection<object>?>(nameof(SelectedItems));
    public ObservableCollection<object>? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    public static readonly StyledProperty<object?> SelectedItemProperty = AvaloniaProperty.Register<EntitySearchControl, object?>(nameof(SelectedItem));
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public ICommand RemovePillCommand { get; }

    public EntitySearchControl()
    {
        InitializeComponent();

        RemovePillCommand = new Core.Domain.Infrastructure.RelayCommand(RemovePill);

        // Defaults
        SelectedItems ??= new ObservableCollection<object>();

        // wire up UI: locate named controls from XAML
        _searchBox = this.FindControl<TextBox>("SearchBox");
        _suggestionsListControl = this.FindControl<ListBox>("SuggestionsList");

        if (_searchBox != null)
        {
            // subscribe to text changes (fire-and-forget)
            _searchBox.GetObservable(TextBox.TextProperty).Subscribe(text => { _ = OnTextChanged(); });
            _searchBox.KeyDown += SearchBox_KeyDown;
        }

        if (_suggestionsListControl != null)
        {
            _suggestionsListControl.DoubleTapped += SuggestionsList_DoubleTapped;
            _suggestionsListControl.KeyDown += SuggestionsList_KeyDown;
        }

        // Items are bound in XAML to Suggestions and SelectedItems; no need to set them here.
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SuggestionsList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _suggestionsListControl != null && _suggestionsListControl.SelectedIndex >= 0)
        {
            SearchResult sr = Suggestions[_suggestionsListControl.SelectedIndex];
            AcceptSuggestion(sr);
            e.Handled = true;
        }
    }

    private void SuggestionsList_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (_suggestionsListControl != null && _suggestionsListControl.SelectedIndex >= 0)
        {
            SearchResult sr = Suggestions[_suggestionsListControl.SelectedIndex];
            AcceptSuggestion(sr);
        }
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back && (_searchBox == null || string.IsNullOrEmpty(_searchBox.Text)) && SelectedItems != null && SelectedItems.Any())
        {
            // remove last pill
            SelectedItems.RemoveAt(SelectedItems.Count - 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (Suggestions.Any() && _suggestionsListControl != null)
                _suggestionsListControl.SelectedIndex = 0;
        }
    }

    private void RemovePill(object? o)
    {
        if (o == null) return;
        SelectedItems?.Remove(o);
    }

    private async Task OnTextChanged()
    {
        string text = _searchBox?.Text ?? string.Empty;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        try
        {
            await Task.Delay(Debounce, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (SearchService == null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            Suggestions.Clear();
            return;
        }

        IEnumerable<SearchResult> results = await SearchService.SearchAsync(text, token);
        Suggestions.Clear();
        foreach (SearchResult r in results)
            Suggestions.Add(r);
    }

    private void AcceptSuggestion(SearchResult? suggestion)
    {
        if (suggestion == null) return;

        object entity = suggestion.Entity;

        // multi-mode by default here; caller/viewmodel can map single-mode
        if (SelectedItems != null && !SelectedItems.Contains(entity))
            SelectedItems.Add(entity);

        SelectedItem = entity;
        _searchBox?.Text = string.Empty;
        Suggestions.Clear();
    }
}









