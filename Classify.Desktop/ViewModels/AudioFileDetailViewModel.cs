using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class AudioFileDetailViewModel(IUnitOfWork uow, IAppSettings appSettings) : ViewModelBase, IDetailViewModel
{
    public string Path { get; set; } = "";

    public async Task LoadAsync(int id)
    {
        AudioFile? m = await uow.AudioFiles.GetByIdAsync(id);
        Path = m is null ? string.Empty : m.GetFullPath(appSettings);
        RaisePropertyChanged(nameof(Path));
    }
}
