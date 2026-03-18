using System.Collections.ObjectModel;

namespace Classify.Desktop.ViewModels;

public class HomeViewModel : ViewModelBase
{
    public string Text => "Home page";

    public Classify.Data.Services.ComposerSearchService ComposerSearchService { get; }

    //private object? _selectedComposer;
    public ObservableCollection<object> SelectedComposers { get; set; } = [];
    public object? SelectedComposer { get; set; }

    public string DisplayComposerName
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            RaisePropertyChanged();
        }
    } = string.Empty;

    public System.Windows.Input.ICommand SubmitCommand { get; }

    public HomeViewModel(Classify.Data.Services.ComposerSearchService composerSearchService)
    {
        ComposerSearchService = composerSearchService;
        SubmitCommand = new Core.Domain.Infrastructure.RelayCommand(_ => OnSubmit());
    }

    private void OnSubmit()
    {
        string name = "";
        foreach (object o in SelectedComposers)
        {
            if (o is Core.Domain.Composer c)
            {
                name += c.Name + ", ";
            }
            else if (o is not null)
            {
                name += o + ", ";
            }
        }
        
        name = name[..^2];
        DisplayComposerName = name;
        // if (SelectedComposer is Classify.Core.Domain.Composer c)
        // {
        //     DisplayComposerName = c.Name;
        // }
        // else if (SelectedComposer != null)
        // {
        //     DisplayComposerName = SelectedComposer.ToString() ?? string.Empty;
        // }
    }
}