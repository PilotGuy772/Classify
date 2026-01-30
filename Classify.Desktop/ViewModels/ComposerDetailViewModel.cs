using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class ComposerDetailViewModel : ViewModelBase, IDetailViewModel
{
    public string Name { get; set; }
    private readonly IUnitOfWork _uow;

    public ComposerDetailViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        Name = "";
    }
    
    public async Task LoadAsync(int id)
    {
        Composer? composer = await _uow.Composers.GetByIdAsync(id);
        Name = composer!.Name;
        RaisePropertyChanged(nameof(Name));
    }
}