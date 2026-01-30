using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class AudioFileDetailViewModel : ViewModelBase, IDetailViewModel
{
    public string Path { get; set; }
    private readonly IUnitOfWork _uow;


    public AudioFileDetailViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        Path = "";
    }
    
    public async Task LoadAsync(int id)
    {
        AudioFile? m = await _uow.AudioFiles.GetByIdAsync(id);
        Path = m!.Path;
        RaisePropertyChanged(nameof(Path));
    }
}
