using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class MovementDetailViewModel : ViewModelBase, IDetailViewModel
{
    public string Name { get; set; }
    private readonly IUnitOfWork _uow;

    public MovementDetailViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        Name = "";
    }
    
    public async Task LoadAsync(int id)
    {
        Movement? m = await _uow.Movements.GetByIdAsync(id);
        Name = m!.Name;
        RaisePropertyChanged(nameof(Name));
    }
}