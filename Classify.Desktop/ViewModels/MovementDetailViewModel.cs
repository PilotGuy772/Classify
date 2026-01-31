using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class MovementDetailViewModel(IUnitOfWork uow) : ViewModelBase, IDetailViewModel
{
    public string Name { get; set; } = "";

    public async Task LoadAsync(int id)
    {
        Movement? m = await uow.Movements.GetByIdAsync(id);
        Name = m!.Name;
        RaisePropertyChanged(nameof(Name));
    }
}