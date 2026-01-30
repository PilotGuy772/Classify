using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class WorkDetailViewModel : ViewModelBase, IDetailViewModel
{
    public string Name { get; set; }
    private readonly IUnitOfWork _uow;

    public WorkDetailViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        Name = "";
    }
    
    public async Task LoadAsync(int id)
    {
        Work? m = await _uow.Works.GetByIdAsync(id);
        Name = m!.Name;
        RaisePropertyChanged(nameof(Name));
    }
}