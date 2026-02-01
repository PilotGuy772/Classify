using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Desktop.ViewModels;

public enum LibraryItemType
{
    Composer,
    Work,
    Movement,
    Recording,
    AudioFile
}

public class LibraryViewModel : ViewModelBase, IDisposable, IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly MainWindowViewModel _shell;
    private readonly IIngestionOrchestrationService _scanner;

    public LibraryItemType SelectedType
    {
        get;
        init
        {
            if (field == value) return;
            field = value;
            RaisePropertyChanged();
            _ = LoadAsync();
        }
    }

    public ObservableCollection<LibraryItemViewModel> Items { get; } = [];

    public LibraryViewModel(IUnitOfWork unitOfWork, MainWindowViewModel shell, IIngestionOrchestrationService scanner)
    {
        _unitOfWork = unitOfWork;
        SelectedType = LibraryItemType.Composer;
        _shell = shell;
        _scanner = scanner;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Items.Clear();

        switch (SelectedType)
        {
            case LibraryItemType.Composer:
                foreach (Composer c in await _unitOfWork.Composers.GetAllAsync())
                    Items.Add(new LibraryItemViewModel(c.Id, c.Name, LibraryItemType.Composer));
                break;

            case LibraryItemType.Work:
                foreach (Work w in await _unitOfWork.Works.GetAllAsync())
                    Items.Add(new LibraryItemViewModel(w.Id, w.Name, LibraryItemType.Work));
                break;

            case LibraryItemType.Movement:
                foreach (Movement m in await _unitOfWork.Movements.GetAllAsync())
                    Items.Add(new LibraryItemViewModel(m.Id, m.Name, LibraryItemType.Movement));
                break;

            case LibraryItemType.Recording:
                foreach (Recording r in await _unitOfWork.Recordings.GetAllAsync())
                    Items.Add(new LibraryItemViewModel(r.Id, r.Conductor, LibraryItemType.Recording));
                break;

            case LibraryItemType.AudioFile:
                foreach (AudioFile a in await _unitOfWork.AudioFiles.GetAllAsync())
                    Items.Add(new LibraryItemViewModel(a.Id, a.Path, LibraryItemType.AudioFile));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public async Task OpenItemAsync(LibraryItemViewModel item)
    {
        await _shell.NavigateToDetail(item.Type, item.Id);
    }


    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        GC.SuppressFinalize(this);
    }
}