using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class AudioFileDetailViewModel(IUnitOfWork uow) : ViewModelBase, IDetailViewModel
{
    public string Path { get; set; } = "";


    public async Task LoadAsync(int id)
    {
        AudioFile? m = await uow.AudioFiles.GetByIdAsync(id);
        Path = m!.Path;
        RaisePropertyChanged(nameof(Path));
    }
}
