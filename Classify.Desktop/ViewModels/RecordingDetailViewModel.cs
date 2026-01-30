using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class RecordingDetailViewModel : ViewModelBase, IDetailViewModel
{
    public string Conductor { get; set; }
    private readonly IUnitOfWork _uow;

    public RecordingDetailViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        Conductor = "";
    }
    
    public async Task LoadAsync(int id)
    {
        Recording? m = await _uow.Recordings.GetByIdAsync(id);
        Conductor = m!.Conductor;
        RaisePropertyChanged(nameof(Conductor));
    }
}