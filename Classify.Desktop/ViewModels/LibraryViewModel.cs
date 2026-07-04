using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;
using Classify.Core.Domain.Infrastructure;

namespace Classify.Desktop.ViewModels;

/// <summary>
/// View model for the Library browse hierarchical list with filter system tray.
/// </summary>
public sealed class LibraryViewModel : ViewModelBase, IDisposable, IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly MainWindowViewModel _shell;
    private readonly IIngestionOrchestrationService _scanner;

    private LibraryItemViewModel? _selectedLibraryItem;
    private bool _isFiltersTrayOpen;

    // Filter properties (stubs for UI binding)
    private string _composerQuery = string.Empty;
    private string _pieceTitleQuery = string.Empty;
    private string _formQuery = "Any";
    private string _eraQuery = "Any";
    private string _conductorQuery = string.Empty;
    private string _soloistQuery = string.Empty;
    private string _ensembleNameQuery = string.Empty;
    private string _ensembleTypeQuery = "Any";
    private string _yearFromQuery = string.Empty;
    private string _yearToQuery = string.Empty;

    /// <summary>
    /// Gets the list of form options.
    /// </summary>
    public List<string> FormOptions { get; } = new() { "Any", "Symphony", "Concerto", "Sonata", "Suite" };

    /// <summary>
    /// Gets the list of era options.
    /// </summary>
    public List<string> EraOptions { get; } = new() { "Any", "Baroque", "Classical", "Romantic", "Modern" };

    /// <summary>
    /// Gets the list of ensemble type options.
    /// </summary>
    public List<string> EnsembleTypeOptions { get; } = new() { "Any", "Symphony Orchestra", "Chamber Ensemble", "String Quartet", "Solo Instrument" };

    /// <summary>
    /// Gets the collection of currently visible library rows in the list.
    /// </summary>
    public ObservableCollection<LibraryItemViewModel> Items { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected library row item.
    /// </summary>
    public LibraryItemViewModel? SelectedLibraryItem
    {
        get => _selectedLibraryItem;
        set
        {
            if (ReferenceEquals(_selectedLibraryItem, value))
                return;
            _selectedLibraryItem = value;
            RaisePropertyChanged();
            _ = _shell.OnLibraryBrowserSelectionChangedAsync(value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the filters tray is open.
    /// </summary>
    public bool IsFiltersTrayOpen
    {
        get => _isFiltersTrayOpen;
        set
        {
            if (_isFiltersTrayOpen == value) return;
            _isFiltersTrayOpen = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(FilterMenuCaretIcon));
        }
    }

    /// <summary>
    /// Gets the name of the Tabler icon to display for the filter menu toggle caret.
    /// </summary>
    public string FilterMenuCaretIcon => IsFiltersTrayOpen ? "IconCaretDown" : "IconCaretRight";

    /// <summary>
    /// Gets the command to toggle the filters tray open/close state.
    /// </summary>
    public ICommand ToggleFiltersTrayCommand { get; }

    #region Filter Query Properties

    /// <summary>
    /// Gets or sets the composer query string.
    /// </summary>
    public string ComposerQuery
    {
        get => _composerQuery;
        set
        {
            if (_composerQuery == value) return;
            _composerQuery = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the piece title query string.
    /// </summary>
    public string PieceTitleQuery
    {
        get => _pieceTitleQuery;
        set
        {
            if (_pieceTitleQuery == value) return;
            _pieceTitleQuery = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the form filter query string.
    /// </summary>
    public string FormQuery
    {
        get => _formQuery;
        set
        {
            if (_formQuery == value) return;
            _formQuery = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the historical era filter query string.
    /// </summary>
    public string EraQuery
    {
        get => _eraQuery;
        set
        {
            if (_eraQuery == value) return;
            _eraQuery = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the conductor search query string.
    /// </summary>
    public string ConductorQuery
    {
        get => _conductorQuery;
        set
        {
            if (_conductorQuery == value) return;
            _conductorQuery = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the soloist search query string.
    /// </summary>
    public string SoloistQuery
    {
        get => _soloistQuery;
        set
        {
            if (_soloistQuery == value) return;
            _soloistQuery = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the ensemble name search query string.
    /// </summary>
    public string EnsembleNameQuery
    {
        get => _ensembleNameQuery;
        set
        {
            if (_ensembleNameQuery == value) return;
            _ensembleNameQuery = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the ensemble type search query string.
    /// </summary>
    public string EnsembleTypeQuery
    {
        get => _ensembleTypeQuery;
        set
        {
            if (_ensembleTypeQuery == value) return;
            _ensembleTypeQuery = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the start year of recording search query.
    /// </summary>
    public string RecordingYearFromQuery
    {
        get => _yearFromQuery;
        set
        {
            string cleaned = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            if (_yearFromQuery == cleaned)
            {
                if (value != cleaned)
                {
                    RaisePropertyChanged();
                }
                return;
            }
            _yearFromQuery = cleaned;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the end year of recording search query.
    /// </summary>
    public string RecordingYearToQuery
    {
        get => _yearToQuery;
        set
        {
            string cleaned = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            if (_yearToQuery == cleaned)
            {
                if (value != cleaned)
                {
                    RaisePropertyChanged();
                }
                return;
            }
            _yearToQuery = cleaned;
            RaisePropertyChanged();
        }
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryViewModel"/> class.
    /// </summary>
    /// <param name="unitOfWork">The database unit of work.</param>
    /// <param name="shell">The shell window view model.</param>
    /// <param name="scanner">The library scanning service.</param>
    public LibraryViewModel(IUnitOfWork unitOfWork, MainWindowViewModel shell, IIngestionOrchestrationService scanner)
    {
        _unitOfWork = unitOfWork;
        _shell = shell;
        _scanner = scanner;
        ToggleFiltersTrayCommand = new RelayCommand(_ => IsFiltersTrayOpen = !IsFiltersTrayOpen);
        _ = LoadAsync();
    }

    /// <summary>
    /// Initial load of the Composer list (root nodes).
    /// </summary>
    private async Task LoadAsync()
    {
        SelectedLibraryItem = null;
        Items.Clear();

        IEnumerable<Composer> composers = await _unitOfWork.Composers.GetAllAsync();
        foreach (Composer c in composers.OrderBy(x => x.Name))
        {
            LibraryItemViewModel item = new LibraryItemViewModel(c.Id, c.Name, LibraryItemType.Composer, level: 0);
            item.ToggleExpandCallback = (n) => _ = ToggleExpandNodeAsync(n);
            Items.Add(item);
        }

        UpdateAlternatingBackdrops();
    }

    /// <summary>
    /// Handles expanding or collapsing a node.
    /// </summary>
    private async Task ToggleExpandNodeAsync(LibraryItemViewModel node)
    {
        if (node.IsExpanded)
        {
            if (node.Children == null)
            {
                node.Children = await LoadChildrenAsync(node);
            }

            int index = Items.IndexOf(node);
            if (index >= 0)
            {
                List<LibraryItemViewModel> descendants = new List<LibraryItemViewModel>();
                GetVisibleDescendants(node, descendants);
                for (int i = 0; i < descendants.Count; i++)
                {
                    Items.Insert(index + 1 + i, descendants[i]);
                }
            }
        }
        else
        {
            int index = Items.IndexOf(node);
            if (index >= 0)
            {
                List<LibraryItemViewModel> descendants = new List<LibraryItemViewModel>();
                GetDescendantsToCollapse(node, descendants);
                foreach (LibraryItemViewModel desc in descendants)
                {
                    Items.Remove(desc);
                }
            }
            node.ResetExpansionState();
        }

        UpdateAlternatingBackdrops();
    }

    /// <summary>
    /// Traverses and collects all descendants that are visible when expanding a node.
    /// </summary>
    private void GetVisibleDescendants(LibraryItemViewModel node, List<LibraryItemViewModel> result)
    {
        if (node.Children != null)
        {
            foreach (LibraryItemViewModel child in node.Children)
            {
                result.Add(child);
                if (child.IsExpanded)
                {
                    GetVisibleDescendants(child, result);
                }
            }
        }
    }

    /// <summary>
    /// Traverses and collects all descendants of a node to remove during collapse.
    /// </summary>
    private void GetDescendantsToCollapse(LibraryItemViewModel node, List<LibraryItemViewModel> result)
    {
        if (node.Children != null)
        {
            foreach (LibraryItemViewModel child in node.Children)
            {
                result.Add(child);
                GetDescendantsToCollapse(child, result);
            }
        }
    }

    /// <summary>
    /// Lazily loads children of a specific node from the database.
    /// </summary>
    private async Task<List<LibraryItemViewModel>> LoadChildrenAsync(LibraryItemViewModel node)
    {
        List<LibraryItemViewModel> list = new List<LibraryItemViewModel>();

        if (node.Type == LibraryItemType.Composer)
        {
            IEnumerable<Work> works = await _unitOfWork.Works.GetWorksByComposerIdAsync(node.Id);
            foreach (Work w in works.OrderBy(x => x.Name))
            {
                LibraryItemViewModel item = new LibraryItemViewModel(w.Id, w.Name, LibraryItemType.Work, level: 1);
                item.ToggleExpandCallback = (n) => _ = ToggleExpandNodeAsync(n);
                list.Add(item);
            }
        }
        else if (node.Type == LibraryItemType.Work)
        {
            IEnumerable<Recording> recordings = await _unitOfWork.Recordings.GetRecordingsByWorkIdAsync(node.Id);
            foreach (Recording r in recordings)
            {
                List<string> parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(r.Soloist)) parts.Add(r.Soloist);
                if (!string.IsNullOrWhiteSpace(r.Conductor)) parts.Add(r.Conductor);
                if (!string.IsNullOrWhiteSpace(r.Ensemble)) parts.Add(r.Ensemble);

                string artistLine = string.Join(" - ", parts);
                string labelYear = r.Year.HasValue ? $"([Label], {r.Year.Value})" : "([Label])";
                string disp = string.IsNullOrWhiteSpace(artistLine) ? $"Recording #{r.Id} {labelYear}" : $"{artistLine} ({labelYear.Trim('(', ')')})";

                LibraryItemViewModel item = new LibraryItemViewModel(r.Id, disp, LibraryItemType.Recording, level: 2);
                item.ToggleExpandCallback = (n) => _ = ToggleExpandNodeAsync(n);
                list.Add(item);
            }
        }
        else if (node.Type == LibraryItemType.Recording)
        {
            IEnumerable<PerformedMovement> pms = await _unitOfWork.PerformedMovements.GetByRecordingId(node.Id);
            List<(PerformedMovement Pm, Movement Mv)> temp = new List<(PerformedMovement Pm, Movement Mv)>();

            foreach (PerformedMovement pm in pms)
            {
                Movement? mv = await _unitOfWork.Movements.GetByIdAsync(pm.MovementId);
                if (mv != null)
                {
                    temp.Add((pm, mv));
                }
            }

            List<(PerformedMovement Pm, Movement Mv)> sorted = temp
                .OrderBy(x => x.Mv.Order)
                .ThenBy(x => x.Mv.Name)
                .ToList();

            foreach ((PerformedMovement Pm, Movement Mv) pair in sorted)
            {
                string roman = FormatRomanOrdinal(pair.Mv.Order);
                LibraryItemViewModel item = new LibraryItemViewModel(pair.Pm.Id, pair.Mv.Name, LibraryItemType.MovementRecording, level: 3);
                item.Ordinal = roman;
                list.Add(item);
            }
        }

        return list;
    }

    /// <summary>
    /// Recalculates the alternating backdrop property for all visible items.
    /// </summary>
    private void UpdateAlternatingBackdrops()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].IsAlternate = (i % 2 == 1);
        }
    }

    /// <summary>
    /// Formats an index into a Roman numeral.
    /// </summary>
    private static string FormatRomanOrdinal(int indexOneBased)
    {
        string[] table =
        [
            "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
            "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX",
            "XXI", "XXII", "XXIII", "XXIV", "XXV", "XXVI", "XXVII", "XXVIII", "XXIX", "XXX"
        ];
        if (indexOneBased <= 0 || indexOneBased >= table.Length)
            return indexOneBased.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
        return table[indexOneBased] + ".";
    }

    /// <summary>
    /// Double click handler target to navigate to details page.
    /// </summary>
    public async Task OpenItemAsync(LibraryItemViewModel item)
    {
        await _shell.NavigateToDetail(item.Type, item.Id);
    }

    /// <summary>
    /// Asynchronously disposes the resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources.
    /// </summary>
    public void Dispose()
    {
        _unitOfWork.Dispose();
        GC.SuppressFinalize(this);
    }
}