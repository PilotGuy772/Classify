using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class RecordingDetailViewModel(IUnitOfWork uow) : ViewModelBase, IDetailViewModel
{
    public string Conductor { get; set; } = "";

    public async Task LoadAsync(int id)
    {
        Recording? m = await uow.Recordings.GetByIdAsync(id);
        Conductor = m!.Conductor;
        RaisePropertyChanged(nameof(Conductor));
    }
}