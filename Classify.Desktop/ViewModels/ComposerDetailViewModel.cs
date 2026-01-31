using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Desktop.ViewModels;

public class ComposerDetailViewModel(IUnitOfWork uow) : ViewModelBase, IDetailViewModel
{
    public string Name { get; set; } = "";

    public async Task LoadAsync(int id)
    {
        Composer? composer = await uow.Composers.GetByIdAsync(id);
        Name = composer!.Name;
        RaisePropertyChanged(nameof(Name));
    }
}