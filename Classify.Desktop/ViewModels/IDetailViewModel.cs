using System.Threading.Tasks;

namespace Classify.Desktop.ViewModels;

public interface IDetailViewModel
{
    public Task LoadAsync(int id);
}