using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Classify.Core.Interfaces.Service;

namespace Classify.Desktop.Controls;

/// <summary>
/// Shared non-visual logic for entity search controls. Derived controls should load their XAML
/// and call <see cref="InitializeShared"/> from their constructor.
/// </summary>
public class EntitySearchControlBase : UserControl
{
	private CancellationTokenSource? _cts;
	protected TextBox? _searchBox;
	protected ListBox? _suggestionsListControl;

	public ObservableCollection<SearchResult> Suggestions { get; } = [];

	public static readonly StyledProperty<IEntitySearchService?> SearchServiceProperty =
		AvaloniaProperty.Register<EntitySearchControlBase, IEntitySearchService?>(nameof(SearchService));
	public IEntitySearchService? SearchService
	{
		get => GetValue(SearchServiceProperty);
		set => SetValue(SearchServiceProperty, value);
	}

	public static readonly StyledProperty<int> SuggestionLimitProperty = AvaloniaProperty.Register<EntitySearchControlBase, int>(nameof(SuggestionLimit), 10);
	public int SuggestionLimit
	{
		get => GetValue(SuggestionLimitProperty);
		set => SetValue(SuggestionLimitProperty, value);
	}

	public static readonly StyledProperty<TimeSpan> DebounceProperty = AvaloniaProperty.Register<EntitySearchControlBase, TimeSpan>(nameof(Debounce), TimeSpan.FromMilliseconds(300));
	public TimeSpan Debounce
	{
		get => GetValue(DebounceProperty);
		set => SetValue(DebounceProperty, value);
	}

	public static readonly StyledProperty<ObservableCollection<object>?> SelectedItemsProperty = AvaloniaProperty.Register<EntitySearchControlBase, ObservableCollection<object>?>(nameof(SelectedItems));
	public ObservableCollection<object>? SelectedItems
	{
		get => GetValue(SelectedItemsProperty);
		set => SetValue(SelectedItemsProperty, value);
	}

	public static readonly StyledProperty<object?> SelectedItemProperty = AvaloniaProperty.Register<EntitySearchControlBase, object?>(nameof(SelectedItem));
	public object? SelectedItem
	{
		get => GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}

	public ICommand RemovePillCommand { get; }

	public EntitySearchControlBase()
	{
		RemovePillCommand = new Core.Domain.Infrastructure.RelayCommand(o => RemovePill(o));
	}

	/// <summary>
	/// Call this from derived control after its XAML has been loaded (InitializeComponent).
	/// It wires the named controls (SearchBox, SuggestionsList, PillsPanel) and hooks events.
	/// </summary>
	protected void InitializeShared()
	{
		_searchBox = this.FindControl<TextBox>("SearchBox");
		_suggestionsListControl = this.FindControl<ListBox>("SuggestionsList");

		SelectedItems ??= new ObservableCollection<object>();

		if (_searchbox_supports_subscription(_searchBox))
		{
			// Subscribe using an IObserver wrapper to avoid requiring System.Reactive extension methods.
			_searchBox!.GetObservable(TextBox.TextProperty).Subscribe(new ActionObserver<string?>(unused => { _ = OnTextChanged(); }));
			_searchBox.KeyDown += SearchBox_KeyDown;
		}

		if (_suggestionsListControl != null)
		{
			_suggestionsListControl.DoubleTapped += SuggestionsList_DoubleTapped;
			_suggestionsListControl.KeyDown += SuggestionsList_KeyDown;
		}
	}

	// Simple IObserver<T> adapter to allow subscribing with an Action<T> without adding System.Reactive.
	private sealed class ActionObserver<T> : IObserver<T>
	{
		private readonly Action<T> _onNext;

		public ActionObserver(Action<T> onNext) => _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));

		public void OnNext(T value) => _onNext(value);
		public void OnError(Exception error) { }
		public void OnCompleted() { }
	}

	// small helper to avoid static analyzer confusion
	private static bool _searchbox_supports_subscription(TextBox? tb) => tb != null;

	private void SuggestionsList_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter && _suggestionsListControl != null && _suggestionsListControl.SelectedIndex >= 0)
		{
			var sr = Suggestions[_suggestionsListControl.SelectedIndex];
			AcceptSuggestion(sr);
			e.Handled = true;
		}
	}

	private void SuggestionsList_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (_suggestionsListControl != null && _suggestionsListControl.SelectedIndex >= 0)
		{
			var sr = Suggestions[_suggestionsListControl.SelectedIndex];
			AcceptSuggestion(sr);
		}
	}

	private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Back && (_searchBox == null || string.IsNullOrEmpty(_searchBox.Text)) && SelectedItems != null && SelectedItems.Any())
		{
			// default behavior: remove last
			if (SelectedItems.Any()) SelectedItems.RemoveAt(SelectedItems.Count - 1);
			e.Handled = true;
		}
		else if (e.Key == Key.Down)
		{
			if (Suggestions.Any() && _suggestionsListControl != null)
				_suggestionsListControl.SelectedIndex = 0;
		}
	}

	private async Task OnTextChanged()
	{
		string text = _searchBox?.Text ?? string.Empty;
		_cts?.Cancel();
		_cts = new CancellationTokenSource();
		var token = _cts.Token;
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

		var results = await SearchService.SearchAsync(text, token);
		Suggestions.Clear();
		foreach (var r in results)
			Suggestions.Add(r);
	}

	protected virtual void RemovePill(object? o)
	{
		if (o == null) return;
		SelectedItems?.Remove(o);
	}

	protected virtual void AcceptSuggestion(SearchResult? suggestion)
	{
		if (suggestion == null) return;
		object entity = suggestion.Entity;

		if (SelectedItems != null && !SelectedItems.Contains(entity))
			SelectedItems.Add(entity);

		SelectedItem = entity;
		_searchBox?.Clear();
		Suggestions.Clear();
	}
}





