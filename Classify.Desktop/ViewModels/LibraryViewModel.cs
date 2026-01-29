using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces;

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

    public ObservableCollection<string> Items { get; } = new();

    public LibraryViewModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        SelectedType = LibraryItemType.Composer;
    }

    private async Task LoadAsync()
    {
        Items.Clear();

        switch (SelectedType)
        {
            case LibraryItemType.Composer:
                foreach (Composer c in await _unitOfWork.Composers.GetAllAsync())
                    Items.Add(c.Name);
                break;

            case LibraryItemType.Work:
                foreach (Work w in await _unitOfWork.Works.GetAllAsync())
                    Items.Add(w.Name);
                break;

            case LibraryItemType.Movement:
                foreach (Movement m in await _unitOfWork.Movements.GetAllAsync())
                    Items.Add(m.Name);
                break;

            case LibraryItemType.Recording:
                foreach (Recording r in await _unitOfWork.Recordings.GetAllAsync())
                    Items.Add(r.Conductor);
                break;

            case LibraryItemType.AudioFile:
                foreach (AudioFile a in await _unitOfWork.AudioFiles.GetAllAsync())
                    Items.Add(a.Path);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
    }

    public void Dispose()
    {
        _unitOfWork.DisposeAsync();
    }
}