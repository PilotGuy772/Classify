using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class WorkDetailViewModel(IUnitOfWork uow) : ViewModelBase, IDetailViewModel
{
    public string Name { get; set; } = "";

    public async Task LoadAsync(int id)
    {
        Work? m = await uow.Works.GetByIdAsync(id);
        Name = m!.Name;
        RaisePropertyChanged(nameof(Name));
    }
}